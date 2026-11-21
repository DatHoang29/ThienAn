using System.Text.Json;

namespace ITS.Toll.Core.Contracts;

/// <summary>
/// Body cho POST /api/toll/record — TCS đẩy 1 bản ghi kèm thao tác.
/// Author: ChuongLP
/// Created date: 12/09/2026
/// </summary>
public sealed class TollRecordRequest
{
    /// <summary>Loại dữ liệu: TransactionIn | TransactionOut | OutSummary.</summary>
    public string? DataType { get; set; }

    /// <summary>Thao tác: Insert | Update | Delete.</summary>
    public string? Operation { get; set; }

    /// <summary>Object bản ghi TCS (đúng tên field như response hiện tại). Bắt buộc với Insert/Update.</summary>
    public JsonElement? Data { get; set; }

    /// <summary>
    /// Id bên TCS dùng cho Delete (Transaction_ID với transaction, id với summary).
    /// Tùy chọn: nếu bỏ trống, hệ thống tự rút từ <see cref="Data"/> theo khóa của DataType.
    /// </summary>
    public string? SourceId { get; set; }
}
