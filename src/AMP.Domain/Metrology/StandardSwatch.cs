namespace AMP.Domain.Metrology;

/// <summary>
/// 标准色砖：砖名 + 出厂标定的标准 Lab 值。
///
/// 注意：它只有"标准是什么"，没有"测出来是什么"——
/// 测量结果属于某一次具体的检定会话（MeasurementSession），不属于色砖本身。
/// 把"物"和"对物的测量记录"分开，是实体/值对象划分的基本功：
/// 色砖用十年不换，测量记录每天产生一批，两者生命周期完全不同。
/// </summary>
public readonly record struct StandardSwatch
{
    /// <summary>色砖名称，如"01 白"。在一次计量标准内唯一。</summary>
    public string Name { get; }

    /// <summary>出厂标定的标准 Lab 值。</summary>
    public LabColor StandardLab { get; }

    public StandardSwatch(string name, LabColor standardLab)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("色砖名称不能为空。", nameof(name));

        Name = name.Trim();
        StandardLab = standardLab;
    }
}
