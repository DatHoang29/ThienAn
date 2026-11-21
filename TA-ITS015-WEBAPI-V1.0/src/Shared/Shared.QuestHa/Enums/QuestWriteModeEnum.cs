namespace Shared.QuestHa.Enums;

/// <summary>
/// Author: KienLT
/// Description: Cách Services.Vds ghi dữ liệu QuestDB (triển khai từng bước, có đường quay lại).
/// Created date: 27/10/2026
/// </summary>
public enum QuestWriteModeEnum
{
    /// <summary>Như hiện nay: ghi thẳng 1 node QuestDB, không journal. Mặc định.</summary>
    Direct,

    /// <summary>Ghi thẳng node chính như cũ VÀ ghi journal; replicator chỉ giao sang node phụ để so sánh.</summary>
    Shadow,

    /// <summary>Chỉ ghi journal (ACK = commit SQL); replicator giao cho mọi node.</summary>
    Journal,
}
