namespace ITS.PA.Core.Configuration;

/// <summary>
/// Cấu hình dịch vụ PA (section "Service:Pa"). Ở ITS008 các giá trị này nằm rải rác: IP lấy từ thiết bị
/// trong DB, tài khoản/cổng từ Web.config (USER_FM, PASS_FM, PORT_FM), nhóm/âm lượng từ APP_PARAMS
/// (PA_EVENT_GROUP, PA_SOUND_TEST_GROUP, PA_VOLUME), còn lại gán cứng trong code. Nay gom về đây.
/// Author: ChuongLP
/// Created date: 31/10/2026
/// </summary>
public sealed class PaOptions
{
    public const string SectionName = "Service:Pa";

    /// <summary>IP máy chủ PA. Cũ: IP_ADDRESS của thiết bị.</summary>
    public string Ip { get; set; } = string.Empty;

    /// <summary>Cổng API. Cũ: PORT_FM (mặc định của lớp API là 81).</summary>
    public int Port { get; set; } = 81;

    /// <summary>Cũ: USER_FM.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Cũ: PASS_FM. Đặt qua biến môi trường, không ghi vào file.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Tiền tố đường dẫn API. Cũ: "api/v29+".</summary>
    public string ApiPath { get; set; } = "api/v29+";

    /// <summary>Mã công ty trong gói forwarder. Cũ: "BL".</summary>
    public string Company { get; set; } = "BL";

    /// <summary>Cũ: client.Timeout = 20 giây.</summary>
    public int TimeoutSeconds { get; set; } = 20;

    /// <summary>Nhóm loa phát khi có sự cố. Cũ: APP_PARAMS PA_EVENT_GROUP (mặc định "EVENT_GROUP").</summary>
    public string EventGroup { get; set; } = "EVENT_GROUP";

    /// <summary>Nhóm loa phát khi kiểm âm. Cũ: APP_PARAMS PA_SOUND_TEST_GROUP (mặc định "SOUND_TEST_GROUP").</summary>
    public string SoundTestGroup { get; set; } = "SOUND_TEST_GROUP";

    /// <summary>Âm lượng. Cũ: APP_PARAMS PA_VOLUME (mặc định 70).</summary>
    public int Volume { get; set; } = 70;

    /// <summary>Độ ưu tiên tác vụ. Cũ: gán cứng 100.</summary>
    public int Priority { get; set; } = 100;

    /// <summary>Chế độ phát. Cũ: gán cứng "list_cycle_mode".</summary>
    public string PlayMode { get; set; } = "list_cycle_mode";

    /// <summary>Trạng thái tác vụ coi là phát thành công. Cũ: "task_process_work".</summary>
    public string WorkingTaskStatus { get; set; } = "task_process_work";

    /// <summary>
    /// Độ dài tiền tố mã sự cố dùng để dừng các tác vụ cũ của cùng loại sự cố (cũ: IncidentCode.Substring(0, 6),
    /// ví dụ "inc_xx"). 0 hoặc mã ngắn hơn thì dùng nguyên mã.
    /// </summary>
    public int StopTaskPrefixLength { get; set; } = 6;
}
