namespace Shared.QuestHa.Infrastructure.Services.Journal.Schema;

/// <summary>
/// Author: KienLT
/// Description: Một dòng đã chuyển kiểu, giá trị xếp đúng thứ tự <see cref="QuestTableSpec.Columns"/>.
/// <see cref="Seq"/> = Seq journal sinh ra dòng (0 nếu không biết).
/// Created date: 09/10/2026
/// </summary>
internal sealed class QuestRow
{
    /// <summary>Tạo dòng QuestDB với Seq journal và giá trị theo thứ tự cột của bảng.</summary>
    public QuestRow(long seq, object?[] values)
    {
        Seq = seq;
        Values = values;
    }

    public long Seq { get; }

    public object?[] Values { get; }
}
