namespace Modules.VDS.Infrastructure.Services.Vehicle.Plates;

/// <summary>
/// Author: KienLT
/// Description: Chuẩn hoá biển số cho truy vấn hành trình VDS: bỏ khoảng trắng, dấu chấm,
/// gạch ngang rồi chuyển sang chữ hoa. Lịch sử lọc phương tiện do TMS sở hữu từ Phase 05.
/// Created date: 21/09/2026
/// </summary>
public static class VdsPlate
{
    /// <summary>Biển số đã chuẩn hoá; null/rỗng ⇒ chuỗi rỗng.</summary>
    public static string Normalize(string? plate) => new string((plate ?? "")
        .Where(c => !char.IsWhiteSpace(c) && c != '.' && c != '-').ToArray()).ToUpperInvariant();
}
