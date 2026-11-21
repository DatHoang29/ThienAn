namespace ITS.Toll.Core.Models;

/// <summary>
/// Kết quả lưu dữ liệu API xuống DB (để form ghi log + đẩy watermark).
/// Received = tổng bản ghi xử lý; Inserted/Updated/Unchanged/Failed = phân loại kết quả.
/// Quy tắc CHỈ LƯU KHI THAY ĐỔI: bản ghi trùng và không đổi được tính vào Unchanged (không ghi DB).
/// </summary>
public sealed record TollSaveResult(
    bool Ok,
    string Message,
    int Received = 0,
    int Inserted = 0,
    int Updated = 0,
    int Unchanged = 0,
    int Failed = 0,
    int Deleted = 0);
