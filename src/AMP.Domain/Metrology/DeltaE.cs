namespace AMP.Domain.Metrology;

/// <summary>
/// 色差 ΔE。由平方和开根而来，数学上永远 ≥ 0。
///
/// 为什么不直接用 double？这是"基本类型偏执（Primitive Obsession）"的反例教学：
/// double 能表示 -5、NaN、无穷——这些对 ΔE 全是非法状态。
/// 用工厂方法拦截非法值，让"非法的 ΔE 根本造不出来"，
/// 下游规则就不再需要到处写防御性 if（对比老项目：判定逻辑里散布各种数据合理性检查）。
/// </summary>
public readonly record struct DeltaE
{
    public double Value { get; }

    private DeltaE(double value) => Value = value;

    /// <summary>
    /// 由计算结果构造。NaN 说明上游（仪器通信/反推算法）出了问题，
    /// 必须当场爆炸（Fail Fast），绝不能带着 NaN 流进判定链路。
    /// </summary>
    public static DeltaE FromComputed(double value)
    {
        if (double.IsNaN(value))
            throw new ArgumentException("ΔE 计算结果为 NaN，上游数据异常，需先排查仪器通信或反推算法。", nameof(value));
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), value, "ΔE 不可能为负，检查计算公式输入。");
        return new DeltaE(value);
    }

    public override string ToString() => Value.ToString("F4");
}
