namespace AMP.Domain.Metrology;

/// <summary>
/// 一次检定会话中，对一块色砖的测量记录。
///
/// 它是值对象而不是实体：在同一会话内用"砖名"就能定位它，
/// 不需要额外的 ID（相对身份）。整条记录不可变——测量做完就是历史，
/// 要"重测"就在会话层面产生新的判定，而不是原地改这条记录
/// （老项目的通用病：结果集合被随手改，改到哪一步没人说得清）。
/// </summary>
public readonly record struct SwatchMeasurement(
    string SwatchName,
    LabColor Measured,
    LabColor Standard,
    GradingResult Result)
{
    /// <summary>这块砖的色差，等价于 Result.DeltaE 的便捷入口。</summary>
    public DeltaE DeltaE => Result.DeltaE;
}
