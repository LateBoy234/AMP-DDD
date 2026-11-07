namespace AMP.Domain.Motion;

/// <summary>
/// 三维目标位置，X、Y、Z 的单位统一为毫米。
///
/// 为什么需要这个类型？
/// 如果各处都传三个 double，编译器无法区分它们代表坐标、速度还是脉冲。
/// 将三个坐标组合成一个有业务含义的对象，方法签名就可以表达“移动到某个位置”。
/// 厂商的轴号、脉冲当量和通信方式不属于位置本身，留给后续设备适配层处理。
/// </summary>
/// <remarks>
/// 这是“值对象”：没有独立 ID，三个坐标相同就表示同一个位置。
/// record 提供按值判等；sealed 表示当前不需要通过继承扩展位置的含义。
///
/// 这里选择 class 而不是 struct，是为了让每个非 null 实例都经过构造校验。
/// struct 的 default 值会绕过构造函数。class 仍可能为 null，后续使用它的方法
/// 应在入口检查 null；值对象不能替调用方解决所有问题。
/// </remarks>
public sealed record Position3D
{
    // 只有 get：位置创建后不可修改，也不能通过 with { X = ... } 绕过校验。
    // 需要另一个位置时，显式创建另一个对象，原有测量点和记录不受影响。
    public double X { get; }
    public double Y { get; }
    public double Z { get; }

    /// <summary>
    /// 用毫米创建位置。只接受有限数值，拒绝 NaN 和正负无穷。
    ///
    /// 为什么在构造时检查？让非法坐标在进入业务流程之前暴露，避免每个使用者重复检查。
    /// 为什么不限制为正数？负坐标是否有效取决于原点和设备行程，不能在这里任意规定。
    /// 当前类型也不保证“设备一定能到达”：设备行程、限位和坐标系转换是后续独立的规则。
    /// </summary>
    public Position3D(double x, double y, double z)
    {
        if (!double.IsFinite(x))
            throw new ArgumentOutOfRangeException(nameof(x), x, "X 坐标必须是有限数值，单位为毫米。");
        if (!double.IsFinite(y))
            throw new ArgumentOutOfRangeException(nameof(y), y, "Y 坐标必须是有限数值，单位为毫米。");
        if (!double.IsFinite(z))
            throw new ArgumentOutOfRangeException(nameof(z), z, "Z 坐标必须是有限数值，单位为毫米。");

        // 三处校验暂时直接展开，便于阅读；此时还不需要通用校验框架或基类。
        X = x;
        Y = y;
        Z = z;
    }
}
