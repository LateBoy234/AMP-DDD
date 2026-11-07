namespace AMP.Domain.Metrology.Rules;

/// <summary>
/// 规程第(1)条：用色砖实测 Lab 与标准 Lab 算 ΔE，
/// 不超二级限值判合格，二级与三级之间降级，三级以上报废。
///
/// 为什么是"纯函数静态类"？
/// 它不碰数据库、不碰硬件、不碰 UI、不依赖任何可变状态——
/// 同样的输入永远得到同样的输出。这正是它可以被单元测试穷举的原因，
/// 也是老项目 SVDBaseModel 的反面：它判定完顺手把表格染成橙色、弹 toast，
/// 于是测一个"是否降级"就得启动整个 WPF 程序。
/// </summary>
public static class SwatchGradeRule
{
    /// <summary>
    /// 对一块色砖做出判定。注意：只判一块；"8 块逐一判定"是上层编排的事，
    /// 规则不关心第几块、也不关心是谁在测——职责越小，复用和测试越容易。
    /// </summary>
    public static GradingResult Judge(LabColor measured, LabColor standard, GradingThresholds thresholds)
    {
        if (!thresholds.IsValid)
            throw new ArgumentException("计量限值无效：必须满足 0 < 二级限值 < 三级限值。", nameof(thresholds));

        DeltaE deltaE = measured.DeltaETo(standard);
        MetrologyGrade grade =
            deltaE.Value <= thresholds.Level2Limit ? MetrologyGrade.Qualified :
            deltaE.Value <= thresholds.Level3Limit ? MetrologyGrade.Downgraded :
            MetrologyGrade.Scrapped;

        return new GradingResult(grade, deltaE);
    }
}
