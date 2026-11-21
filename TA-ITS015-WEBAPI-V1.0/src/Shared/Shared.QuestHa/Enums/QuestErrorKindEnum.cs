namespace Shared.QuestHa.Enums;

/// <summary>
/// Author: KienLT
/// Description: Kết quả phân loại lỗi.
/// Created date: 09/10/2026
/// </summary>
internal enum QuestErrorKindEnum
{
    /// <summary>Không tới được node (socket, timeout, "Connection is not open") ⇒ backoff, giữ cursor.</summary>
    Connectivity,
    /// <summary>Lỗi của riêng dòng dữ liệu (sai kiểu, payload hỏng) ⇒ Quarantine, đi tiếp.</summary>
    Data,
    /// <summary>Lỗi phía node/schema (thiếu bảng, WAL suspend) ⇒ Fenced, dừng giao, không quarantine.</summary>
    Node,
}
