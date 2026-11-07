namespace AMP.Domain.Metrology;

/// <summary>
/// 检定会话的生命周期状态。
///
/// 为什么只有两个状态、而不是老项目跑批的九态机？
/// 这是"业务会话"的状态，不是"运动平台"的状态。
/// 平台的 MovingXY/PilingDown/ReturningZ 属于硬件编排层（后续课程），
/// 会话只关心业务事实：进行中，还是已出结论。状态分错了层，
/// 就会出现老项目那种 93KB 的 ShellViewModel——把两套状态机揉在一起。
/// </summary>
public enum MeasurementSessionStatus
{
    /// <summary>进行中：可以继续录砖、录组。</summary>
    InProgress = 0,

    /// <summary>已完成：结论已出，记录不可再变（审计要求）。</summary>
    Completed = 1,
}
