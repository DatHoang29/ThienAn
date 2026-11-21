using System.IO;
using System.Net;
using System.Net.Sockets;
using NModbus;
using NModbus.Data;

namespace ITS.FA.Wpf.Simulation;

/// <summary>
/// Tủ báo cháy giả: Modbus TCP slave chạy ngay trong form, chỉ nghe trên 127.0.0.1. Dịch vụ đọc nó qua cùng
/// FaModbusClient như tủ thật nên toàn bộ đường đi từ Modbus đến sự cố đều được chạy.
/// Địa chỉ dùng quy ước 1-based của dịch vụ; khi ghi vào bộ nhớ slave thì trừ 1.
/// Author: ChuongLP
/// Created date: 02/11/2026
/// </summary>
public sealed class FaPanelSimulator : IAsyncDisposable
{
    private readonly byte _unitId;
    private readonly ushort _fireCountAddress;
    private readonly ushort _unknownFireAddress;
    private readonly DefaultSlaveDataStore _store = new();
    private TcpListener? _listener;
    private IModbusSlaveNetwork? _network;
    private CancellationTokenSource? _cts;
    private Task? _listenTask;

    public FaPanelSimulator(byte unitId, ushort fireCountAddress, ushort unknownFireAddress)
    {
        _unitId = unitId;
        _fireCountAddress = fireCountAddress;
        _unknownFireAddress = unknownFireAddress;
    }

    public bool IsRunning => _listenTask is { IsCompleted: false };

    /// <summary>Port thực tế đang nghe (truyền 0 để hệ điều hành tự chọn).</summary>
    public int Port { get; private set; }

    public void Start(int port)
    {
        if (IsRunning) return;

        _listener = new TcpListener(IPAddress.Loopback, port);
        _listener.Start();
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;

        var factory = new ModbusFactory();
        _network = factory.CreateSlaveNetwork(_listener);
        _network.AddSlave(factory.CreateSlave(_unitId, _store));

        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        _listenTask = Task.Run(() => _network.ListenAsync(token));
    }

    /// <summary>Đặt trạng thái một điểm hoặc cửa (discrete input).</summary>
    public void SetPoint(int address, bool isActive) =>
        _store.CoilInputs.WritePoints((ushort)(address - 1), [isActive]);

    /// <summary>Đặt số điểm đang báo cháy (input register).</summary>
    public void SetFireCount(int count) =>
        _store.InputRegisters.WritePoints((ushort)(_fireCountAddress - 1), [(ushort)Math.Max(0, count)]);

    /// <summary>Báo cháy chưa rõ nguồn: còn bật thì dịch vụ coi là hệ thống CHƯA reset dù số báo cháy = 0.</summary>
    public void SetUnknownFire(bool isActive) => SetPoint(_unknownFireAddress, isActive);

    public async ValueTask DisposeAsync()
    {
        if (_cts is not null) await _cts.CancelAsync().ConfigureAwait(false);
        _listener?.Stop();
        _network?.Dispose();

        if (_listenTask is not null)
        {
            try { await _listenTask.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false); }
            catch (Exception ex) when (ex is OperationCanceledException or TimeoutException or SocketException or ObjectDisposedException or IOException or InvalidOperationException) { }
        }

        _cts?.Dispose();
        _cts = null;
        _listenTask = null;
    }
}
