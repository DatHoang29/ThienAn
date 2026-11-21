namespace Services.Vds.Features.Events.Automation;

/// <summary>
/// Author: KienLT
/// Description: Gọi API của WebAPI ITS để nhờ TMS xử lý tiếp sự cố VDS vừa ghi
/// (section <c>"Service:TmsApi"</c>). Hiện dùng cho đúng MỘT việc: lập lịch kịch bản tự động
/// (<c>POST /api/tms/tmsIncidentAutomationRun/ScheduleForIncident</c>).
///
/// <para>
/// <b>VDS vẫn TỰ ghi sự cố</b>, không chuyển việc tạo sự cố sang API. Lý do: DTO tạo sự cố của đường tích
/// hợp không mang được <c>EventId</c> (khoá chống trùng durable), <c>Metadata</c> (nguồn thay token
/// <c>{VehiclePlate}</c>/<c>{VehicleSpeed}</c> vào nội dung VMS) và danh sách camera xem được mà
/// <c>IncidentPromoter</c> đang dựng. Đi qua API sẽ mất cả ba.
/// </para>
///
/// <para>
/// <b>Xác thực.</b> WebAPI chặn xác thực toàn bộ <c>/api/*</c> nên phải có Bearer token. Hai cách:
/// <list type="number">
/// <item>Khai sẵn <see cref="Token"/> — token dài hạn của tài khoản dịch vụ, không đăng nhập.</item>
/// <item>Khai <see cref="Username"/> + <see cref="Password"/> — client tự đăng nhập, giữ token trong RAM
/// và đăng nhập lại khi gặp 401.</item>
/// </list>
/// KHÔNG commit mật khẩu thật vào <c>appsettings.json</c>: đặt ở <c>appsettings.{Environment}.json</c>
/// ngoài repo, hoặc biến môi trường <c>Service__TmsApi__Password</c>.
/// </para>
/// Created date: 26/08/2026
/// </summary>
public sealed class TmsApiOptions
{
    /// <summary>
    /// Bật/tắt việc gọi API. Mặc định TẮT để môi trường chưa khai tài khoản không đập lỗi 401 mỗi sự cố.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>Gốc URL của WebAPI, ví dụ <c>https://its.example.vn</c> (không cần dấu / cuối).</summary>
    public string BaseUrl { get; set; } = "";

    /// <summary>
    /// Author: KienLT - 12/11/2026
    /// Description: Bỏ qua kiểm tra chứng chỉ HTTPS của WebAPI (tự ký, hoặc cấp cho tên miền nhưng <see cref="BaseUrl"/>
    /// khai bằng IP ⇒ <c>RemoteCertificateNameMismatch</c>). Mặc định TẮT; chỉ bật ở môi trường dev/nội bộ.
    /// </summary>
    public bool AllowSelfSignedCert { get; set; }

    /// <summary>Đường dẫn endpoint lập lịch tự động.</summary>
    public string SchedulePath { get; set; } = "/api/tms/tmsIncidentAutomationRun/ScheduleForIncident";

    /// <summary>
    /// Đường dẫn đăng nhập lấy token. Hợp đồng login nằm trong <c>Modules.System.dll</c> (repo không có
    /// source) nên để CẤU HÌNH được: đổi đường dẫn không phải build lại.
    /// </summary>
    public string LoginPath { get; set; } = "/api/system/sysauth/login";

    /// <summary>
    /// State kích hoạt gửi kèm — phải khớp <c>TmsIncidentAutomationConfig.TriggerState</c>. Sự cố VDS sinh
    /// ra ở trạng thái đăng ký mới nên mặc định là <c>register</c>.
    /// </summary>
    public string TriggerState { get; set; } = "register";

    /// <summary>Định danh gửi kèm để TMS ghi vào bản tin realtime.</summary>
    public string Actor { get; set; } = "VDS";

    /// <summary>Bearer token dài hạn. Có giá trị thì KHÔNG đăng nhập.</summary>
    public string Token { get; set; } = "";

    public string Username { get; set; } = "";

    public string Password { get; set; } = "";

    /// <summary>Timeout mỗi lượt gọi (giây).</summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Trần số sự cố chờ gọi API trong RAM. Đầy thì BỎ việc mới thay vì chặn pipeline: sự cố đã nằm trong
    /// DB và người trực vẫn xử lý thủ công được, còn mất nhịp pipeline là mất dữ liệu đo đếm.
    /// </summary>
    public int QueueCapacity { get; set; } = 500;

    /// <summary>Số lần thử lại khi lỗi mạng/5xx (0 = không thử lại). Lỗi nghiệp vụ KHÔNG thử lại.</summary>
    public int MaxRetries { get; set; } = 2;

    /// <summary>Giãn cách giữa hai lần thử lại (giây).</summary>
    public int RetryDelaySeconds { get; set; } = 5;

    /// <summary>Thời gian chờ gọi hết các sự cố còn trong hàng đợi khi dừng service (giây).</summary>
    public int ShutdownDrainSeconds { get; set; } = 10;

    /// <summary>
    /// Giữ danh sách sự cố CHƯA bàn giao lệnh lập lịch trong bao nhiêu PHÚT (file
    /// <c>incident-dispatch-pending.json</c>, sống qua restart). Cũ hơn thế thì BỎ, không gọi nữa: kịch bản
    /// tự động cho một sự việc đã qua lâu không còn ý nghĩa điều hành.
    /// </summary>
    public int DispatchRetentionMinutes { get; set; } = 60;

    /// <summary>
    /// Author: KienLT - 14/10/2026
    /// Description: Danh sách TRẮNG loại sự kiện (<c>TmsEventType.Code</c>) được gửi sang TMS (lập lịch tự động / tự
    /// kết thúc khi không ai nhận); rỗng ⇒ không loại nào. Từ 17/10/2026 SUY TỪ cấu hình tự động TMS (OpConfigManager
    /// nạp, xem <c>VdsCatalogRepository.QueryAutomationEventTypesAsync</c>), không khai tay ở đâu nữa. Chỉ là bộ lọc để
    /// bớt lượt gọi; TMS vẫn quyết định cuối cùng.
    /// </summary>
    public List<string> AutomationEventTypes { get; set; } = new();

    public int EffectiveTimeoutSeconds => Math.Clamp(TimeoutSeconds, 1, 120);
    public int EffectiveQueueCapacity => Math.Max(16, QueueCapacity);
    public int EffectiveMaxRetries => Math.Clamp(MaxRetries, 0, 10);
    public int EffectiveRetryDelaySeconds => Math.Clamp(RetryDelaySeconds, 1, 300);
    public int EffectiveShutdownDrainSeconds => Math.Clamp(ShutdownDrainSeconds, 0, 120);
    public int EffectiveDispatchRetentionMinutes => Math.Clamp(DispatchRetentionMinutes, 1, 1_440);

    /// <summary>Đã khai đủ để gọi: bật cờ, có URL gốc, có đường dẫn và có ít nhất một cách xác thực.</summary>
    public bool IsUsable =>
        Enabled
        && !string.IsNullOrWhiteSpace(BaseUrl)
        && !string.IsNullOrWhiteSpace(SchedulePath)
        && HasCredential;

    /// <summary>Có token sẵn, hoặc có đủ tài khoản/mật khẩu để tự đăng nhập.</summary>
    public bool HasCredential =>
        !string.IsNullOrWhiteSpace(Token)
        || (!string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password));

    /// <summary>Loại sự kiện này có nằm trong danh sách được nhờ TMS lập lịch tự động không.</summary>
    public bool AllowsAutomationFor(string? eventTypeCode)
        => !string.IsNullOrWhiteSpace(eventTypeCode)
           && AutomationEventTypes.Contains(eventTypeCode.Trim(), StringComparer.OrdinalIgnoreCase);

    /// <summary>Ghép URL tuyệt đối từ <see cref="BaseUrl"/> và một đường dẫn; sai định dạng ⇒ null.</summary>
    public Uri? BuildUri(string? path)
    {
        if (string.IsNullOrWhiteSpace(BaseUrl) || string.IsNullOrWhiteSpace(path)) return null;

        return Uri.TryCreate($"{BaseUrl.TrimEnd('/')}/{path.TrimStart('/')}", UriKind.Absolute, out var uri)
            ? uri
            : null;
    }

    public TmsApiOptions Clone() => new()
    {
        Enabled = Enabled,
        BaseUrl = BaseUrl,
        AllowSelfSignedCert = AllowSelfSignedCert,
        SchedulePath = SchedulePath,
        LoginPath = LoginPath,
        TriggerState = TriggerState,
        Actor = Actor,
        Token = Token,
        Username = Username,
        Password = Password,
        TimeoutSeconds = TimeoutSeconds,
        QueueCapacity = QueueCapacity,
        MaxRetries = MaxRetries,
        RetryDelaySeconds = RetryDelaySeconds,
        ShutdownDrainSeconds = ShutdownDrainSeconds,
        DispatchRetentionMinutes = DispatchRetentionMinutes,
        AutomationEventTypes = new List<string>(AutomationEventTypes),
    };
}
