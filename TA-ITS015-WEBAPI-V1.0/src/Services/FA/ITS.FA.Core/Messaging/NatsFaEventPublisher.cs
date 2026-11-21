using ITS.FA.Core.Configuration;
using ITS.FA.Core.Models;
using Modules.TMS.Core.Constants;
using Services.Shared.Messaging;
using Shared.DTO.Enums;
using Shared.Utility.Utilities;

namespace ITS.FA.Core.Messaging;

/// <summary>
/// Phát event FA qua <see cref="TransportManager"/>, cùng chính sách với VdsPublisher: chưa cấu hình hoặc chưa kết
/// nối NATS thì bỏ qua êm (cảnh báo một lần), lỗi phát không làm đứt vòng quét.
/// Author: ChuongLP
/// Created date: 02/11/2026
/// </summary>
public sealed class NatsFaEventPublisher : IFaEventPublisher
{
    private const string RegistrationMessageKey = "lz.message.tmsIncident.registrationNotify";

    private readonly TransportManager _transport;
    private readonly FaOptions _options;
    private readonly ILogger<NatsFaEventPublisher> _logger;
    private int _warnedNotConnected;

    public NatsFaEventPublisher(TransportManager transport, IOptions<FaOptions> options, ILogger<NatsFaEventPublisher> logger)
    {
        _transport = transport;
        _options = options.Value;
        _logger = logger;
    }

    public Task PublishIncidentAsync(FaCreatedIncident created, CancellationToken ct)
    {
        if (!_options.Publish.Incident) return Task.CompletedTask;

        var (incident, type) = created;
        var now = BaseUtil.TimeServer.GetTimeServer().Ticks;
        var state = (int)BaseEnums.TmsIncidentStateEnum.Registration;

        return SendAsync(FaSubjects.Incident, new FaIncidentPayload
        {
            IncidentId = incident.ID,
            Code = incident.Code,
            Name = incident.Name,
            EventTypeId = incident.EventTypeId,
            EventTypeCode = type?.Code,
            EventTypeName = type?.Name,
            EventGroup = incident.EventGroup ?? IncidentEventGroupConst.Normalize(type?.Type),
            EquipmentId = incident.EquipmentId,
            ZoneId = incident.ZoneId,
            Km = incident.KmNumber,
            Met = incident.MetNumber,
            StartDate = incident.StartDate,
            State = incident.State,
            Remark = incident.Remark,
            WarningType = type?.WarningType,
            Icon = type?.Url,
            Priority = type?.Priority,
            BroadcastCode = $"{incident.ID}-{now}-{state}",
            BroadcastMessage = new FaBroadcastMessage(RegistrationMessageKey, _options.Incident.Source, incident.Name),
            BroadcastDateTime = now,
        }, ct);
    }

    public Task PublishAlarmAsync(FaAlarmEvent alarm, string? incidentId, CancellationToken ct)
    {
        if (!_options.Publish.State || alarm.Point is not { } point) return Task.CompletedTask;

        return SendAsync(FaSubjects.Data, new FaStatePayload
        {
            Type = FaStatePayload.TypeAlarm,
            Id = point.ID,
            Address = point.PointAddress,
            Name = point.Description,
            DeviceType = point.TypeObject?.ToString(),
            Km = point.KmNumber,
            Met = point.MetNumber,
            Location = point.Location,
            IsActive = alarm.IsActive,
            IncidentId = incidentId,
            Time = BaseUtil.TimeServer.GetTimeServer(),
        }, ct);
    }

    public Task PublishDoorAsync(FaAlarmEvent door, CancellationToken ct)
    {
        if (!_options.Publish.State || door.Door is not { } item) return Task.CompletedTask;

        return SendAsync(FaSubjects.Data, new FaStatePayload
        {
            Type = FaStatePayload.TypeDoor,
            Id = item.ID,
            Address = item.ModbusAddress,
            Name = item.Description,
            DeviceType = item.DeviceType,
            Km = item.KmNumber,
            Met = item.MetNumber,
            Location = item.Location,
            IsActive = door.IsActive,
            Time = BaseUtil.TimeServer.GetTimeServer(),
        }, ct);
    }

    private async Task SendAsync(string subject, object payload, CancellationToken ct)
    {
        try
        {
            // Idempotent; lỗi kết nối được NatsService tự thử lại ngầm nên không chặn vòng quét.
            if (!_transport.IsConnected) await _transport.ConnectAsync(ct).ConfigureAwait(false);

            if (!_transport.IsConnected)
            {
                if (Interlocked.Exchange(ref _warnedNotConnected, 1) == 0)
                    _logger.LogWarning("[Publish] Chưa kết nối NATS, bỏ qua bản tin. Dữ liệu vẫn được ghi DB. Subject đầu tiên bị bỏ: {Subject}", subject);
                return;
            }

            Interlocked.Exchange(ref _warnedNotConnected, 0);
            if (await _transport.PublishAsync(subject, payload, ct).ConfigureAwait(false))
                _logger.LogInformation("[Publish] {Subject}", subject);
            else
                _logger.LogWarning("[Publish] {Subject} trả về false.", subject);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Publish] {Subject} thất bại, bỏ qua bản tin này (dữ liệu đã ghi DB).", subject);
        }
    }
}
