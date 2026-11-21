using Microsoft.Extensions.DependencyInjection;
using Services.Vds.Configuration;
using Services.Vds.Catalog;
using Services.Vds.Pipeline.Logging;
using Wolverine;

namespace Services.Vds.Pipeline.Queue;

/// <summary>
/// Author: KienLT
/// Description: Biên duy nhất đưa raw JSON vào Wolverine; queue đầy sẽ tạo backpressure thay vì bỏ frame.
/// Created date: 25/08/2026
/// </summary>
public sealed class VdsFrameDispatcher
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IVdsConfigManager _config;
    private readonly CameraManager _cameras;
    private readonly VdsRawLogger _rawLog;
    private readonly ILogger<VdsFrameDispatcher> _logger;

    private int _questDisabledLogged;
    private int _cameraNotReadyLogged;
    private int _oversizedLogged;

    public VdsFrameDispatcher(
        IServiceScopeFactory scopeFactory,
        IVdsConfigManager config,
        CameraManager cameras,
        VdsRawLogger rawLog,
        ILogger<VdsFrameDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _cameras = cameras;
        _rawLog = rawLog;
        _logger = logger;
    }

    public async Task<bool> DispatchAsync(
        string json,
        bool isBackfill = false,
        CancellationToken ct = default,
        string? completionId = null,
        DateTime? processingDeadlineUtc = null)
    {
        if (string.IsNullOrWhiteSpace(json)) return false;

        var runtime = _config.Current;
        if (json.Length > runtime.Ingestion.EffectiveMaxFrameCharacters)
        {
            if (Interlocked.Exchange(ref _oversizedLogged, 1) == 0)
            {
                _logger.LogWarning(
                    "[Ingestion] Từ chối frame {Length} ký tự; giới hạn là {Limit}.",
                    json.Length, runtime.Ingestion.EffectiveMaxFrameCharacters);
            }

            return false;
        }

        // Ghi log thô NGAY tại biên vào, trước mọi cửa nghiệp vụ: frame bị bỏ vì camera lạ hay QuestDB
        // tắt vẫn phải có dấu vết, vì đó chính là lúc cần đối chiếu "thiết bị đã gửi gì".
        // Đặt sau cửa kiểm độ dài để một frame lỗi khổng lồ không ghi thành một dòng log khổng lồ.
        // Chỉ ghi frame live: frame bù lịch sử là đọc lại quá khứ, ghi vào đây sẽ làm sai số liệu đối chiếu.
        if (!isBackfill) _rawLog.Write(json);

        if (!runtime.QuestEnabled)
        {
            if (Interlocked.Exchange(ref _questDisabledLogged, 1) == 0)
            {
                _logger.LogWarning(
                    "[Ingestion] Chưa nhận frame vì QuestConnectionId đang rỗng.");
            }

            return false;
        }
        Interlocked.Exchange(ref _questDisabledLogged, 0);

        if (_cameras.Count == 0)
        {
            if (Interlocked.Exchange(ref _cameraNotReadyLogged, 1) == 0)
            {
                _logger.LogWarning(
                    "[Ingestion] Chưa nhận frame vì camera cache chưa sẵn sàng.");
            }

            return false;
        }
        Interlocked.Exchange(ref _cameraNotReadyLogged, 0);

        try
        {
            ct.ThrowIfCancellationRequested();
            using var scope = _scopeFactory.CreateScope();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(
                    new ProcessVdsFrameCommand(
                        json, isBackfill, completionId, processingDeadlineUtc))
                .AsTask()
                .WaitAsync(ct)
                .ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Ingestion] Không đưa được frame vào queue {Queue}.", VdsQueues.Data);
            return false;
        }
    }
}

/// <summary>
/// Author: KienLT
/// Description: Tên local queue của Wolverine dùng cho ingestion VDS.
/// Created date: 25/08/2026
/// </summary>
public static class VdsQueues
{
    public const string Data = "vds-data";
}
