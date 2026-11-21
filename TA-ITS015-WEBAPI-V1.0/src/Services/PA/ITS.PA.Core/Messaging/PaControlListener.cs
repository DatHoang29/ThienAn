using System.Text.Json;
using ITS.PA.Core.Processing;
using Microsoft.Extensions.Hosting;
using Services.Shared.Messaging;
using Shared.Utility.Utilities;

namespace ITS.PA.Core.Messaging;

/// <summary>
/// Nhận lệnh trên <see cref="PaSubjects.Control"/>, gọi <see cref="PaControlService"/> rồi phát kết quả lên CÙNG kênh
/// để BE đối chiếu theo RequestId. Thay việc web gọi trực tiếp thư viện PA ở ITS008. Cùng giao thức với kênh VMS,
/// nhưng dùng TransportManager theo rule chung. Bỏ qua gói khác "request" để không xử lý lại kết quả của chính mình.
/// Author: ChuongLP
/// Created date: 02/11/2026
/// </summary>
public sealed class PaControlListener : BackgroundService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly TransportManager _transport;
    private readonly PaControlService _control;
    private readonly ILogger<PaControlListener> _logger;
    private CancellationToken _stopping;

    public PaControlListener(TransportManager transport, PaControlService control, ILogger<PaControlListener> logger)
    {
        _transport = transport;
        _control = control;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stopping = stoppingToken;

        // Kết nối lỗi không ném ra: NatsService tự thử lại ngầm và đăng ký lại subject khi kết nối được.
        await _transport.ConnectAsync(stoppingToken).ConfigureAwait(false);
        await _transport.SubscribeAsync(PaSubjects.Control, OnMessageAsync, stoppingToken).ConfigureAwait(false);
        _logger.LogInformation("Lắng nghe lệnh PA trên {Subject}. Đã kết nối NATS: {Connected}", PaSubjects.Control, _transport.IsConnected);
    }

    /// <summary>Trả về ngay: một lệnh đang chờ máy chủ PA không được chặn lệnh kế tiếp.</summary>
    private Task OnMessageAsync(string payload)
    {
        _ = Task.Run(() => HandleSafeAsync(payload), CancellationToken.None);
        return Task.CompletedTask;
    }

    private async Task HandleSafeAsync(string payload)
    {
        try
        {
            var request = Parse(payload);
            if (request is null) return;

            var result = await ExecuteAsync(request).ConfigureAwait(false);
            await PublishAsync(result).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Xử lý gói lệnh PA lỗi.");
        }
    }

    private PaControlRequest? Parse(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload)) return null;

        try
        {
            var request = JsonSerializer.Deserialize<PaControlRequest>(payload, Json);
            return string.Equals(request?.Type?.Trim(), PaControlType.Request, StringComparison.OrdinalIgnoreCase) ? request : null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Gói lệnh PA không đọc được JSON.");
            return null;
        }
    }

    private async Task<PaControlResult> ExecuteAsync(PaControlRequest request)
    {
        var action = request.Action?.Trim().ToLowerInvariant();
        var result = new PaControlResult { RequestId = request.RequestId, Action = action, TaskId = request.TaskId };

        var error = Validate(action, request);
        if (error is not null)
        {
            result.Message = error;
        }
        else if (request.Simulate)
        {
            result.Success = true;
            result.Message = "Giả lập thành công, không gọi máy chủ PA.";
        }
        else
        {
            try
            {
                var outcome = await _control.ExecuteAsync(ToCommand(action!, request), _stopping).ConfigureAwait(false);
                result.Success = outcome.Success;
                result.Message = outcome.Message;
                result.TaskId = outcome.TaskId ?? request.TaskId;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Thực thi lệnh PA {Action} lỗi. RequestId={RequestId}", action, request.RequestId);
                result.Message = ex.Message;
            }
        }

        result.FinishedAt = BaseUtil.TimeServer.GetTimeServer();
        _logger.LogInformation("Lệnh PA {Action} RequestId={RequestId}: {Success} {Message}", action, request.RequestId, result.Success, result.Message);
        return result;
    }

    /// <summary>Trả lý do từ chối, hoặc null khi hợp lệ.</summary>
    private static string? Validate(string? action, PaControlRequest request)
    {
        switch (action)
        {
            case PaControlAction.Stop:
                return string.IsNullOrWhiteSpace(request.TaskId) ? "Thiếu TaskId." : null;
            case PaControlAction.Play:
            case PaControlAction.SoundTest:
                if (string.IsNullOrWhiteSpace(request.SongName)) return "Thiếu SongName.";
                if (string.IsNullOrWhiteSpace(request.IncidentCode)) return "Thiếu IncidentCode.";
                return string.IsNullOrWhiteSpace(request.TaskId) ? "Thiếu TaskId." : null;
            default:
                return $"Action không hỗ trợ: {action}";
        }
    }

    private static PaControlCommand ToCommand(string action, PaControlRequest request) => new()
    {
        TurnOn = action != PaControlAction.Stop,
        Mode = action == PaControlAction.SoundTest ? PaControlMode.SoundTest : PaControlMode.Incident,
        IncidentCode = request.IncidentCode ?? string.Empty,
        TaskId = request.TaskId ?? string.Empty,
        SongName = request.SongName ?? string.Empty,
    };

    private async Task PublishAsync(PaControlResult result)
    {
        if (!_transport.IsConnected)
        {
            _logger.LogWarning("Mất kết nối NATS, không phát được kết quả RequestId={RequestId}.", result.RequestId);
            return;
        }

        // Chuỗi JSON được gửi nguyên văn, giữ camelCase như kết quả của kênh VMS.
        await _transport.PublishAsync(PaSubjects.Control, JsonSerializer.Serialize(result, Json)).ConfigureAwait(false);
    }
}
