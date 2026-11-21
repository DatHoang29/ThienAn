namespace Modules.Integration.Barrier.Base.Helpers;

/// <summary>
/// Phân tích địa chỉ vùng nhớ Siemens dạng "DB{n}.DBX{byte}.{bit}" (vd: DB1.DBX0.1).
/// Tương đương clsPLC_S71200.fn_SplitData_PLCS71200 của DLL cũ.
/// Đặt ở Base để WPF có thể validate địa chỉ người dùng nhập trước khi gửi lệnh.
/// Author: ChuongLP
/// Date: 05-10-2026
/// </summary>
public static class IntgBarrierAddressHelper
{
    public static bool TryParseDbBit(string? address, out int db, out int startByte, out int bit)
    {
        db = 0; startByte = 0; bit = 0;
        if (string.IsNullOrWhiteSpace(address)) return false;

        var parts = address.Trim().ToUpperInvariant().Split('.');
        if (parts.Length != 3) return false;
        if (!parts[0].StartsWith("DB") || !parts[1].StartsWith("DBX")) return false;

        return int.TryParse(parts[0].AsSpan(2), out db)
            && int.TryParse(parts[1].AsSpan(3), out startByte)
            && int.TryParse(parts[2], out bit)
            && db > 0 && startByte >= 0 && bit is >= 0 and <= 7;
    }
}
