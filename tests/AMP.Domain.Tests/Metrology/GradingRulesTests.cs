using AMP.Domain.Metrology;
using AMP.Domain.Metrology.Rules;
using Xunit;

namespace AMP.Domain.Tests.Metrology;

/// <summary>
/// 第 1 课的核心证据：这些测试不需要数据库、不需要运动平台、不需要 WPF，
/// 毫秒级跑完。老项目里同样的判定逻辑埋在 ViewModel 里，根本无法这样测。
/// </summary>
public class GradingRulesTests
{
    // ---------- 规程第(1)条：色砖逐块判定 ----------

    private static readonly GradingThresholds Thresholds = new(Level2Limit: 1.0, Level3Limit: 2.0);

    [Theory]
    [InlineData(0.9999, MetrologyGrade.Qualified)]   // 边界内：合格
    [InlineData(1.0, MetrologyGrade.Qualified)]      // 边界值：≤ 二级限值判合格（原文"小于等于二级"）
    [InlineData(1.5, MetrologyGrade.Downgraded)]     // 二级与三级之间：降级
    [InlineData(2.0, MetrologyGrade.Downgraded)]     // 恰好等于三级限值：仍是降级（未"超过"）
    [InlineData(2.0001, MetrologyGrade.Scrapped)]    // 超过三级限值：报废
    public void SwatchGradeRule_按规程阈值分级(double delta, MetrologyGrade expected)
    {
        // 构造实测色 = 标准色在 L 轴上偏移 delta，使 ΔE 恰好等于 delta
        var standard = new LabColor(50, 0, 0);
        var measured = new LabColor(50 + delta, 0, 0);

        GradingResult result = SwatchGradeRule.Judge(measured, standard, Thresholds);

        Assert.Equal(expected, result.Grade);
        Assert.Equal(delta, result.DeltaE.Value, precision: 4);
    }

    [Fact]
    public void SwatchGradeRule_限值倒置时拒绝判定()
    {
        var bad = new GradingThresholds(Level2Limit: 2.0, Level3Limit: 1.0);

        Assert.Throws<ArgumentException>(
            () => SwatchGradeRule.Judge(new LabColor(50, 0, 0), new LabColor(50, 0, 0), bad));
    }

    // ---------- 规程第(2)条：平均 ΔE 判定 ----------

    [Fact]
    public void AverageGradeRule_平均不超1判合格()
    {
        var deltaEs = new[] { D(0.8), D(1.2), D(1.0) }; // 平均 = 1.0，恰好在合格线上

        Assert.Equal(MetrologyGrade.Qualified, AverageGradeRule.Judge(deltaEs).Grade);
    }

    [Fact]
    public void AverageGradeRule_平均在1到2之间降级()
    {
        var deltaEs = new[] { D(1.0), D(2.0), D(1.5) }; // 平均 = 1.5

        Assert.Equal(MetrologyGrade.Downgraded, AverageGradeRule.Judge(deltaEs).Grade);
    }

    [Fact]
    public void AverageGradeRule_平均超过2判报废()
    {
        var deltaEs = new[] { D(2.0), D(2.6), D(2.4) }; // 平均 = 2.333

        Assert.Equal(MetrologyGrade.Scrapped, AverageGradeRule.Judge(deltaEs).Grade);
    }

    [Fact]
    public void AverageGradeRule_空数据直接报错()
    {
        Assert.Throws<ArgumentException>(() => AverageGradeRule.Judge(Array.Empty<DeltaE>()));
    }

    // ---------- 值对象的不变量 ----------

    [Fact]
    public void DeltaE_拒绝负数和NaN()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DeltaE.FromComputed(-0.1));
        Assert.Throws<ArgumentException>(() => DeltaE.FromComputed(double.NaN));
    }

    [Fact]
    public void LabColor_CIE76色差计算正确()
    {
        var a = new LabColor(50, 2.5, 0);
        var b = new LabColor(73, 0, -25);

        // 经典算例：ΔE = √(23² + 2.5² + 25²) = √1160.25 ≈ 34.06
        Assert.Equal(34.06, a.DeltaETo(b).Value, precision: 2);
        // 色差对称：A 到 B 与 B 到 A 相等
        Assert.Equal(b.DeltaETo(a).Value, a.DeltaETo(b).Value);
    }

    private static DeltaE D(double v) => DeltaE.FromComputed(v);
}
