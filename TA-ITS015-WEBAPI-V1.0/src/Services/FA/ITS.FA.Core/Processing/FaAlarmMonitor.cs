using ITS.FA.Core.Configuration;
using ITS.FA.Core.Integration;
using ITS.FA.Core.Messaging;
using ITS.FA.Core.Models;
using ITS.FA.Core.Persistence;

namespace ITS.FA.Core.Processing;

/// <summary>
/// Vòng quét báo cháy. Port NGUYÊN luồng ITS008:
///   ModbusDataProvider.Init + CheckAllDectector/CheckAllButtonAlarm/CheckAllDoorAlarm  → <see cref="RunSessionAsync"/>
///   FireAlarmManager (OnAlarm / OnDoor) + SqlDataEventHandler + SocketIOEventHandler    → <see cref="OnAlarmAsync"/>, <see cref="OnDoorAsync"/>
///
/// Hành vi giữ nguyên, kể cả những chỗ đáng chú ý:
///  - Đầu báo và nút ấn chỉ được quét khi tủ báo có cháy (số báo cháy &gt; 0) và hệ thống chưa reset.
///  - Một điểm hết báo (reset riêng) KHÔNG ghi DB và không phát event; DB chỉ xóa khi cả hệ thống reset
///    (ResetAllPoints).
///  - Cửa luôn được quét, đổi trạng thái mới ghi DB.
///  - Luồng lỗi nguồn AC / "hai báo cháy trở lên" của bản cũ đã bị tắt từ trước nên không port.
/// Author: ChuongLP
/// Created date: 29/10/2026
/// </summary>
public sealed class FaAlarmMonitor
{
    private readonly FaModbusClient _modbus;
    private readonly FaRepository _repository;
    private readonly FaIncidentService _incidents;
    private readonly IFaEventPublisher _publisher;
    private readonly FaOptions _options;
    private readonly ILogger<FaAlarmMonitor> _logger;

    /// <summary>Địa chỉ các điểm đang báo cháy. Cũ: ModbusDataProvider.AlarmActivedDevices.</summary>
    private readonly HashSet<int> _activeAddresses = new();

    /// <summary>
    /// DB còn điểm IsActive = true cần đưa về false khi tủ reset. Tách khỏi <see cref="_activeAddresses"/> vì một điểm
    /// có thể tắt (bị bỏ khỏi tập) trước khi số báo cháy về 0: lúc reset tập đã rỗng, nếu dựa vào tập thì bỏ qua
    /// bước reset và DB kẹt "đang báo" tới lần khởi động sau (lỗi có từ bản cũ).
    /// </summary>
    private bool _dbHasActive;

    public FaAlarmMonitor(FaModbusClient modbus,FaRepository repository,FaIncidentService incidents,IFaEventPublisher publisher,
        IOptions<FaOptions> options,ILogger<FaAlarmMonitor> logger)
    {
        _modbus = modbus;
        _repository = repository;
        _incidents = incidents;
        _publisher = publisher;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Một phiên quét: kết nối, nạp danh sách rồi quét liên tục. Chỉ thoát bằng exception (mất kết nối, lỗi DB)
    /// hoặc khi bị hủy — vòng ngoài chờ rồi gọi lại. Cũ: Init() ném exception, Program.Main bắt và chạy lại.
    /// </summary>
    public async Task RunSessionAsync(CancellationToken ct)
    {
        await _modbus.ConnectAsync(ct).ConfigureAwait(false);

        var points = await _repository.GetPointsAsync(ct).ConfigureAwait(false);
        var doors = await _repository.GetDoorsAsync(ct).ConfigureAwait(false);

        _activeAddresses.Clear();
        foreach (var point in points.Where(p => p.IsActive == true && p.PointAddress != null))
            _activeAddresses.Add(point.PointAddress!.Value);
        _dbHasActive = points.Any(p => p.IsActive == true);

        _logger.LogInformation("Đã nạp {Points} đầu báo/nút ấn và {Doors} cửa.", points.Count, doors.Count);

        while (!ct.IsCancellationRequested)
        {
            if (!_modbus.IsConnected) throw new InvalidOperationException("Disconnect to Modbus Server!");

            var isSystemReset = await _modbus.IsSystemResetAsync().ConfigureAwait(false);
            _logger.LogDebug("Is system reset: {Reset}", isSystemReset);

            if (isSystemReset && (_activeAddresses.Count > 0 || _dbHasActive))
            {
                try
                {
                    // Xóa tập đang báo SAU khi ghi DB thành công: lỗi thì chu kỳ sau thử lại (cũ: xóa trước, DB kẹt trạng thái báo).
                    await _repository.ResetAllPointsAsync(ct).ConfigureAwait(false);
                    _activeAddresses.Clear();
                    _dbHasActive = false;
                    points = await _repository.GetPointsAsync(ct).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Reset toàn bộ điểm báo cháy thất bại.");
                }
            }

            var fireCount = await _modbus.CountFireAlarmAsync().ConfigureAwait(false);
            _logger.LogDebug("Number Fire alarm: {Count}", fireCount);
            var isCommonAlarmActive = fireCount > 0;

            await CheckPointsAsync(FaDeviceType.Detector, isCommonAlarmActive, isSystemReset, points, ct).ConfigureAwait(false);
            await CheckPointsAsync(FaDeviceType.Button, isCommonAlarmActive, isSystemReset, points, ct).ConfigureAwait(false);
            await CheckDoorsAsync(doors, ct).ConfigureAwait(false);

            await Task.Delay(_options.Modbus.ScanIntervalMs, ct).ConfigureAwait(false);
        }
    }

    /// <summary>Cũ: CheckAllDectector / CheckAllButtonAlarm (hai hàm giống hệt nhau, chỉ khác loại).</summary>
    private async Task CheckPointsAsync(
        FaDeviceType type, bool isCommonAlarmActive, bool isSystemReset, IEnumerable<FaObjectData> points, CancellationToken ct)
    {
        if (!isCommonAlarmActive || isSystemReset) return;

        foreach (var point in points
                     .Where(p => p.TypeObject == (int)type && p.PointAddress != null)
                     .OrderBy(p => p.IsActive == true))
        {
            ct.ThrowIfCancellationRequested();
            var address = point.PointAddress!.Value;
            try
            {
                var isActive = await _modbus.IsPointActiveAsync(address).ConfigureAwait(false);
                _logger.LogDebug("FA ALARM ADDRESS {Address} - {Type} Km{Km}+{Met}: {Active}",
                    address, type, point.KmNumber, point.MetNumber, isActive);

                if (isActive && !_activeAddresses.Contains(address))
                {
                    // Chỉ đánh dấu đang báo khi đã xử lý xong; lỗi DB thì chu kỳ sau xử lý lại, không mất sự cố.
                    if (await OnAlarmAsync(new FaAlarmEvent { Point = point, IsActive = true }, ct).ConfigureAwait(false))
                        _activeAddresses.Add(address);
                }
                else if (!isActive && _activeAddresses.Remove(address))
                {
                    // Bản cũ: OnAlarmReset không có xử lý nào. Giữ nguyên — chỉ bỏ khỏi tập đang báo.
                    _logger.LogWarning("Điểm {Address} hết báo (bỏ qua, chờ reset toàn hệ thống).", address);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException && !IsConnectionFailure(ex))
            {
                _logger.LogError(ex, "Lỗi quét điểm {Address}.", address);
            }
        }
    }

    /// <summary>Cũ: CheckAllDoorAlarm.</summary>
    private async Task CheckDoorsAsync(IReadOnlyList<FaFireAlarmConfig> doors, CancellationToken ct)
    {
        foreach (var door in doors.Where(d => d.ModbusAddress != null))
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var isActive = await _modbus.IsPointActiveAsync(door.ModbusAddress!.Value).ConfigureAwait(false);
                _logger.LogDebug("ALARM ADDRESS {Address} - {Name}: {Active}", door.ModbusAddress, door.Description, isActive);

                if ((door.IsAlarm ?? false) != isActive)
                    await OnDoorAsync(new FaAlarmEvent { Door = door, IsActive = isActive }, ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException && !IsConnectionFailure(ex))
            {
                _logger.LogError(ex, "Lỗi quét cửa {Address}.", door.ModbusAddress);
            }
        }
    }

    /// <summary>
    /// Lỗi đường truyền Modbus phải thoát ra để dựng lại phiên, không nuốt theo từng điểm: nếu không,
    /// tủ treo thì mỗi điểm chờ hết timeout và một vòng quét kéo dài rất lâu.
    /// </summary>
    private static bool IsConnectionFailure(Exception ex) =>
        ex is IOException or TimeoutException or System.Net.Sockets.SocketException or ObjectDisposedException;

    /// <summary>
    /// Cũ: EventManager.OnAlarmActived → SqlDataEventHandler (cập nhật điểm + tạo sự cố) song song với
    /// SocketIOEventHandler (phát event). Trả false khi chưa tạo được sự cố để chu kỳ sau xử lý lại;
    /// kiểm tra trùng vị trí bảo đảm không sinh hai sự cố.
    /// </summary>
    private async Task<bool> OnAlarmAsync(FaAlarmEvent alarm, CancellationToken ct)
    {
        var point = alarm.Point!;
        _logger.LogWarning("Báo cháy tại Km{Km}+{Met}, điểm {Address}", point.KmNumber, point.MetNumber, point.PointAddress);

        await _repository.UpdatePointStateAsync(point.PointAddress!.Value, alarm.IsActive, _options.ServiceUserId, ct)
            .ConfigureAwait(false);
        if (alarm.IsActive) _dbHasActive = true;

        FaCreatedIncident? created;
        try
        {
            created = await _incidents.CreateForPointAsync(point, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Tạo sự cố báo cháy thất bại, sẽ thử lại ở chu kỳ sau.");
            return false;
        }

        if (created is not null)
            await _publisher.PublishIncidentAsync(created, ct).ConfigureAwait(false);
        await _publisher.PublishAlarmAsync(alarm, created?.Incident.ID, ct).ConfigureAwait(false);

        return true;
    }

    /// <summary>Cũ: EventManager.OnDoorActived → UpdateDoorState rồi phát EquipmentState "door".</summary>
    private async Task OnDoorAsync(FaAlarmEvent door, CancellationToken ct)
    {
        _logger.LogWarning("Cửa đổi trạng thái tại Km{Km}+{Met}, điểm {Address}",
            door.Door!.KmNumber, door.Door.MetNumber, door.Door.ModbusAddress);

        // Đổi trạng thái trong bộ nhớ SAU khi ghi DB: lỗi thì chu kỳ sau ghi lại (cũ: đổi trước, DB lệch đến lần đổi kế tiếp).
        await _repository.UpdateDoorStateAsync(door.Door.ID, door.IsActive, ct).ConfigureAwait(false);
        door.Door.IsAlarm = door.IsActive;

        await _publisher.PublishDoorAsync(door, ct).ConfigureAwait(false);
    }

    /// <summary>Cũ: ModbusDataProvider.Reset() khi phiên lỗi — ngắt kết nối để phiên sau dựng lại.</summary>
    public void Reset() => _modbus.Disconnect();
}
