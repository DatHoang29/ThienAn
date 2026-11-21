namespace Services.Vds.Shared.Domain;

/// <summary>
/// Author: KienLT
/// Description: Mốc NGÀY theo GIỜ SERVER, quy về UTC để so với <c>detectTime</c> (cột UTC trên QuestDB).
///
/// <para><b>Vì sao không dùng <c>nowUtc.Date</c></b>: giờ Việt Nam là UTC+7, nên nửa đêm UTC không phải nửa
/// đêm ở đây. Dùng <c>nowUtc.Date</c> thì "hôm nay" hụt 7 tiếng đầu ngày, và từ 00:00 tới 07:00 giờ VN nó
/// còn trỏ sang NGÀY HÔM TRƯỚC — làm chuỗi theo giờ vắt qua nửa đêm và nhãn trục X lặp lại giờ của hai ngày
/// khác nhau. Chart TMS (<c>TrafficDataToDayChart</c>) dùng <c>DateTime.Now.Date</c> nên đây cũng là cách
/// duy nhất để hai bên cùng hiểu "hôm nay" giống nhau.</para>
///
/// <para><b>Vì sao ở <c>Shared</c></b>: hai tính năng dùng thật — bản tin đếm theo camera
/// (<c>CountPayloadBuilder</c>) và bản tin lưu lượng theo vùng (<c>ZoneTrafficPayloadBuilder</c>). Trước đây
/// mỗi bên giữ một bản sao y hệt; sửa một bên là hai subject <c>count</c>/<c>traffic</c> hiểu "hôm nay"
/// khác nhau — lệch số mà payload không có dấu hiệu gì.</para>
///
/// Hàm THUẦN theo <c>TimeZoneInfo.Local</c> của máy chạy service.
/// Created date: 04/09/2026
/// </summary>
public static class ServerDay
{
    /// <summary>
    /// 00:00 của ngày HÔM NAY theo giờ server, trả về dạng UTC.
    ///
    /// Khoảng đọc QuestDB không phình thêm: mốc này lùi tối đa gần 24 tiếng, vẫn nằm trong cửa sổ 24h mà
    /// bên gọi đang dùng để quyết định mốc đọc.
    /// </summary>
    public static DateTime StartOfTodayUtc(DateTime nowUtc)
    {
        var utc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
        var localMidnight = DateTime.SpecifyKind(
            TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.Local).Date, DateTimeKind.Unspecified);

        // Giờ VN không có DST nên nửa đêm luôn tồn tại. Vẫn chặn: ConvertTimeToUtc NÉM với thời điểm nằm
        // trong khe DST, và một bản tin đếm không được chết vì múi giờ của máy chạy.
        return TimeZoneInfo.Local.IsInvalidTime(localMidnight)
            ? utc.Date
            : TimeZoneInfo.ConvertTimeToUtc(localMidnight, TimeZoneInfo.Local);
    }
}
