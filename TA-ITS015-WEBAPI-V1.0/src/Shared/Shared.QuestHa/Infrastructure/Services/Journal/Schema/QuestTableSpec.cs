namespace Shared.QuestHa.Infrastructure.Services.Journal.Schema;

/// <summary>
/// Author: KienLT
/// Description: Mô tả bảng đích để chuyển payload journal → dòng QuestDB, tính EventKey/fingerprint.
/// Payload là JSON object, tên thuộc tính khớp tên cột (không phân biệt hoa thường).
/// Created date: 09/10/2026
/// </summary>
public sealed class QuestTableSpec
{
    private readonly Dictionary<string, int> _index;

    /// <summary>Định nghĩa bảng QuestDB được nhân bản; kiểm tra hợp lệ (đúng 1 designated timestamp, cột dedup, cột version) ngay khi tạo.</summary>
    public QuestTableSpec(string name, IReadOnlyList<QuestColumn> columns, string? versionColumn = null, int schemaVer = 1)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Table name is required.", nameof(name));
        if (columns.Count == 0) throw new ArgumentException("Columns are required.", nameof(columns));

        Name = name;
        Columns = columns;
        SchemaVer = schemaVer;
        _index = columns.Select((c, i) => (c.Name, i)).ToDictionary(x => x.Name, x => x.i, StringComparer.OrdinalIgnoreCase);

        var designated = columns.Where(c => c.IsDesignated).ToList();
        if (designated.Count != 1 || designated[0].Type != QuestColumnTypeEnum.Timestamp)
            throw new ArgumentException("Exactly one designated TIMESTAMP column is required.", nameof(columns));
        if (!designated[0].IsDedupKey)
            throw new ArgumentException("Designated timestamp must be part of DEDUP UPSERT KEYS.", nameof(columns));

        Designated = designated[0];
        DesignatedIndex = _index[Designated.Name];
        KeyIndexes = columns.Select((c, i) => (c, i)).Where(x => x.c.IsDedupKey).Select(x => x.i).ToArray();

        if (versionColumn is not null)
        {
            if (!_index.TryGetValue(versionColumn, out var vi))
                throw new ArgumentException($"Version column '{versionColumn}' not found.", nameof(versionColumn));
            if (columns[vi].Type is not (QuestColumnTypeEnum.Int or QuestColumnTypeEnum.Long))
                throw new ArgumentException("Version column must be INT or LONG.", nameof(versionColumn));
            VersionIndex = vi;
        }
    }

    public string Name { get; }

    public IReadOnlyList<QuestColumn> Columns { get; }

    public QuestColumn Designated { get; }

    public int DesignatedIndex { get; }

    /// <summary>Chỉ số các cột DEDUP key, theo thứ tự khai báo.</summary>
    public IReadOnlyList<int> KeyIndexes { get; }

    /// <summary>Cột version (bản sửa cùng khoá — version cao nhất thắng). -1 nếu không có.</summary>
    public int VersionIndex { get; } = -1;

    public int SchemaVer { get; }

    /// <summary>Vị trí cột theo tên (không phân biệt hoa thường); -1 nếu không có.</summary>
    public int IndexOf(string column) => _index.TryGetValue(column, out var i) ? i : -1;
}
