namespace ITS.Toll.Core.Contracts;

/// <summary>
/// Body cho POST /api/toll/import — TCS báo đã import dữ liệu của MỘT ngày; ITS tự pull lại đúng ngày đó.
/// Author: ChuongLP
/// Created date: 12/09/2026
/// Cập nhật 16/09/2026: bỏ FromDate/ToDate, chỉ dùng 1 field DateTime (lấy lại theo đúng ngày).
/// </summary>
public sealed class TollImportRequest
{
    /// <summary>Loại dữ liệu: TransactionIn | TransactionOut | OutSummary.</summary>
    public string? DataType { get; set; }

    /// <summary>
    /// Ngày cần lấy lại (CHỈ 1 ngày). ITS sẽ lấy lại toàn bộ dữ liệu trong ngày này (00:00:00 → 23:59:59).
    /// Nhận chuỗi để chấp nhận nhiều định dạng: "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss" (chuẩn hệ Toll)
    /// và ISO 8601 "yyyy-MM-ddTHH:mm:ss" (chỉ phần ngày được dùng để dựng khoảng cả ngày).
    /// </summary>
    public string? DateTime { get; set; }
}
