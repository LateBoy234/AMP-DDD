using AMP.Domain.Metrology.Rules;

namespace AMP.Domain.Metrology;

/// <summary>
/// 一次计量检定会话：某台仪器、按某套标准、在某个限值下，测完所有色砖
/// 和规定组数的数据，得出 最终结论。这是本系统的核心业务概念（聚合根）。
///
/// ── 实体 vs 值对象 ─────────────────────────────────────────────
/// 会话是"实体"：它有身份（Id），两次会话即使仪器、数据、结论全部相同，
/// 也是两次不同的检定。而 DeltaE、SwatchMeasurement 是"值对象"：只看内容。
/// 分不清这两者，就会像老项目 IndicationErrorMeasurement 那样——
/// 领域里没有身份概念，只好在存储层发明 SHA256 去重来补救。
/// ─────────────────────────────────────────────────────────────
///
/// ── 聚合与不变量 ─────────────────────────────────────────────
/// 聚合根是一致性边界的守门人。它守着这些不变量：
///   1. 未出结论前，结论不存在（FinalResult 为 null）；
///   2. 结论一旦给出，记录冻结——已完成的检定不允许再补数据（审计要求）；
///   3. 每块砖最多判一次，且必须属于所用计量标准（不允许"测了不存在的砖"）；
///   4. 组数不满不出结论（规程要求 36 组，少一组都不能拍板）。
/// 所有变更都经过方法、由方法守护不变量——外部永远无法绕过。
/// 对比老项目：实体的 RawDataJson 是个谁都能反序列化改写的袋子，
/// "36 组是否齐全"要靠查询代码各自判断，十处判断九处漏。
/// ─────────────────────────────────────────────────────────────
/// </summary>
public sealed class MeasurementSession
{
    private readonly Dictionary<string, LabColor> _standards;
    private readonly Dictionary<string, SwatchMeasurement> _swatchResults = new(StringComparer.Ordinal);
    private readonly List<DeltaE> _groupDeltaEs = new();

    /// <summary>会话身份。由工厂生成，终身不变。</summary>
    public Guid Id { get; }

    /// <summary>被检仪器。SN 是计量追溯链的锚点。</summary>
    public InstrumentIdentity Instrument { get; }

    /// <summary>所用计量标准的名称（如"NR 系列 8 砖标准"）。</summary>
    public string StandardName { get; }

    /// <summary>本次会话锁定的判定限值。创建时确定，进行中不可换标准。</summary>
    public GradingThresholds Thresholds { get; }

    /// <summary>规程要求的测量组数（如 36）。作为构造参数而非硬编码：不同机型规程可能不同。</summary>
    public int RequiredGroupCount { get; }

    public MeasurementSessionStatus Status { get; private set; }

    public DateTimeOffset StartedAt { get; }
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>最终结论（平均 ΔE 判定）。未完成前为 null——"没测完就没有结论"直接体现在类型上。</summary>
    public GradingResult? FinalResult { get; private set; }

    /// <summary>已判定的色砖记录。键为砖名。只读视图：外部看得到，改不了。</summary>
    public IReadOnlyDictionary<string, SwatchMeasurement> SwatchResults => _swatchResults;

    /// <summary>已记录的各组 ΔE。AsReadOnly 包装：连强制类型转换改回去的口子都堵上。</summary>
    public IReadOnlyList<DeltaE> GroupDeltaEs => _groupDeltaEs.AsReadOnly();

    /// <summary>还没判定的砖名——给上层编排/进度展示用，不必让外面自己数字典。</summary>
    public IEnumerable<string> MissingSwatchNames =>
        _standards.Keys.Except(_swatchResults.Keys, StringComparer.Ordinal);

    private MeasurementSession(
        Guid id, InstrumentIdentity instrument, string standardName,
        Dictionary<string, LabColor> standards, GradingThresholds thresholds,
        int requiredGroupCount, DateTimeOffset startedAt)
    {
        Id = id;
        Instrument = instrument;
        StandardName = standardName;
        _standards = standards;
        Thresholds = thresholds;
        RequiredGroupCount = requiredGroupCount;
        StartedAt = startedAt;
        Status = MeasurementSessionStatus.InProgress;
    }

    /// <summary>
    /// 开启一次检定。为什么用工厂方法而不用 public 构造函数？
    /// 因为"出生即合法"——所有不变量在这里一次性校验，
    /// 不存在"先 new 出来、再一点点 set、中途任何时刻都是半成品"的对象。
    /// 半成品对象流转到别处，就是 NullReferenceException 的工厂。
    /// </summary>
    public static MeasurementSession Start(
        InstrumentIdentity instrument,
        string standardName,
        IReadOnlyList<StandardSwatch> standardSwatches,
        GradingThresholds thresholds,
        int requiredGroupCount,
        DateTimeOffset? startedAt = null)
    {
        if (string.IsNullOrWhiteSpace(standardName))
            throw new ArgumentException("计量标准名称不能为空。", nameof(standardName));
        if (standardSwatches is null || standardSwatches.Count == 0)
            throw new ArgumentException("计量标准至少要有一块色砖。", nameof(standardSwatches));
        if (!thresholds.IsValid)
            throw new ArgumentException("判定限值无效：必须满足 0 < 二级限值 < 三级限值。", nameof(thresholds));
        if (requiredGroupCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(requiredGroupCount), requiredGroupCount, "测量组数必须为正。");

        var standards = new Dictionary<string, LabColor>(StringComparer.Ordinal);
        foreach (StandardSwatch swatch in standardSwatches)
        {
            if (!standards.TryAdd(swatch.Name, swatch.StandardLab))
                throw new ArgumentException($"计量标准中色砖名重复：\"{swatch.Name}\"。", nameof(standardSwatches));
        }

        return new MeasurementSession(
            Guid.NewGuid(), instrument, standardName.Trim(),
            standards, thresholds, requiredGroupCount, startedAt ?? DateTimeOffset.Now);
    }

    /// <summary>
    /// 记录一块色砖的实测值。会话自己查标准值、自己调规则、自己存结论——
    /// 调用方只需提供事实（"这块砖测出来是这个 Lab"），不负责判断。
    /// 领域行为放在领域对象上，而不是让 Application 层拿来摆弄内部集合。
    /// </summary>
    /// <returns>该砖的判定结果，供编排层展示/继续流转。</returns>
    public GradingResult RecordSwatch(string swatchName, LabColor measured)
    {
        EnsureInProgress(nameof(RecordSwatch));

        if (!_standards.TryGetValue(swatchName, out LabColor standardLab))
            throw new InvalidOperationException(
                $"色砖\"{swatchName}\"不属于计量标准\"{StandardName}\"，拒绝记录。");
        if (_swatchResults.ContainsKey(swatchName))
            throw new InvalidOperationException(
                $"色砖\"{swatchName}\"已有判定结果。如需重测，应作废本次会话重新开始，而不是覆盖历史。");

        GradingResult result = SwatchGradeRule.Judge(measured, standardLab, Thresholds);
        _swatchResults[swatchName] = new SwatchMeasurement(swatchName, measured, standardLab, result);
        return result;
    }

    /// <summary>记录一组实测数据与标准的 ΔE（36 组之一）。判定留给 Complete() 统一做。</summary>
    public void RecordGroupMeasurement(DeltaE deltaE)
    {
        EnsureInProgress(nameof(RecordGroupMeasurement));

        if (_groupDeltaEs.Count >= RequiredGroupCount)
            throw new InvalidOperationException(
                $"已记录满 {RequiredGroupCount} 组数据，多出的一组说明流程编排有误，拒绝记录。");

        _groupDeltaEs.Add(deltaE);
    }

    /// <summary>
    /// 所有色砖判完、组数记满后出最终结论。结论由规程第(2)条（平均 ΔE 规则）给出。
    /// 完成即冻结：此后任何 Record 都抛异常——计量记录是不可抵赖的历史。
    /// </summary>
    public GradingResult Complete(DateTimeOffset? completedAt = null)
    {
        EnsureInProgress(nameof(Complete));

        List<string> missing = MissingSwatchNames.ToList();
        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"尚有 {missing.Count} 块色砖未判定（{string.Join("、", missing)}），不能出结论。");
        if (_groupDeltaEs.Count != RequiredGroupCount)
            throw new InvalidOperationException(
                $"数据组数不足：已记 {_groupDeltaEs.Count} 组，规程要求 {RequiredGroupCount} 组。");

        GradingResult final = AverageGradeRule.Judge(_groupDeltaEs);
        Status = MeasurementSessionStatus.Completed;
        CompletedAt = completedAt ?? DateTimeOffset.Now;
        FinalResult = final;
        return final;
    }

    private void EnsureInProgress(string operation)
    {
        if (Status != MeasurementSessionStatus.InProgress)
            throw new InvalidOperationException(
                $"会话已完成（{CompletedAt}），结论已定，不允许再执行 {operation}。");
    }
}
