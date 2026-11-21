namespace ITS.Sync.Application.Sync;

/// <summary>
/// Khai báo cột KHÔNG đồng bộ (loại trừ) cho bảng TollPriceList.
/// Để trống nghĩa là đồng bộ toàn bộ cột nội dung.
/// Author: HieuNV
/// Created date: 07/10/2026
/// </summary>
public static class TollPriceListColumns
{
    /// <summary>Cột loại trừ khi đồng bộ.</summary>
    public static readonly string[] Ignore = [];
}
