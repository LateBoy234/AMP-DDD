namespace AMP.Domain.Metrology;

/// <summary>
/// 一次判定的结论：等级 + 得出该等级所依据的 ΔE。
///
/// 为什么结论要和证据绑在一起？
/// 老项目里"合格/不合格"常常只是一个 UI 状态，用户看不到是按哪个 ΔE 判的，
/// 出了质量争议无法追溯。让证据成为结论的一部分，追溯就变成读字段，而不是翻日志。
/// </summary>
public readonly record struct GradingResult(MetrologyGrade Grade, DeltaE DeltaE);
