namespace AMP.Domain.Metrology;

/// <summary>
/// 被检仪器的身份：型号 + 序列号。
///
/// 为什么单独建模而不在会话上放两个 string 字段？
/// 老项目 IndicationErrorMeasurement 里 InstrumentModel/SN 是两个裸字符串，
/// 空串、空白、拼错都畅通无阻，直到入库去重时才发现 SN 是空导致数据错乱。
/// 身份信息在出生点校验一次，全系统就再也不用怀疑它的合法性。
/// </summary>
public readonly record struct InstrumentIdentity
{
    /// <summary>仪器型号，如 NR20XE。</summary>
    public string Model { get; }

    /// <summary>仪器序列号——计量追溯的最小单位，同一型号的每台仪器命运不同。</summary>
    public string SerialNumber { get; }

    public InstrumentIdentity(string model, string serialNumber)
    {
        if (string.IsNullOrWhiteSpace(model))
            throw new ArgumentException("仪器型号不能为空。", nameof(model));
        if (string.IsNullOrWhiteSpace(serialNumber))
            throw new ArgumentException("仪器序列号不能为空：没有 SN 的计量记录无法追溯到具体仪器。", nameof(serialNumber));

        Model = model.Trim();
        SerialNumber = serialNumber.Trim();
    }

    public override string ToString() => $"{Model} #{SerialNumber}";
}
