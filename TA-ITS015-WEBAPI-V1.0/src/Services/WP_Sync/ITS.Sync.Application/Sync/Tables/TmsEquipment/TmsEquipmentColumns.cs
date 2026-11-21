namespace ITS.Sync.Application.Sync;

/// <summary>
/// Khai báo cột KHÔNG đồng bộ (loại trừ) cho bảng TmsEquipment.
/// Cột trong danh sách này vừa không dùng để so sánh, vừa không bị ghi đè khi update.
/// </summary>
public static class TmsEquipmentColumns
{
    /// <summary>Cột loại trừ khi đồng bộ.</summary>
    public static readonly string[] Ignore = ["EquipmentVisible"];
}
