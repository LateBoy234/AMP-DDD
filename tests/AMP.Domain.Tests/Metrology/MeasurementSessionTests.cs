using AMP.Domain.Metrology;
using Xunit;

namespace AMP.Domain.Tests.Metrology;

/// <summary>
/// 第 2 课的核心证据：聚合根的不变量守卫。
/// 注意这些测试测的全是"业务上禁止什么"——
/// 这正是老项目里散落各处、十处判断九处漏的那些规则，现在集中在一个类里被穷举。
/// </summary>
public class MeasurementSessionTests
{
    private static readonly InstrumentIdentity Instrument = new("NR20XE", "SN20260101");

    private static readonly IReadOnlyList<StandardSwatch> TwoSwatches = new[]
    {
        new StandardSwatch("白砖", new LabColor(95, 0, 0)),
        new StandardSwatch("黑砖", new LabColor(20, 0, 0)),
    };

    private static readonly GradingThresholds Thresholds = new(1.0, 2.0);

    // ---------- 出生即合法 ----------

    [Fact]
    public void Start_标准为空拒绝()
    {
        Assert.Throws<ArgumentException>(
            () => MeasurementSession.Start(Instrument, "标准", Array.Empty<StandardSwatch>(), Thresholds, 36));
    }

    [Fact]
    public void Start_砖名重复拒绝()
    {
        var duplicated = new[]
        {
            new StandardSwatch("白砖", new LabColor(95, 0, 0)),
            new StandardSwatch("白砖", new LabColor(96, 0, 0)),
        };

        Assert.Throws<ArgumentException>(
            () => MeasurementSession.Start(Instrument, "标准", duplicated, Thresholds, 36));
    }

    [Fact]
    public void Start_限值无效拒绝()
    {
        var bad = new GradingThresholds(2.0, 1.0);

        Assert.Throws<ArgumentException>(
            () => MeasurementSession.Start(Instrument, "标准", TwoSwatches, bad, 36));
    }

    // ---------- 身份：实体与值对象的区别 ----------

    [Fact]
    public void 同仪器的两次会话是两个不同的实体()
    {
        MeasurementSession s1 = StartSession();
        MeasurementSession s2 = StartSession();

        Assert.NotEqual(s1.Id, s2.Id);
        // 老项目因为没有领域身份，只能在存储层用 SHA256 判"是不是同一条记录"——
        // 两次完全相同的检定本来就该是两条记录，去重把合法历史吞掉了。
        Assert.Equal(s1.Instrument, s2.Instrument);
    }

    // ---------- 逐砖判定的不变量 ----------

    [Fact]
    public void 记录不属于标准的砖_拒绝()
    {
        MeasurementSession session = StartSession();

        Assert.Throws<InvalidOperationException>(
            () => session.RecordSwatch("不存在的砖", new LabColor(90, 0, 0)));
    }

    [Fact]
    public void 同一块砖不允许判两次()
    {
        MeasurementSession session = StartSession();
        session.RecordSwatch("白砖", new LabColor(95.2, 0.1, 0));

        Assert.Throws<InvalidOperationException>(
            () => session.RecordSwatch("白砖", new LabColor(95.3, 0, 0)));
    }

    [Fact]
    public void 记录色砖时自动套用限值判定()
    {
        MeasurementSession session = StartSession();

        // 实测 L 偏离标准 0.5 → ΔE = 0.5 ≤ 二级限值 1.0 → 合格
        GradingResult result = session.RecordSwatch("白砖", new LabColor(95.5, 0, 0));

        Assert.Equal(MetrologyGrade.Qualified, result.Grade);
        Assert.Single(session.SwatchResults);
        Assert.Single(session.MissingSwatchNames);
    }

    // ---------- 完成与冻结 ----------

    [Fact]
    public void 色砖未判完不允许出结论()
    {
        MeasurementSession session = StartSession();
        session.RecordSwatch("白砖", new LabColor(95.2, 0, 0));
        FillGroups(session);

        Assert.Throws<InvalidOperationException>(() => session.Complete());
    }

    [Fact]
    public void 组数不足不允许出结论()
    {
        MeasurementSession session = StartSession();
        RecordAllSwatches(session);
        session.RecordGroupMeasurement(D(0.5));
        // 还差 2 组

        Assert.Throws<InvalidOperationException>(() => session.Complete());
    }

    [Fact]
    public void 正常流程_出结论并冻结()
    {
        MeasurementSession session = StartSession();
        RecordAllSwatches(session);
        foreach (double delta in new[] { 0.5, 0.4, 0.6 })
            session.RecordGroupMeasurement(D(delta));

        GradingResult final = session.Complete(new DateTimeOffset(2026, 11, 4, 10, 0, 0, TimeSpan.Zero));

        Assert.Equal(MetrologyGrade.Qualified, final.Grade); // 平均 0.5 ≤ 1
        Assert.Equal(MeasurementSessionStatus.Completed, session.Status);
        Assert.NotNull(session.CompletedAt);
        Assert.NotNull(session.FinalResult);
    }

    [Fact]
    public void 完成后不允许再补数据()
    {
        MeasurementSession session = StartSession();
        RecordAllSwatches(session);
        FillGroups(session);
        session.Complete();

        Assert.Throws<InvalidOperationException>(() => session.RecordSwatch("白砖", new LabColor(95, 0, 0)));
        Assert.Throws<InvalidOperationException>(() => session.RecordGroupMeasurement(D(0.1)));
        // 已经记满再记也拒绝——冻结后连"合法操作"都不允许
        Assert.Throws<InvalidOperationException>(() => session.RecordSwatch("黑砖", new LabColor(20, 0, 0)));
    }

    // ---------- 辅助 ----------

    private static MeasurementSession StartSession() =>
        MeasurementSession.Start(Instrument, "测试标准", TwoSwatches, Thresholds, requiredGroupCount: 3);

    private static void RecordAllSwatches(MeasurementSession session)
    {
        session.RecordSwatch("白砖", new LabColor(95.2, 0, 0));
        session.RecordSwatch("黑砖", new LabColor(20.1, 0, 0));
    }

    private static void FillGroups(MeasurementSession session)
    {
        session.RecordGroupMeasurement(D(0.5));
        session.RecordGroupMeasurement(D(0.4));
        session.RecordGroupMeasurement(D(0.6));
    }

    private static DeltaE D(double v) => DeltaE.FromComputed(v);
}
