namespace AMP.Domain.Metrology;

/// <summary>
/// 色砖判定的限值，即规程原文中的"二级限值/三级限值"。
///
/// 为什么做成参数而不是常量？
/// 这些数字在老项目里来自 MetrologyDelta 实体、随计量标准配置——是业务参数，
/// 不同机型/不同标准可能不同。参数化的东西放配置，规程固定的事实放常量，
/// 二者混在一起是配置失控的开始。
///
/// 不可变：限值在"一次判定过程"中必须稳定。老项目允许运行中改配置表再继续判定，
/// 同一批仪器可能被两套标准判过——这是质量事故的温床。
/// </summary>
/// <param name="Level2Limit">ΔE ≤ 此值判合格（对应规程"二级"）。</param>
/// <param name="Level3Limit">ΔE &gt; 此值判报废；介于两者之间降级（对应规程"三级"）。</param>
public readonly record struct GradingThresholds(double Level2Limit, double Level3Limit)
{
    /// <summary>0 &lt; 二级限值 &lt; 三级限值，否则判定规则没有意义。</summary>
    public bool IsValid => Level2Limit > 0 && Level3Limit > Level2Limit;
}
