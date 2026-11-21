namespace Services.Vds.Shared.Messaging;

/// <summary>
/// Author: KienLT
/// Description: Subject NATS của VDS.
///
/// NGUỒN CHÂN LÝ cho các subject OUTBOUND là <c>ItsDataTransporterChannel</c>
/// (<c>Modules.DataTransporter.ITS</c>) — phía WebAPI dùng bộ hằng đó, và
/// <c>TAC_WebAPI/Configuration/DataTransporter.json</c> khai mode pubsub/jetstream theo từng subject.
/// Ở đây khai LẠI thành const thay vì reference trực tiếp, vì <c>Modules.DataTransporter.ITS</c> kéo
/// theo <c>Shared.Reference</c> (Furion + toàn bộ stack WebAPI) mà service cố tình không mang.
/// ⇒ ĐỔI GIÁ TRỊ Ở ĐÂY PHẢI ĐỔI CẢ HAI BÊN.
///
/// NATS core pub/sub không cần "đăng ký" subject để publish — subject chỉ là chuỗi trong bản tin.
/// Đăng ký thật chỉ xảy ra ở phía NHẬN.
/// Created date: 24/08/2026
/// </summary>
public static class VdsSubjects
{
    // ===== Outbound: dữ liệu VDS publish ra ngoài =====

    /// <summary>
    /// Dữ liệu realtime theo từng lượt xe: thiết bị, hướng, vùng, cỡ xe, tốc độ, thời điểm.
    /// <b>KHÔNG kèm biển số</b> — subject này là dữ liệu giao thông, không phải dữ liệu nhận dạng.
    /// Phát SAU khi ghi dữ liệu thô thành công, nên bên nhận không thấy bản tin mà DB chưa có.
    /// </summary>
    public const string Traffic = "ta.its.data.vds.traffic";

    /// <summary>Trạng thái vùng vừa đổi. Chỉ phát khi vùng ĐỔI trạng thái, không phát theo từng lượt xe.</summary>
    public const string Zone = "ta.its.data.vds.zone";

    /// <summary>Số liệu đếm xe theo chu kỳ.</summary>
    public const string Count = "ta.its.data.vds.count";

    /// <summary>
    /// Sự cố VDS dùng CHUNG subject sự cố của ITS, không có subject riêng
    /// <c>ta.its.data.vds.incident</c> — phía WebAPI đã subscribe subject này cho mọi nguồn sự cố.
    /// </summary>
    public const string Incident = "ta.its.data.incident";

    /// <summary>Số liệu thống kê chu kỳ do thiết bị VDS tự tổng hợp.</summary>
    public const string Statistic = "ta.its.data.vds.statistic";

    /// <summary>
    /// Cảnh báo biển số khớp danh sách đen/trắng có bật cảnh báo. Phát SAU khi đã ghi lịch sử nên bản
    /// tin luôn trỏ tới một dòng có thật.
    /// </summary>
    public const string Filter = "ta.its.data.vds.filter";

    // ===== Inbound: nhận yêu cầu nạp lại từ ngoài =====

    /// <summary>
    /// Subject DÙNG CHUNG toàn hệ để yêu cầu nạp lại dữ liệu. Payload:
    /// <see cref="Configuration.RefreshMessage"/> — <c>{ "code": n }</c>.
    ///
    /// <para>Bên gửi (web/BE) KHÔNG đẩy giá trị tham số xuống nữa; chỉ bắn một MÃ, service tự nạp lại
    /// mảng dữ liệu tương ứng TỪ DB rồi ghi xuống file cache và dùng ngay — không cần restart. Riêng mã
    /// cấu hình vận hành (<see cref="Configuration.RefreshCodes.OpConfig"/>) sẽ ghi đè ngưỡng tốc độ và
    /// chu kỳ thống kê xuống snapshot VDS.</para>
    ///
    /// Dùng chung nhiều module nên mỗi service chỉ xử lý MÃ nó biết; mã lạ thì bỏ qua.
    /// Bảng mã của VDS: xem <see cref="Configuration.RefreshCodes"/>.
    /// </summary>
    public const string Refresh = "ta.its.refresh";

    // ===== Giám sát dịch vụ =====

    /// <summary>
    /// Heartbeat của service VDS — mỗi chu kỳ (mặc định 30s) phát <c>{ "data": "ok" }</c> để bên
    /// heartbeat/FE tính VDS Online/Offline, cùng kiểu <c>ta.its.heartbeat.vms</c>/<c>.toll</c>. Phát bằng
    /// <c>Services.Shared.Messaging.HeartbeatPublisher</c>; mục <c>Heartbeat</c> trong DataTransporter.json chỉ để GHI ĐÈ
    /// (tắt, đổi subject/chu kỳ) — không khai vẫn chạy với subject này.
    /// </summary>
    public const string Heartbeat = "ta.its.heartbeat.vds";
}
