using Modules.VDS.Core.Exceptions;

namespace Modules.VDS.Infrastructure.Services.TrafficData.Rules;

/// <summary>
/// Author: KienLT
/// Description: Miền hợp lệ của KHÓA KÉP tra một bản tin VDS: <c>vendorEventId</c> + <c>detectTime</c>.
///
/// <para><b>Vì sao cần lớp này.</b> Ba API chi tiết (<c>vdsEvent/Detail</c>, <c>vdsTrafficData/Detail</c>,
/// <c>vdsMedia/File</c>) nhận khóa này và trước 23/09/2026 dựa vào <c>[Required]</c> đặt trên <c>long</c> và
/// <c>DateTime</c>. <b>Attribute đó không chặn được gì:</b> value type không nullable thì LUÔN có giá trị, nên
/// query thiếu <c>eventId</c> chỉ cho ra <c>0</c> và thiếu <c>detectTime</c> cho ra
/// <c>0001-01-01T00:00:00</c> — cả hai đi thẳng xuống câu truy vấn và trả về "không tìm thấy bản ghi", tức là
/// người dùng bị chỉ sai nguyên nhân.</para>
///
/// <para><b>Miền của <c>detectTime</c>: phải khác <c>default</c>.</b> Cột được worker ghi từ mốc phát hiện của
/// thiết bị; dữ liệu thật sớm nhất trong <c>TmsTrafficData</c> là 01/01/2025, nên
/// <c>0001-01-01T00:00:00</c> chỉ có thể là "client không gửi tham số".</para>
///
/// <para><b>Miền của <c>eventId</c>: chỉ chặn số ÂM, KHÔNG chặn 0.</b> Đây là chỗ dễ làm sai: đường ghi
/// (<c>VdsFrameProcessor</c>) loại frame có <c>id &lt;= 0</c>, nên đọc code sẽ tưởng phía đọc được phép đòi
/// <c>&gt; 0</c>. Nhưng đo dữ liệu thật ngày 23/09/2026 thì <c>VendorEventId = 0</c> CÓ TỒN TẠI:
/// 40/342.034 dòng <c>TmsTrafficData</c> và 28/448.939 dòng <c>TmsTrafficStatistic</c> (mốc phát hiện từ
/// 01/01/2025 đến 19/08/2026) — dữ liệu có trước khi cửa chặn kia được thêm. Chặn <c>&gt; 0</c> sẽ khóa luôn
/// đường xem chi tiết của đúng những bản ghi đó. Số âm thì không có dòng nào, và cũng không có cách nào sinh
/// ra, nên chặn được.</para>
///
/// <para>Trả về KHÓA localization thay vì ném <c>Oops.Oh</c> để lớp này không cần Furion và để người gọi tự
/// chọn cách báo lỗi — API media chẳng hạn phải trả <c>ProblemDetails</c> chứ không ném.</para>
/// Created date: 23/09/2026
/// </summary>
public static class VdsEventKeyRules
{
    /// <summary>Khóa localization của lỗi đầu tiên tìm thấy, hoặc <c>null</c> nếu khóa dùng được.</summary>
    public static string? Validate(long eventId, DateTime detectTime)
    {
        if (detectTime == default) return BaseMsg.Vds.Exception.DetectTimeRequired;
        if (eventId < 0) return BaseMsg.Vds.Exception.EventIdInvalid;
        return null;
    }
}
