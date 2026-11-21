namespace Services.Vds.Scheduling;

/// <summary>
/// Author: KienLT
/// Description: Bộ điều phối job định kỳ TRONG tiến trình (section <c>"Service:Jobs"</c>) — không dùng
/// Hangfire: service này chỉ có vài việc theo chu kỳ, thêm một hạ tầng job có bảng riêng và dashboard riêng
/// là trả giá vận hành cho thứ mà một vòng lặp giải quyết xong.
///
/// <para>
/// <b>Nhịp chạy mặc định của mỗi job nằm trong cấu hình của CHÍNH tính năng</b> (nhịp quét trạng thái vùng ở
/// section trạng thái vùng, nhịp nạp lại danh sách biển ở section biển số…). Khai ở đây là GHI ĐÈ, dùng khi
/// cần đổi nhịp mà không muốn đụng vào cấu hình tính năng. Để trống hoặc khai 0 ⇒ lấy nhịp của tính năng.
/// </para>
/// Created date: 26/08/2026
/// </summary>
public sealed class JobsOptions
{
    private Dictionary<string, JobOptions>? _lookup;

    /// <summary>Ghi đè theo TÊN job: bật/tắt lịch và nhịp chạy.</summary>
    public Dictionary<string, JobOptions> Schedules { get; set; } = new();

    /// <summary>Thời gian chờ các lượt job đang chạy khi dừng service (giây).</summary>
    public int ShutdownTimeoutSeconds { get; set; } = 30;

    public int EffectiveShutdownTimeoutSeconds => Math.Clamp(ShutdownTimeoutSeconds, 0, 300);

    /// <summary>Ghi đè của một job; null nếu không khai.</summary>
    public JobOptions? For(string jobName)
    {
        // Bộ sưu tập bind từ file nên có comparer mặc định (phân biệt hoa thường); dựng bản không phân biệt
        // một lần rồi dùng lại để tên job trong file không phải khớp y hệt chữ hoa chữ thường.
        _lookup ??= new Dictionary<string, JobOptions>(Schedules, StringComparer.OrdinalIgnoreCase);
        return _lookup.TryGetValue(jobName, out var value) ? value : null;
    }

    public JobsOptions Clone() => new()
    {
        Schedules = Schedules.ToDictionary(kv => kv.Key, kv => kv.Value.Clone()),
        ShutdownTimeoutSeconds = ShutdownTimeoutSeconds,
    };
}

/// <summary>Ghi đè lịch của một job.</summary>
public sealed class JobOptions
{
    /// <summary>Bật/tắt LỊCH của job này. Tắt tính năng thì dùng công tắc của chính tính năng.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Nhịp chạy (giây). 0 hoặc âm ⇒ dùng nhịp mặc định của tính năng.</summary>
    public int IntervalSeconds { get; set; }

    public JobOptions Clone() => new()
    {
        Enabled = Enabled,
        IntervalSeconds = IntervalSeconds,
    };
}
