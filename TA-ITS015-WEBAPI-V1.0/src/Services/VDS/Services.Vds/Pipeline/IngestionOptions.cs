using System.Net.Http.Headers;

namespace Services.Vds.Pipeline;

/// <summary>
/// Author: KienLT
/// Description: Cấu hình biên nhận dữ liệu VDS và local queue tuần tự.
/// Created date: 25/08/2026
/// </summary>
public sealed class IngestionOptions
{
    /// <summary>
    /// Chỉ bật nguồn SSE thật. Cửa <c>IVdsFrameInjector</c> vẫn dùng được khi giá trị này false để WPF
    /// harness và smoke test bơm frame mà không mở kết nối ra ngoài.
    /// </summary>
    public bool SseEnabled { get; set; }

    public string ApiBaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public bool UseBearerAuth { get; set; }

    /// <summary>
    /// Cho phép HTTP không mã hóa. Mặc định false để API key và payload giao thông không đi dạng cleartext.
    /// Chỉ bật trên mạng nội bộ đã có lớp bảo vệ tương đương.
    /// </summary>
    public bool AllowInsecureHttp { get; set; }

    /// <summary>
    /// Bỏ kiểm tra chứng thư HTTPS. Mặc định false; chỉ bật tạm cho endpoint nội bộ dùng cert tự ký.
    /// </summary>
    public bool AllowSelfSignedCert { get; set; }

    public string StreamPath { get; set; } = "/api/v1/events/stream";
    public string Sources { get; set; } = string.Empty;
    public int SettleSeconds { get; set; } = 10;
    public int QueueCapacity { get; set; } = 10_000;

    /// <summary>
    /// Ghi nguyên văn mỗi frame nhận được ra file, tách theo ngày.
    ///
    /// Cần vì pipeline loại bỏ khá nhiều thứ (JSON sai, camera lạ, field không có cột tương ứng); khi
    /// phải đối chiếu "thiết bị có gửi cái đó không" thì chỉ file thô trả lời được. Chỉ ghi frame LIVE,
    /// không ghi frame bù lịch sử — bù là đọc lại quá khứ, ghi vào đây sẽ làm sai số liệu đối chiếu.
    /// </summary>
    public bool RawLogEnabled { get; set; } = true;

    /// <summary>Thư mục log frame thô; tương đối thì neo vào thư mục chứa exe.</summary>
    public string RawLogDirectory { get; set; } = "logs/vds-raw";

    /// <summary>Giới hạn ký tự của một frame để SSE/injector không giữ chuỗi vô hạn trong RAM.</summary>
    public int MaxFrameCharacters { get; set; } = 1_000_000;

    public int ConnectTimeoutSeconds { get; set; } = 30;
    public int SseIdleTimeoutSeconds { get; set; } = 120;
    public int EffectiveConnectTimeoutSeconds => Math.Clamp(ConnectTimeoutSeconds, 1, 300);
    public int EffectiveSseIdleTimeoutSeconds => Math.Clamp(SseIdleTimeoutSeconds, 5, 3600);

    public int ReconnectBackoffInitialMs { get; set; } = 500;
    public int ReconnectBackoffMaxSeconds { get; set; } = 30;

    /// <summary>
    /// Độ dài tối thiểu của biển ĐÃ CHUẨN HOÁ để lượt xe được đưa vào cửa chống trùng. Ngắn hơn ⇒ coi như
    /// không đọc được biển và KHÔNG chặn.
    ///
    /// Đây là van an toàn chống sai HỤT: chuỗi rác một hai ký tự rất dễ trùng nhau giữa các xe khác nhau.
    /// </summary>
    public int VehicleDedupMinPlateLength { get; set; } = 4;

    /// <summary>Trần số khoá (camera + loại + biển) mà cửa chống trùng giữ trong RAM.</summary>
    public int VehicleDedupMaxKeys { get; set; } = 20_000;

    public int EffectiveSettleSeconds => Math.Clamp(SettleSeconds, 0, 60);
    public int EffectiveQueueCapacity => Math.Max(16, QueueCapacity);
    public int EffectiveMaxFrameCharacters => Math.Clamp(MaxFrameCharacters, 1_024, 10_000_000);
    public int EffectiveReconnectInitialMs => Math.Max(100, ReconnectBackoffInitialMs);
    public int EffectiveReconnectMaxSeconds => Math.Max(1, ReconnectBackoffMaxSeconds);

    // Cửa sổ chống trùng lượt xe KHÔNG còn khai ở đây: nó lấy theo TỪNG LOẠI bản tin từ danh mục
    // SysConfig 'vds_event_skip' qua EventOptions.SkipWindowFor(type) — xem VdsFrameProcessor.
    public int EffectiveVehicleDedupMinPlateLength => Math.Clamp(VehicleDedupMinPlateLength, 1, 16);
    public int EffectiveVehicleDedupMaxKeys => Math.Clamp(VehicleDedupMaxKeys, 128, 2_000_000);

    /// <summary>Gắn một trong hai kiểu header xác thực được VDS API hỗ trợ.</summary>
    public void ApplyAuth(HttpRequestMessage request)
    {
        if (string.IsNullOrWhiteSpace(ApiKey)) return;

        if (UseBearerAuth)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
        else
            request.Headers.TryAddWithoutValidation("X-API-Key", ApiKey);
    }

    public IngestionOptions Clone() => new()
    {
        ConnectTimeoutSeconds = ConnectTimeoutSeconds,
        SseIdleTimeoutSeconds = SseIdleTimeoutSeconds,
        SseEnabled = SseEnabled,
        ApiBaseUrl = ApiBaseUrl,
        ApiKey = ApiKey,
        UseBearerAuth = UseBearerAuth,
        AllowInsecureHttp = AllowInsecureHttp,
        AllowSelfSignedCert = AllowSelfSignedCert,
        StreamPath = StreamPath,
        Sources = Sources,
        SettleSeconds = SettleSeconds,
        QueueCapacity = QueueCapacity,
        RawLogEnabled = RawLogEnabled,
        RawLogDirectory = RawLogDirectory,
        MaxFrameCharacters = MaxFrameCharacters,
        ReconnectBackoffInitialMs = ReconnectBackoffInitialMs,
        ReconnectBackoffMaxSeconds = ReconnectBackoffMaxSeconds,
        VehicleDedupMinPlateLength = VehicleDedupMinPlateLength,
        VehicleDedupMaxKeys = VehicleDedupMaxKeys,
    };
}
