namespace Modules.Integration.Barrier.Base.Enums;

/// <summary>
/// Lệnh điều khiển barrier.
/// Dự án cũ CLVH (CommonFunctions.ChangeBarrrierState): newState == 0 -> MỞ, ngược lại -> ĐÓNG.
/// Dùng <see cref="BarrierCommandTypeExtensions.FromLegacyState"/> khi cần quy đổi từ trạng thái cũ.
/// </summary>
public enum BarrierCommandType
{
    Open = 1,      // Mở barie
    Close = 2,     // Đóng barie
    Stop = 3       // Dừng barie (PLC CLVH KHÔNG hỗ trợ — chưa có vùng nhớ)
}

/// <summary>Trạng thái barrier đọc được từ PLC.</summary>
public enum BarrierStatusType
{
    Unknown = 0,
    Opened = 1,    // Đang mở hoàn toàn
    Closed = 2,    // Đang đóng hoàn toàn
    Moving = 3,    // Đang chuyển động
    Fault = 4      // Lỗi
}

/// <summary>
/// Hãng thiết bị barrier. Mỗi hãng tương ứng 1 project driver Modules.Integration.Barrier.{Hãng}.
/// </summary>
public enum BarrierVendor
{
    /// <summary>PLC Siemens S7-1200 (dự án CLVH).</summary>
    SiemensS71200 = 1
}

public static class BarrierCommandTypeExtensions
{
    /// <summary>
    /// Quy đổi trạng thái kiểu cũ (cột OPTION1 / tham số option1 của CLVH) sang lệnh.
    /// Giữ nguyên logic cũ: 0 = mở, khác 0 = đóng.
    /// </summary>
    public static BarrierCommandType FromLegacyState(int legacyState)
        => legacyState == 0 ? BarrierCommandType.Open : BarrierCommandType.Close;
}
