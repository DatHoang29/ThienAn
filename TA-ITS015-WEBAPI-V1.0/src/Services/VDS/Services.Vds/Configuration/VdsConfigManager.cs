using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Services.Shared.Persistence;

namespace Services.Vds.Configuration;

/// <summary>
/// Author: KienLT
/// Description: Giữ snapshot cấu hình đang hiệu lực và là nơi DUY NHẤT được đổi nó.
///
/// <para><b>Ba nguồn, thứ tự ưu tiên từ thấp lên cao:</b>
/// <list type="number">
/// <item><c>appsettings.json</c> section <c>"Service"</c> — nền.</item>
/// <item><c>VdsConfig</c> trên DB — người vận hành sửa trên web (bộ nạp gọi <see cref="Apply"/>).</item>
/// <item>Override lúc chạy — NATS đẩy xuống, thắng cả hai nguồn trên.</item>
/// </list></para>
///
/// <para><b>Vì sao override phải thắng, và vì sao phải lưu ra file.</b>
/// Nếu <see cref="ReloadAsync"/> chỉ bind lại từ file thì mỗi lần reload sẽ ghi đè ngược tham số vừa đẩy
/// xuống — sai âm thầm, không có lỗi nào để thấy. Cách xử lý: mọi lần <see cref="Apply"/> thành công đều
/// được NHỚ LẠI (tên → json) và phát lại sau mỗi lần bind file. Bản cũ nhớ trong RAM nên restart là mất;
/// bản này ghi qua <see cref="ConfigOverrideStore"/> ra <c>{StateDirectory}/config-overrides.json</c> và
/// đọc lại lúc khởi tạo ⇒ đẩy tham số xuống một lần là có hiệu lực vĩnh viễn, không cần restart mà cũng
/// không mất khi restart.</para>
///
/// <para><b>Đổi snapshot bằng <see cref="Volatile.Write{T}"/>.</b> Luồng đọc không bị chặn và không thấy
/// trạng thái nửa vời, vì snapshot không bao giờ bị sửa tại chỗ — luôn thay bằng object mới
/// (<see cref="ServiceConfig.Clone"/>).</para>
/// Created date: 24/08/2026
/// </summary>
public sealed class VdsConfigManager : IVdsConfigManager, IDisposable
{
    private readonly IConfiguration? _configuration;
    private readonly DatabaseManager? _databases;
    private readonly ConfigOverrideStore? _overrideStore;
    private readonly ILogger<VdsConfigManager> _logger;

    /// <summary>Chỉ một lượt <see cref="ReloadAsync"/> chạy tại một thời điểm.</summary>
    private readonly SemaphoreSlim _reloadLock = new(1, 1);

    /// <summary>
    /// Tham số đã áp lúc chạy (tên → json nguyên văn). Giữ trong RAM để phát lại sau mỗi lần bind file,
    /// và đồng bộ ra file qua <see cref="ConfigOverrideStore"/> để sống qua restart.
    /// </summary>
    private readonly ConcurrentDictionary<string, string> _overrides = new(StringComparer.Ordinal);

    private ServiceConfig _current;

    public VdsConfigManager(
        IConfiguration configuration,
        DatabaseManager databases,
        ConfigOverrideStore overrideStore,
        ILogger<VdsConfigManager> logger)
    {
        _configuration = configuration;
        _databases = databases;
        _overrideStore = overrideStore;
        _logger = logger;

        _current = LoadFromFile();

        // Đọc override đã lưu và phát lên snapshot nền. Làm NGAY trong ctor để mọi thành phần nhận
        // VdsConfigManager qua DI đều thấy cấu hình đã đầy đủ, không có khoảng thời gian "chưa áp override".
        foreach (var (name, json) in overrideStore.Load())
            _overrides[name] = json;

        _current = ReapplyOverrides(_current);
        Validate(_current);
        LogEffective(_current);
    }

    /// <summary>
    /// Khởi tạo từ snapshot dựng bằng code — cho unit test và harness.
    /// Không đọc file, không validate kết nối; <see cref="ReloadAsync"/> thành no-op và
    /// <see cref="Apply"/> không ghi ra đĩa.
    /// </summary>
    public VdsConfigManager(ServiceConfig seed, ILogger<VdsConfigManager> logger)
    {
        _current = seed ?? new ServiceConfig();
        _logger = logger;
    }

    public ServiceConfig Current => Volatile.Read(ref _current);

    /// <summary>Deployment baseline, without persisted runtime overrides.</summary>
    public ServiceConfig ReadBaseline() => _configuration is null ? new ServiceConfig() : LoadFromFile();

    public bool Apply(string parameterName, string json)
    {
        var name = parameterName?.Trim() ?? "";

        ServiceConfig? next;
        try
        {
            next = ConfigParamApplier.BuildNext(Current, name, json);
        }
        catch (Exception ex)
        {
            // BuildNext đã bắt JsonException; vào đây là lỗi ngoài dự kiến — vẫn không được làm sập caller.
            _logger.LogWarning(ex, "[Config] Lỗi khi áp tham số '{Name}'; giữ giá trị đang hiệu lực.", name);
            Services.Vds.Catalog.CatalogRefreshAudit.Failed(name);
            return false;
        }

        if (next is null)
        {
            Services.Vds.Catalog.CatalogRefreshAudit.Failed(name);
            _logger.LogWarning(
                "[Config] Từ chối tham số '{Name}' (không hỗ trợ hoặc giá trị không hợp lệ). " +
                "Các tham số đổi được lúc chạy: {Supported}.",
                name, string.Join(", ", ConfigParamApplier.SupportedNames));
            return false;
        }

        Volatile.Write(ref _current, next);

        // Nhớ lại để reload-từ-file không ghi đè ngược, và ghi ra đĩa để sống qua restart.
        _overrides[name] = json;
        _overrideStore?.Save(_overrides);

        _logger.LogInformation("[Config] Áp tham số '{Name}' — đã lưu, còn hiệu lực sau khi restart.", name);
        LogEffective(next);
        return true;
    }

    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        if (_configuration is null) return;   // snapshot dựng bằng code: không có nguồn file

        await _reloadLock.WaitAsync(cancellationToken);
        try
        {
            var next = LoadFromFile();
            next = ReapplyOverrides(next);

            Validate(next);
            Volatile.Write(ref _current, next);

            _logger.LogInformation(
                "[Config] Nạp lại cấu hình '{Name}' từ file + {Count} override.", next.Name, _overrides.Count);
            LogEffective(next);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Reload KHÔNG được ném ra ngoài: giữ snapshot last-known-good.
            _logger.LogError(ex, "[Config] Nạp lại cấu hình thất bại; giữ snapshot đang hiệu lực.");
        }
        finally
        {
            _reloadLock.Release();
        }
    }

    public void Dispose() => _reloadLock.Dispose();

    // ===== Các bước trong pipeline nạp =====

    private ServiceConfig LoadFromFile() =>
        _configuration!.GetSection(ServiceConfig.SectionName).Get<ServiceConfig>()
        ?? throw new InvalidOperationException(
            $"Thiếu section '{ServiceConfig.SectionName}' trong cấu hình của VDS.");

    /// <summary>
    /// Phát lại toàn bộ override lên snapshot vừa bind từ file.
    /// Override nào không áp được nữa (giá trị cũ giờ không hợp lệ) thì BỎ khỏi danh sách và ghi lại file
    /// — giữ lại chỉ để mỗi lần reload lại ném một cảnh báo y như cũ.
    /// </summary>
    private ServiceConfig ReapplyOverrides(ServiceConfig snapshot)
    {
        if (_overrides.IsEmpty) return snapshot;

        var stale = new List<string>();

        foreach (var (name, json) in _overrides)
        {
            var applied = ConfigParamApplier.BuildNext(snapshot, name, json);
            if (applied is not null)
            {
                snapshot = applied;
                continue;
            }

            _logger.LogWarning(
                "[Config] Override '{Name}' không áp lại được; bỏ nó và dùng giá trị từ appsettings.", name);
            stale.Add(name);
        }

        if (stale.Count > 0)
        {
            foreach (var name in stale) _overrides.TryRemove(name, out _);
            _overrideStore?.Save(_overrides);
        }

        return snapshot;
    }

    private void Validate(ServiceConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.Name))
            throw new InvalidOperationException("Service:Name là bắt buộc.");

        if (_databases is not null && !_databases.Contains(config.DataConnectionId))
            throw new InvalidOperationException(
                $"Kết nối dữ liệu '{config.DataConnectionId}' chưa khai trong Configuration/Database.json.");

        // QuestConnectionId KHÔNG validate ở đây: rỗng là hợp lệ (tắt QuestDB), và khai sai thì
        // QuestDbHealthCheck báo bằng log chứ không chặn khởi động — mọi tính năng trên SQL Server
        // không có lý do gì phải dừng theo.
    }

    /// <summary>
    /// In ra những tham số mà nhìn vào là biết service đang hành xử thế nào. Có dòng này trong log khởi
    /// động thì khi số liệu bất thường không phải đoán xem cấu hình nào đang hiệu lực.
    /// </summary>
    private void LogEffective(ServiceConfig config)
    {
        var quest = config.QuestEnabled ? config.QuestConnectionId : "(tắt)";

        var speeds = config.SpeedRangeByDirection.Count == 0
            ? "(chưa có — sẽ KHÔNG sinh sự kiện quá tốc độ)"
            : string.Join(", ", config.SpeedRangeByDirection
                .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                .Select(kv => $"hướng {kv.Key} {kv.Value}"));

        _logger.LogInformation(
            "[Config] Đang hiệu lực — SqlServer: {Data}; QuestDB SqlSugar/PgWire: {Quest}; " +
            "ngưỡng tốc độ: {Speeds}.",
            config.DataConnectionId, quest, speeds);
    }
}
