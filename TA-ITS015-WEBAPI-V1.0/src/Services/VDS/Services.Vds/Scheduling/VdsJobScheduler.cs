using Microsoft.Extensions.Hosting;
using Services.Vds.Configuration;
using Services.Vds.Catalog;
using Services.Vds.Features.VehicleFilter;
using Services.Vds.Features.ZoneStatus;
using Services.Vds.Features.Summary;
using Services.Vds.Features.Journey;

namespace Services.Vds.Scheduling;

/// <summary>
/// Author: KienLT
/// Description: Một chỗ duy nhất chạy MỌI việc theo chu kỳ của service. Trước đây mỗi tính năng tự mang một
/// <c>BackgroundService</c> riêng với cùng một khung vòng lặp: đọc nhịp, chờ, gọi, bọc try/catch. Gom lại thì
/// đọc file này là biết service đang làm gì theo giờ, và khung lặp chỉ còn MỘT bản để sửa.
///
/// <para>
/// <b>Mỗi job một vòng lặp riêng</b>, không dùng bộ hẹn giờ dùng chung: job chậm chỉ làm chính nó trễ, không
/// đẩy lùi job khác. Và vì lượt sau chỉ bắt đầu khi lượt trước xong, KHÔNG cần khoá chống chạy chồng — bản cũ
/// phải có semaphore cho từng job chính vì dùng bộ hẹn giờ.
/// </para>
///
/// <para>
/// <b>Nhịp chạy</b> lấy từ cấu hình của chính tính năng, đọc lại MỖI LƯỢT nên đổi nhịp trong file có hiệu lực
/// mà không cần restart. <c>Service:Jobs:Schedules</c> chỉ để ghi đè.
/// </para>
///
/// <para>
/// <b>Không gom vào đây</b>: những việc chạy theo SỰ KIỆN chứ không theo giờ — phát số đếm (theo lượt xe vừa
/// ghi), tải media và gọi API lập lịch tự động (theo hàng đợi). Nhét chúng vào một khung "cứ N giây một lần"
/// sẽ làm mất đúng tính chất khiến chúng phản hồi nhanh.
/// </para>
/// Created date: 26/08/2026
/// </summary>
public sealed class VdsJobScheduler : BackgroundService
{
    /// <summary>Một job đã đăng ký.</summary>
    /// <param name="Name">Tên job, cũng là khoá cấu hình ghi đè.</param>
    /// <param name="IsFeatureEnabled">Tính năng chủ có đang bật không — đọc lúc khởi động.</param>
    /// <param name="Interval">Nhịp chạy hiện hành, đọc lại mỗi lượt.</param>
    /// <param name="Run">Thân job.</param>
    /// <param name="OverrideMayOnlyShorten">
    /// Ghi đè trong <c>Service:Jobs:Schedules</c> chỉ được làm job chạy NHANH hơn nhịp của tính năng, không được
    /// chậm hơn — dùng cho job mà nhịp là phần của tính đúng (quét trạng thái vùng phải kịp ghi giữ nhịp trước
    /// hạn quá hạn).
    /// </param>
    private sealed record JobEntry(
        string Name,
        Func<bool> IsFeatureEnabled,
        Func<TimeSpan> Interval,
        Func<CancellationToken, Task> Run,
        bool OverrideMayOnlyShorten = false);

    private readonly IVdsConfigManager _config;
    private readonly ILogger<VdsJobScheduler> _logger;
    private readonly List<JobEntry> _jobs = new();

    public VdsJobScheduler(
        ZoneStatusUpdater zoneStatus,
        VehicleFilterManager vehicleFilters,
        CatalogReloadJob catalog,
        TrafficSummaryJob summary,
        JourneyJob journey,
        IVdsConfigManager config,
        ILogger<VdsJobScheduler> logger)
    {
        _config = config;
        _logger = logger;

        // Quét trạng thái vùng: BẮT BUỘC phải có, không phải tuỳ chọn — hai điều kiện hạ trạng thái đều theo
        // thời gian nên không có bản tin nào tới để kích hoạt chúng.
        // Nhịp quét gắn với hạn quá hạn (ZoneStatusOptions.KeepAliveSeconds) ⇒ ghi đè chỉ được rút ngắn.
        Register(
            VdsJobNames.ZoneStatusSweep,
            () => _config.Current.ZoneStatus.Enabled,
            () => TimeSpan.FromSeconds(_config.Current.ZoneStatus.EffectiveSweepIntervalSeconds),
            async ct => await zoneStatus.SweepAsync(ct).ConfigureAwait(false),
            overrideMayOnlyShorten: true);

        // Danh sách đen/trắng biển số: người vận hành sửa trên web, không nạp lại thì phải restart mới thấy.
        Register(
            VdsJobNames.VehicleFilterReload,
            () => _config.Current.VehicleFilter.Enabled,
            () => TimeSpan.FromSeconds(_config.Current.VehicleFilter.EffectiveReloadSeconds),
            vehicleFilters.LoadAsync);

        // Danh mục: camera/vùng/đoạn thêm trên web phải có hiệu lực mà không cần restart.
        Register(
            VdsJobNames.CatalogReload,
            () => _config.Current.Catalog.ReloadSeconds > 0,
            () => TimeSpan.FromSeconds(_config.Current.Catalog.EffectiveReloadSeconds),
            catalog.RunAsync);

        // Tổng hợp số liệu lâu dài (giờ/ngày/tháng) vào TmsTrafficSummary trên SQL Server — nguồn báo cáo
        // dự phòng khi QuestDB lỗi. Mặc định mỗi giờ; mỗi lượt tự bù vài giờ gần nhất (upsert luỹ đẳng).
        Register(
            VdsJobNames.TrafficSummary,
            () => _config.Current.Summary.Enabled,
            () => TimeSpan.FromSeconds(_config.Current.Summary.EffectiveIntervalSeconds),
            summary.RunAsync);

        // Ghép hành trình A→B theo chu kỳ = LƯỢT QUÉT ĐỐI SOÁT (đường chính là ghép theo bản tin,
        // JourneyArrivalWorker). Bù chuyến mà đường bản tin bỏ sót; ghi luỹ đẳng nên chạy trùng vô hại.
        Register(
            VdsJobNames.JourneyMatch,
            () => _config.Current.Journey.Enabled,
            () => TimeSpan.FromSeconds(_config.Current.Journey.EffectiveIntervalSeconds),
            journey.RunAsync);
    }

    private void Register(
        string name, Func<bool> isFeatureEnabled, Func<TimeSpan> interval, Func<CancellationToken, Task> run,
        bool overrideMayOnlyShorten = false)
        => _jobs.Add(new JobEntry(name, isFeatureEnabled, interval, run, overrideMayOnlyShorten));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var loops = new List<Task>();

        foreach (var job in _jobs)
        {
            var schedule = _config.Current.Jobs.For(job.Name);

            if (schedule is { Enabled: false })
            {
                _logger.LogInformation("[Job] {Name}: lịch bị tắt qua Service:Jobs:Schedules.", job.Name);
                continue;
            }

            if (!job.IsFeatureEnabled())
            {
                _logger.LogInformation("[Job] {Name}: tính năng đang tắt ⇒ không chạy.", job.Name);
                continue;
            }

            loops.Add(RunLoopAsync(job, stoppingToken));
        }

        if (loops.Count == 0)
        {
            _logger.LogInformation("[Job] Không có job định kỳ nào được chạy.");
            return;
        }

        // Chờ mọi vòng lặp kết thúc. Vòng lặp chỉ kết thúc khi bị yêu cầu dừng, nên đây là điểm dừng của cả
        // hosted service; hạn chờ lúc dừng nằm ở StopAsync của lớp cha.
        await Task.WhenAll(loops).ConfigureAwait(false);
    }

    private async Task RunLoopAsync(JobEntry job, CancellationToken ct)
    {
        var overrideSeconds = _config.Current.Jobs.For(job.Name)?.IntervalSeconds ?? 0;
        var describe = overrideSeconds > 0 ? $"{overrideSeconds}s (ghi đè)" : $"{job.Interval().TotalSeconds}s";
        _logger.LogInformation("[Job] {Name}: chạy mỗi {Interval}.", job.Name, describe);

        var cappedWarned = false;
        while (!ct.IsCancellationRequested)
        {
            // Đọc lại nhịp MỖI LƯỢT: đổi nhịp trong file có hiệu lực không cần restart. Ghi đè trong section
            // Jobs thắng nhịp của tính năng — trừ job chỉ cho rút ngắn (xem JobEntry.OverrideMayOnlyShorten).
            var seconds = _config.Current.Jobs.For(job.Name)?.IntervalSeconds ?? 0;
            var featureInterval = job.Interval();
            var delay = seconds > 0 ? TimeSpan.FromSeconds(seconds) : featureInterval;
            if (job.OverrideMayOnlyShorten && delay > featureInterval)
            {
                if (!cappedWarned)
                {
                    _logger.LogWarning(
                        "[Job] {Name}: ghi đè {Override}s dài hơn nhịp tính năng {Interval}s ⇒ chạy theo nhịp tính năng.",
                        job.Name, seconds, featureInterval.TotalSeconds);
                    cappedWarned = true;
                }
                delay = featureInterval;
            }

            try { await Task.Delay(delay, ct).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }

            try
            {
                await job.Run(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Một lượt lỗi KHÔNG được giết vòng lặp: mất vòng lặp là mất hẳn việc định kỳ đó cho tới lần
                // restart, mà không có gì báo.
                _logger.LogError(ex, "[Job] {Name}: lượt chạy thất bại; giữ chu kỳ, thử lại lượt sau.", job.Name);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromSeconds(_config.Current.Jobs.EffectiveShutdownTimeoutSeconds);

        try
        {
            await base.StopAsync(cancellationToken).WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("[Job] Còn lượt job chạy quá {Timeout}s khi dừng; không chờ nữa.",
                timeout.TotalSeconds);
        }
        catch (OperationCanceledException) { }
    }
}

/// <summary>
/// Author: KienLT
/// Description: Tên các job định kỳ — cũng là KHOÁ cấu hình ghi đè lịch trong <c>Service:Jobs:Schedules</c>.
///
/// Gom vào một chỗ vì tên xuất hiện ở ba nơi: lúc đăng ký, trong log, và trong file cấu hình. Đổi tên ở đây
/// là làm mất tác dụng của phần ghi đè đang khai trong file — nên đổi thì phải sửa cả file cấu hình.
/// Created date: 26/08/2026
/// </summary>
public static class VdsJobNames
{
    /// <summary>Quét trạng thái vùng theo giờ (hạ chốt sự kiện hết hạn, vùng hết dữ liệu).</summary>
    public const string ZoneStatusSweep = "ZoneStatusSweep";

    /// <summary>Nạp lại danh sách đen/trắng biển số từ DB.</summary>
    public const string VehicleFilterReload = "VehicleFilterReload";

    /// <summary>Nạp lại danh mục (camera, vùng, danh mục hiển thị, cấu hình vận hành).</summary>
    public const string CatalogReload = "CatalogReload";

    /// <summary>Tổng hợp số liệu lâu dài theo giờ/ngày/tháng vào TmsTrafficSummary (fallback báo cáo).</summary>
    public const string TrafficSummary = "TrafficSummary";

    /// <summary>Ghép hành trình A→B tính tốc độ trung bình và nâng sự cố quá tốc độ trung bình.</summary>
    public const string JourneyMatch = "JourneyMatch";
}
