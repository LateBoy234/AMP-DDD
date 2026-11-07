namespace AMP.Domain.Metrology;

/// <summary>
/// 计量判定结论。对应《计量结果说明》原文：合格 / 降级 / 报废。
///
/// 为什么是领域枚举而不是 string 或 int？
/// string 会拼错、int 会魔法数字满天飞；枚举让"一台上合格的仪器"这个概念
/// 在整个系统里只有一种拼法，编译器替你把关。
/// </summary>
public enum MetrologyGrade
{
    /// <summary>合格（规程：ΔE 不超二级限值 / 平均 ΔE ≤ 1）。</summary>
    Qualified = 0,

    /// <summary>降级（介于合格线与报废线之间，降等使用）。</summary>
    Downgraded = 1,

    /// <summary>报废（超出降级线）。</summary>
    Scrapped = 2,
}
