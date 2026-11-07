namespace AMP.Domain.Metrology;

/// <summary>
/// CIELAB 颜色空间中的一个颜色（L* a* b*）。
///
/// 为什么做成"值对象"而不是三个 double 到处传？
/// 老项目的做法是 Lab 值散落在 List&lt;float&gt; 矩阵里，谁都能改、改错了没人知道。
/// 值对象的三个特征：不可变、无身份（没有 ID）、按值判等——
/// 两个 L*a*b* 完全相同的颜色就是同一种颜色，不存在"这是另一个对象"的问题。
/// </summary>
public readonly record struct LabColor(double L, double A, double B)
{
    /// <summary>
    /// 与标准色比较，得到 CIE76 色差：ΔE = √(ΔL² + Δa² + Δb²)。
    /// 把"色差怎么算"放进领域，而不是放在 ViewModel 或算法工具类的静态方法里——
    /// 颜色自己最清楚如何与另一种颜色比较（领域行为归于领域对象）。
    /// 老项目口径一致（TristimulusAlgorithm 同公式）；若将来规程改用 CIEDE2000，
    /// 应新增方法而不是修改这里：已上线的判定逻辑不允许被悄悄改变（开闭原则）。
    /// </summary>
    public DeltaE DeltaETo(LabColor other)
    {
        double dl = L - other.L;
        double da = A - other.A;
        double db = B - other.B;
        return DeltaE.FromComputed(Math.Sqrt(dl * dl + da * da + db * db));
    }
}
