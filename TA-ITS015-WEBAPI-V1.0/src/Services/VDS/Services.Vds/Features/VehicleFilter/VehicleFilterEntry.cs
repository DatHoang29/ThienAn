using Shared.DTO.Enums;

namespace Services.Vds.Features.VehicleFilter;

/// <summary>
/// Author: KienLT
/// Description: Một dòng danh sách đen/trắng đã chuẩn hoá, dạng gọn để giữ trong RAM và ghi file cache —
/// không giữ cả entity với đủ cột audit.
///
/// Property có setter để đọc lại được từ file cache.
/// Created date: 26/08/2026
/// </summary>
public sealed class VehicleFilterEntry
{
    /// <summary>Id dòng cấu hình — gửi kèm bản tin để giao diện truy được về dòng danh sách.</summary>
    public string? FilterId { get; set; }

    /// <summary>Biển số ĐÃ CHUẨN HOÁ (bỏ dấu chấm/gạch/khoảng trắng, in hoa) — cũng là khoá tra.</summary>
    public string Plate { get; set; } = "";

    /// <summary>Biển số nguyên văn người vận hành khai — dùng để hiển thị.</summary>
    public string? RawPlate { get; set; }

    /// <summary>Loại danh sách (đen/trắng) theo mã web đang dùng.</summary>
    public string? Type { get; set; }

    /// <summary>Cờ cảnh báo nguyên văn; ý nghĩa do cấu hình quyết định (<c>AlarmOnValues</c>).</summary>
    public string? Alarm { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    /// <summary>Dòng đang bật. Dòng bị tắt vẫn giữ trong RAM nhưng không bao giờ khớp.</summary>
    public bool Enabled { get; set; }

    /// <summary>Ghi chú của người vận hành — giao diện dùng làm nội dung thông báo.</summary>
    public string? Remark { get; set; }

    public static VehicleFilterEntry From(string normalizedPlate, TmsVehicleFilter row) => new()
    {
        FilterId = row.ID,
        Plate = normalizedPlate,
        RawPlate = row.LicensePlate,
        Type = row.Type,
        Alarm = row.Alarm,
        StartDate = row.StartDate,
        EndDate = row.EndDate,
        Enabled = row.Status == BaseEnums.StatusEnum.Enable,
        Remark = row.Remark,
    };

    /// <summary>
    /// Dòng còn hiệu lực tại thời điểm xe đi qua. Phải đang bật; khoảng hiệu lực để trống nghĩa là không
    /// giới hạn đầu đó.
    ///
    /// <b>Ngày hết hiệu lực được hiểu là HẾT NGÀY đó</b>: web cho khai NGÀY (giờ 00:00), nếu so thẳng thì
    /// biển khai đến hôm nay sẽ chết ngay từ 0 giờ và người vận hành mất trọn một ngày hiệu lực.
    ///
    /// <para><b><paramref name="at"/> phải là GIỜ SERVER, không phải UTC.</b> <c>StartDate</c>/<c>EndDate</c>
    /// là ngày người vận hành khai trên web nên nằm ở giờ server; truyền mốc UTC vào đây làm cửa sổ hiệu lực
    /// dịch đúng số giờ của múi giờ máy chủ. Caller đang giữ mốc UTC thì quy đổi bằng
    /// <c>BaseUtil.TimeServer.ToServerTime</c> trước khi gọi.</para>
    /// </summary>
    public bool IsActiveAt(DateTime at)
    {
        if (!Enabled) return false;
        if (StartDate is DateTime start && at < start) return false;
        if (EndDate is DateTime end && at > EndOfDayIfMidnight(end)) return false;
        return true;
    }

    private static DateTime EndOfDayIfMidnight(DateTime value)
        => value.TimeOfDay == TimeSpan.Zero ? value.Date.AddDays(1).AddTicks(-1) : value;
}
