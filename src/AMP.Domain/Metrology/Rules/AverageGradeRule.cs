namespace AMP.Domain.Metrology.Rules;

/// <summary>
/// 规程第(2)条：36 组数据的平均 ΔE，≤1 合格、1~2 降级、&gt;2 报废。
///
/// 为什么这里用常量、SwatchGradeRule 却用参数？
/// 1/2 这两个数字写在规程条文里，属于"规程事实"，全网统一、不该被配置改掉；
/// 而"二级/三级限值"随计量标准配置变化。区分"事实"与"参数"，
/// 是防止配置失控的第一课（YAGNI：规程没说要可配，就不要做成可配）。
/// </summary>
public static class AverageGradeRule
{
    public const double QualifiedLimit = 1.0;
    public const double DowngradedLimit = 2.0;

    /// <summary>
    /// 对整组数据的平均 ΔE 做出判定。
    /// 规则只管"给定一组 ΔE 怎么判"；至于这 36 组是怎么测出来、怎么反推的，
    /// 属于后续课程（Application 层的用例编排）——边界划清楚，规则才稳定。
    /// </summary>
    public static GradingResult Judge(IReadOnlyList<DeltaE> deltaEs)
    {
        if (deltaEs is null || deltaEs.Count == 0)
            throw new ArgumentException("至少需要一组 ΔE 数据才能求平均。", nameof(deltaEs));

        double average = deltaEs.Average(d => d.Value);
        MetrologyGrade grade =
            average <= QualifiedLimit ? MetrologyGrade.Qualified :
            average <= DowngradedLimit ? MetrologyGrade.Downgraded :
            MetrologyGrade.Scrapped;

        return new GradingResult(grade, DeltaE.FromComputed(average));
    }
}
