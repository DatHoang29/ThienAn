using Services.Vds.Catalog;
using Services.Vds.Configuration;
using Services.Vds.Shared.Domain;
using Services.Vds.Shared.Testing;
using Services.Vds;
using Services.Vds.Manager;
using Services.Shared.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Services.Vds.Pipeline.Mapping;

namespace Services.Vds.Wpf.Simulation;

/// <summary>Một camera trong lượt mô phỏng.</summary>
/// <param name="Ip">IP để bơm bản tin.</param>
/// <param name="DirectionId">Hướng — để tra đích nhân bản.</param>
/// <param name="Speed">
/// Ngưỡng tốc độ của camera, phân giải qua <c>SpeedRangeResolver</c> (vùng chính → mặc định theo hướng) —
/// đúng ngưỡng mà service đẩy xuống làn AID. Tốc độ mỗi lượt xe random TRONG ngưỡng này ⇒ Regular không
/// bao giờ tự thành vi phạm. Camera không phân giải được ngưỡng bị loại khỏi lượt chạy (xem <c>TryBuildRun</c>).
/// </param>
public sealed record SimulatorCamera(string Ip, string? DirectionId, ResolvedSpeedRange Speed);

/// <summary>Tham số một lượt chạy mô phỏng — chốt lúc bấm Bắt đầu, không đổi giữa lượt chạy.</summary>
/// <param name="Cameras">Camera sinh dữ liệu raw; mỗi nhịp chọn NGẪU NHIÊN một camera trong danh sách này.</param>
/// <param name="CountCameraIpByDirection">
/// <c>DirectionId → IP camera</c> nhận bản nhân đôi của cả hướng. Hướng không khai ⇒ không nhân bản.
/// </param>
public sealed record SimulatorRun(
    IReadOnlyList<SimulatorCamera> Cameras,
    IReadOnlyDictionary<string, string> CountCameraIpByDirection,
    int MinIntervalSeconds,
    int MaxIntervalSeconds,
    double MinLengthMeter,
    double MaxLengthMeter,
    int StatisticPeriodMinutes)
{
    /// <summary>IP camera nhận bản nhân đôi của một hướng; null nếu hướng đó không khai.</summary>
    public string? CountCameraIpOf(string? directionId)
        => !string.IsNullOrWhiteSpace(directionId)
           && CountCameraIpByDirection.TryGetValue(directionId.Trim(), out var ip) ? ip : null;

    /// <summary>
    /// Camera cần gửi bản tin <c>Statistic</c>: camera sinh raw HỢP với camera nhận nhân bản — camera
    /// được chọn làm đích nhân bản mà không nằm trong danh sách sinh raw vẫn phải có bản tin thống kê,
    /// nếu không số thống kê của nó sẽ hụt so với số raw đã bơm.
    /// </summary>
    public IReadOnlyList<string> StatisticCameraIps { get; } =
        Cameras.Select(c => c.Ip)
            .Concat(CountCameraIpByDirection.Values)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
}

/// <summary>
/// Sinh bản tin <c>Regular</c> ngẫu nhiên cho các camera được chọn: MỘT vòng lặp duy nhất, mỗi nhịp
/// (<c>[MinIntervalSeconds, MaxIntervalSeconds]</c>) bơm **một** lượt xe cho **một** camera chọn ngẫu nhiên.
/// Trước đây mỗi camera một vòng riêng nên 10 camera sinh ~10 lượt mỗi nhịp — quá nhiều để quan sát.
///
/// Nhân bản theo hướng: nếu hướng của camera vừa bơm có khai camera đếm
/// (<see cref="SimulatorRun.CountCameraIpByDirection"/>) và camera đó KHÁC camera vừa bơm, bơm thêm một
/// lượt y hệt (cùng biển số, cùng tốc độ, cùng chiều dài) cho camera đếm ⇒ hai bản ghi raw, camera đếm
/// mang số của cả hướng.
///
/// Song song đó, mỗi <c>StatisticPeriodMinutes</c> phút gửi một bản tin <c>Statistic</c> cho từng
/// camera, dựng từ ĐÚNG số xe và tốc độ trung bình đã sinh trong chu kỳ vừa qua — nhờ vậy số liệu
/// thống kê của thiết bị khớp với số liệu đếm mà pipeline tự tính.
/// </summary>
public sealed class RegularSimulator
{
    /// <summary>Nhãn giai đoạn trên feed hoạt động.</summary>
    private const string Stage = "Simulator";

    private static readonly char[] PlateLetters = "ABCDEFGHKLMNPSTUVXYZ".ToCharArray();

    private readonly IVdsFrameInjector _app;
    private readonly ActivityPublisher _activities;
    private readonly VehicleSizeResolver _sizeResolver;
    private readonly CameraManager _cameras;
    private readonly SpeedRangeResolver _speeds;
    private readonly ConcurrentDictionary<string, Bucket> _buckets = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Ngưỡng đã dùng gần nhất của từng camera — để báo một lần khi ngưỡng đổi giữa lượt chạy.</summary>
    private readonly ConcurrentDictionary<string, SpeedRange> _lastRange = new(StringComparer.OrdinalIgnoreCase);

    private CancellationTokenSource? _cts;
    private Task? _runner;

    /// <param name="sizeResolver">
    /// Cùng bộ phân cỡ (và cùng hai ngưỡng cấu hình) mà pipeline dùng ⇒ số S/M/L trong bản tin Statistic
    /// khớp với cỡ mà tầng raw tự suy từ chiều dài xe.
    /// </param>
    /// <param name="speeds">
    /// Tra LẠI ngưỡng mỗi lượt xe: đổi Min/Max của zone trên web (refresh NATS) thì mô phỏng đang chạy áp ngay
    /// ở lượt kế tiếp, khớp ngưỡng service vừa đẩy xuống AID — không phải dừng rồi chạy lại.
    /// </param>
    public RegularSimulator(
        IVdsFrameInjector app, ActivityPublisher activities, VehicleSizeResolver sizeResolver,
        CameraManager cameras, SpeedRangeResolver speeds)
    {
        _app = app;
        _activities = activities;
        _sizeResolver = sizeResolver;
        _cameras = cameras;
        _speeds = speeds;
    }

    /// <summary>
    /// Ngưỡng hiện hành của camera (vùng chính → mặc định hướng). Tạm thời không phân giải được (danh mục đang
    /// nạp lại, vùng vừa khai lệch) ⇒ giữ ngưỡng lúc bắt đầu thay vì bỏ lượt xe.
    /// </summary>
    private SpeedRange CurrentRange(SimulatorCamera cam)
    {
        var range = _cameras.TryGet(cam.Ip, out var info) && info is not null && _speeds.ForCamera(info) is ResolvedSpeedRange live
            ? live.Range
            : cam.Speed.Range;

        var previous = _lastRange.GetOrAdd(cam.Ip, range);
        if (previous != range && _lastRange.TryUpdate(cam.Ip, range, previous))
            Info($"Ngưỡng camera {cam.Ip} đổi {previous} → {range} (danh mục vừa nạp lại); áp từ lượt xe này.");
        return range;
    }

    public bool IsRunning => _runner is not null && !_runner.IsCompleted;

    /// <summary>Số xe (tổng và theo cỡ) cùng tổng tốc độ đã sinh trong chu kỳ hiện tại của một camera.</summary>
    private sealed class Bucket
    {
        private readonly object _sync = new();
        private int _count;
        private double _speedSum;
        private int _short;
        private int _medium;
        private int _long;
        private int _other;

        /// <param name="size">
        /// Cỡ xe theo <see cref="VehicleSizeResolver"/> (<c>short</c>/<c>medium</c>/<c>long</c>);
        /// rỗng = không suy được cỡ ⇒ vào nhóm Other.
        /// </param>
        public void Add(int speedKmh, string size)
        {
            lock (_sync)
            {
                _count++;
                _speedSum += speedKmh;
                switch (size)
                {
                    case VehicleSizeResolver.Short: _short++; break;
                    case VehicleSizeResolver.Medium: _medium++; break;
                    case VehicleSizeResolver.Long: _long++; break;
                    default: _other++; break;
                }
            }
        }

        /// <summary>Lấy số liệu chu kỳ và bắt đầu chu kỳ mới.</summary>
        public (int Count, double AvgSpeed, VdsStatisticSizeCounts Sizes) Drain()
        {
            lock (_sync)
            {
                var result = (_count, _count == 0 ? 0d : _speedSum / _count,
                    new VdsStatisticSizeCounts(_short, _medium, _long, _other));
                _count = 0;
                _speedSum = 0;
                _short = _medium = _long = _other = 0;
                return result;
            }
        }
    }

    public void Start(SimulatorRun run)
    {
        if (IsRunning) return;
        if (run.Cameras.Count == 0) { Warn("Chưa chọn camera nào để sinh dữ liệu raw."); return; }

        _buckets.Clear();
        _lastRange.Clear();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        _runner = Task.WhenAll(VehicleLoopAsync(run, ct), StatisticLoopAsync(run, ct));

        var mirror = run.CountCameraIpByDirection.Count == 0
            ? "không nhân bản theo hướng"
            : $"nhân bản cho {run.CountCameraIpByDirection.Count} hướng "
              + $"({string.Join(", ", run.CountCameraIpByDirection.Select(kv => $"hướng {kv.Key}→{kv.Value}"))})";

        var speeds = string.Join(", ", run.Cameras.Select(c => $"{c.Ip} {c.Speed}"));

        Info($"Bắt đầu mô phỏng: {run.Cameras.Count} camera, MỖI LƯỢT 1 xe ở 1 camera ngẫu nhiên, "
          + $"cách nhau {run.MinIntervalSeconds}-{run.MaxIntervalSeconds}s, tốc độ theo ngưỡng vùng/hướng ({speeds}), "
          + $"dài {run.MinLengthMeter:0.#}-{run.MaxLengthMeter:0.#}m, thống kê mỗi {run.StatisticPeriodMinutes} phút; {mirror}.");
    }

    public async Task StopAsync()
    {
        if (_cts is null) return;
        _cts.Cancel();
        if (_runner is not null)
        {
            try { await _runner; }
            catch (OperationCanceledException) { }
            catch (Exception ex) { Warn("Mô phỏng dừng kèm lỗi: " + ex.Message); }
        }
        _cts.Dispose();
        _cts = null;
        _runner = null;
        Info("Đã dừng mô phỏng.");
    }

    /// <summary>
    /// MỘT vòng lặp cho cả lượt chạy: nghỉ ngẫu nhiên rồi bơm một lượt xe ở một camera chọn ngẫu nhiên,
    /// kèm bản nhân đôi cho camera đếm của hướng (nếu có).
    /// </summary>
    private async Task VehicleLoopAsync(SimulatorRun run, CancellationToken ct)
    {
        var rnd = new Random(Environment.TickCount);

        while (!ct.IsCancellationRequested)
        {
            var wait = rnd.Next(run.MinIntervalSeconds, run.MaxIntervalSeconds + 1);
            try { await Task.Delay(TimeSpan.FromSeconds(wait), ct); }
            catch (OperationCanceledException) { break; }

            var cam = run.Cameras[rnd.Next(run.Cameras.Count)];

            // Tốc độ nằm TRONG ngưỡng của camera này (vùng chính → mặc định hướng, hai biên tính là hợp lệ)
            // ⇒ bản tin Regular không bao giờ tự trở thành vi phạm, và không kéo vùng về chậm khi bật đánh
            // giá theo tốc độ (mọi xe ≥ V_min). Camera thiếu ngưỡng đã bị loại từ TryBuildRun.
            var range = CurrentRange(cam);

            var speed = rnd.Next(range.Min, range.Max + 1);
            var length = Math.Round(run.MinLengthMeter + rnd.NextDouble() * (run.MaxLengthMeter - run.MinLengthMeter), 2);
            var plate = RandomPlate(rnd);

            // Phân cỡ NGAY tại đây bằng đúng bộ phân cỡ của pipeline: bản tin đã gửi đi thì bên này
            // không đọc lại được cỡ mà tầng raw đã suy.
            var size = _sizeResolver.ResolveByMillimeter((int)Math.Round(length * 1000, MidpointRounding.AwayFromZero));

            if (!await SendVehicleAsync(cam.Ip, plate, speed, length, size, ct)) break;

            // Nhân bản cho camera đếm của hướng. Bỏ qua khi chính camera vừa bơm là camera đếm —
            // nhân bản vào chính nó sẽ đếm đôi và bản thứ hai bị tầng (2) loại vì trùng (biển số, camera).
            var mirrorIp = run.CountCameraIpOf(cam.DirectionId);
            if (mirrorIp is not null && !string.Equals(mirrorIp, cam.Ip, StringComparison.OrdinalIgnoreCase))
            {
                // CÙNG biển số với bản gốc (chốt 08/08/2026). Lưu ý: nếu về sau khai TmsTrafficSegment nối
                // camera vừa bơm với camera đếm thì JourneyJob sẽ ghép hai bản ghi này thành một hành trình
                // A→B với thời gian ~0s ⇒ tốc độ vô lý. Hiện danh mục đoạn rỗng nên chưa ảnh hưởng.
                if (!await SendVehicleAsync(mirrorIp, plate, speed, length, size, ct)) break;
            }
        }
    }

    /// <summary>Bơm một lượt xe và cộng vào bộ đếm chu kỳ của camera đó. Trả false khi bị huỷ.</summary>
    private async Task<bool> SendVehicleAsync(
        string cameraIp, string plate, int speedKmh, double lengthMeter, string size, CancellationToken ct)
    {
        var json = VdsSampleEvents.Normal(cameraIp, lane: "1", plate: plate, speedKmh: speedKmh, lengthMeter: lengthMeter);
        try
        {
            if (await _app.InjectAsync(json, ct))
                _buckets.GetOrAdd(cameraIp, _ => new Bucket()).Add(speedKmh, size);
            return true;
        }
        catch (OperationCanceledException) { return false; }
        catch (Exception ex)
        {
            Warn($"Bơm Regular cam={cameraIp} lỗi: {ex.Message}");
            return true;
        }
    }

    /// <summary>Gửi Statistic theo chu kỳ, dựng từ số liệu đã sinh nên khớp với tầng đếm.</summary>
    private async Task StatisticLoopAsync(SimulatorRun run, CancellationToken ct)
    {
        var period = TimeSpan.FromMinutes(Math.Max(run.StatisticPeriodMinutes, 1));
        while (!ct.IsCancellationRequested)
        {
            try { await Task.Delay(period, ct); }
            catch (OperationCanceledException) { break; }

            foreach (var ip in run.StatisticCameraIps)
            {
                if (!_buckets.TryGetValue(ip, out var bucket)) continue;
                var (count, avg, sizes) = bucket.Drain();

                var json = VdsSampleEvents.Statistic(ip, periodMinutes: run.StatisticPeriodMinutes,
                    vehicleNum: count, avgSpeedKmh: Math.Round(avg, 1), sizeCounts: sizes);
                try
                {
                    await _app.InjectAsync(json, ct);
                    Info($"Statistic cam={ip}: {count} xe (S={sizes.Short} M={sizes.Medium} L={sizes.Long} "
                       + $"O={sizes.Other}), TB {avg.ToString("0.#", CultureInfo.InvariantCulture)}km/h");
                }
                catch (OperationCanceledException) { return; }
                catch (Exception ex) { Warn($"Bơm Statistic cam={ip} lỗi: {ex.Message}"); }
            }
        }
    }

    private void Info(string detail) => _activities.Publish(Stage, detail, ActivityLevel.Info);

    private void Warn(string detail) => _activities.Publish(Stage, detail, ActivityLevel.Warning);

    /// <summary>Biển số Việt Nam hợp lệ theo mẫu 2 số + 1 chữ + 5 số (khớp PlateRules).</summary>
    private static string RandomPlate(Random rnd)
        => string.Concat(
            rnd.Next(11, 100).ToString(CultureInfo.InvariantCulture),
            PlateLetters[rnd.Next(PlateLetters.Length)],
            rnd.Next(10000, 100000).ToString(CultureInfo.InvariantCulture));
}

