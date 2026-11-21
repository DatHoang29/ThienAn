using System.Net.Sockets;
using ITS.FA.Core.Configuration;
using NModbus;

namespace ITS.FA.Core.Integration;

/// <summary>
/// Đọc tủ báo cháy qua Modbus TCP. Thay ModbusDataProvider (EasyModbus) của ITS008, giữ quy ước địa chỉ:
/// tham số là địa chỉ 1-based, khi gửi lệnh trừ 1.
/// Author: ChuongLP
/// Created date: 29/10/2026
/// </summary>
public sealed class FaModbusClient : IDisposable
{
    private readonly FaModbusOptions _options;
    private readonly ILogger<FaModbusClient> _logger;
    private TcpClient? _tcp;
    private IModbusMaster? _master;

    public FaModbusClient(IOptions<FaOptions> options, ILogger<FaModbusClient> logger)
    {
        _options = options.Value.Modbus;
        _logger = logger;
    }

    public bool IsConnected => _tcp is { Connected: true } && _master is not null;

    /// <summary>Mở kết nối. Ném exception khi không tới được — vòng ngoài sẽ chờ rồi chạy lại. Cũ: fn_Start + "Not found Modbus device".</summary>
    public async Task ConnectAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.Ip))
            throw new InvalidOperationException("Service:Modbus:Ip chưa khai báo.");

        Disconnect();

        var tcp = new TcpClient();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(_options.ConnectTimeoutMs);
        try
        {
            await tcp.ConnectAsync(_options.Ip, _options.Port, timeout.Token).ConfigureAwait(false);
        }
        catch
        {
            tcp.Dispose();
            throw new InvalidOperationException($"Not found Modbus device on {_options.Ip}:{_options.Port}");
        }

        _tcp = tcp;
        _master = new ModbusFactory().CreateMaster(tcp);
        _master.Transport.ReadTimeout = _options.ReadTimeoutMs;
        _master.Transport.WriteTimeout = _options.ReadTimeoutMs;
        // Không để NModbus tự thử lại: lỗi kết nối được xử lý bằng cách dựng lại cả phiên quét.
        _master.Transport.Retries = 0;
        _logger.LogInformation("Đã kết nối Modbus {Ip}:{Port}", _options.Ip, _options.Port);
    }

    public void Disconnect()
    {
        _master?.Dispose();
        _master = null;
        _tcp?.Dispose();
        _tcp = null;
    }

    /// <summary>Cũ: CountFireAlarm() — đọc input register FireCountAddress.</summary>
    public async Task<int> CountFireAlarmAsync() =>
        (await ReadInputRegistersAsync(_options.FireCountAddress).ConfigureAwait(false));

    /// <summary>
    /// Cũ: IsSystemReset() — không còn báo cháy chưa rõ nguồn và số báo cháy = 0 thì hệ thống đã reset.
    /// </summary>
    public async Task<bool> IsSystemResetAsync() =>
        !await ReadDiscreteInputAsync(_options.UnknownFireAlarmAddress).ConfigureAwait(false)
        && await CountFireAlarmAsync().ConfigureAwait(false) == 0;

    /// <summary>Cũ: IsDetectorOrButtonActived(address) — đọc discrete input của một điểm.</summary>
    public Task<bool> IsPointActiveAsync(int address) => ReadDiscreteInputAsync(address);

    private async Task<bool> ReadDiscreteInputAsync(int address)
    {
        var master = RequireMaster();
        var result = await master.ReadInputsAsync(_options.UnitId, ToWire(address), 1).ConfigureAwait(false);
        return result[0];
    }

    private async Task<int> ReadInputRegistersAsync(int address)
    {
        var master = RequireMaster();
        var result = await master.ReadInputRegistersAsync(_options.UnitId, ToWire(address), 1).ConfigureAwait(false);
        return result[0];
    }

    private static ushort ToWire(int address)
    {
        if (address < 1 || address > ushort.MaxValue + 1)
            throw new ArgumentOutOfRangeException(nameof(address), address, "Địa chỉ Modbus phải từ 1 đến 65536.");
        return (ushort)(address - 1);
    }

    private IModbusMaster RequireMaster() =>
        _master is not null && IsConnected
            ? _master
            : throw new InvalidOperationException("Mất kết nối Modbus.");

    public void Dispose() => Disconnect();
}
