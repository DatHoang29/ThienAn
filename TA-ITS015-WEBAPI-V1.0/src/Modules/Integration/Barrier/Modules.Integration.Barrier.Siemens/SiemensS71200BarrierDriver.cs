using System.Diagnostics;
using Modules.Integration.Barrier.Base.Enums;
using Modules.Integration.Barrier.Base.Helpers;
using Modules.Integration.Barrier.Base.Interfaces;
using Modules.Integration.Barrier.Base.Models;
using S7.Net;

namespace Modules.Integration.Barrier.Siemens;

/// <summary>
/// Driver barrier qua PLC Siemens S7-1200.
///
/// Chuyển nguyên luồng từ CLVH (CommonFunctions.ChangeBarrrierState + TACPLC_S71200.clsPLC_S71200):
///   1. Ping IP (NetworkHelper.fn_PingHost) — không tới thì dừng.
///   2. Kết nối Plc(CpuType.S71200, ip, rack 0, slot 0)            (fn_PLCConnect)
///   3. Ghi bit = true vào DB1.DBX0.1 (mở) / DB1.DBX0.2 (đóng)       (fn_KichChan_PLC(bit, true))
///   4. Ngắt kết nối                                                 (fn_DisConnectPLC)
///
/// BỎ so với bản cũ: fn_ConnectPLCServer (socket cổng 2000 nhận text sự kiện BR_MO/LA_ON...).
/// Web CLVH gọi hàm này nhưng không xử lý dữ liệu nhận về -> không ảnh hưởng lệnh đóng/mở.
///
/// Mỗi lệnh mở 1 kết nối mới rồi đóng (giống cũ). Đơn giản, không giữ state; đổi lại tốn
/// ~100-300ms bắt tay S7 mỗi lệnh — chấp nhận được với tần suất điều khiển barrier.
/// Author: ChuongLP
/// Date: 05-10-2026
/// </summary>
public class SiemensS71200BarrierDriver : IBarrierIntegration
{
    public BarrierVendor Vendor => BarrierVendor.SiemensS71200;

    public async Task<BarrierCommandResult> SendCommandAsync(BarrierConfig config, BarrierCommandType command, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        string? targetBit = command switch
        {
            BarrierCommandType.Open => config.OpenBit,
            BarrierCommandType.Close => config.CloseBit,
            _ => null
        };

        if (targetBit is null)
            return Fail(sw, $"Lệnh {command} không được hỗ trợ trên PLC S7-1200");

        if (!IntgBarrierAddressHelper.TryParseDbBit(targetBit, out var db, out var startByte, out var bit))
            return Fail(sw, $"Địa chỉ bit '{targetBit}' không hợp lệ (mẫu chuẩn: DB1.DBX0.1)");

        if (config.PingBeforeConnect && !await IntgBarrierNetHelper.PingAsync(config.IpAddress))
            return Fail(sw, $"Không ping được tới PLC {config.IpAddress}");

        try
        {
            using var linkedCts = CreateTimeoutToken(config.TimeoutMs, ct);
            using var plc = CreatePlc(config);

            await plc.OpenAsync(linkedCts.Token);
            if (!plc.IsConnected)
                return Fail(sw, $"Không kết nối được tới PLC {config.IpAddress}:{config.Port}");

            // Giữ hành vi cũ: chỉ ghi true, PLC tự reset.
            await plc.WriteBitAsync(DataType.DataBlock, db, startByte, bit, true, linkedCts.Token);

            // Tùy chọn: tự trả bit về false nếu PLC không tự reset (mặc định tắt).
            if (config.PulseResetMs > 0)
            {
                await Task.Delay(config.PulseResetMs, linkedCts.Token);
                await plc.WriteBitAsync(DataType.DataBlock, db, startByte, bit, false, linkedCts.Token);
            }

            plc.Close();
            sw.Stop();
            return new BarrierCommandResult
            {
                Success = true,
                Message = $"{command} OK — ghi {targetBit}=true" + (config.PulseResetMs > 0 ? $", reset sau {config.PulseResetMs}ms" : string.Empty),
                DurationMs = sw.ElapsedMilliseconds
            };
        }
        catch (OperationCanceledException)
        {
            return Fail(sw, $"Quá thời gian {config.TimeoutMs}ms khi điều khiển PLC {config.IpAddress}");
        }
        catch (Exception ex)
        {
            return Fail(sw, $"Lỗi PLC {config.IpAddress}: {ex.Message}");
        }
    }

    public async Task<BarrierStatusResult> GetStatusAsync(BarrierConfig config, CancellationToken ct = default)
    {
        try
        {
            using var linkedCts = CreateTimeoutToken(config.TimeoutMs, ct);
            using var plc = CreatePlc(config);

            await plc.OpenAsync(linkedCts.Token);
            if (!plc.IsConnected)
                return new BarrierStatusResult { IsConnected = false, RawDetail = "Không kết nối được PLC" };

            var statusBit = await ReadBitAsync(plc, config.StatusBit, linkedCts.Token);
            var loopBit = await ReadBitAsync(plc, config.LoopBarrierBit, linkedCts.Token);
            plc.Close();

            // CHƯA KIỂM CHỨNG ý nghĩa bit trạng thái trên tủ thật (CLVH không đọc bit này).
            // Tạm hiểu: true = đóng (DLL cũ đặt tên trùng sInput_DongBarie).
            return new BarrierStatusResult
            {
                IsConnected = true,
                Status = statusBit switch
                {
                    true => BarrierStatusType.Closed,
                    false => BarrierStatusType.Opened,
                    _ => BarrierStatusType.Unknown
                },
                LoopDetected = loopBit,
                RawDetail = $"{config.StatusBit}={statusBit?.ToString() ?? "n/a"}, {config.LoopBarrierBit}={loopBit?.ToString() ?? "n/a"}"
            };
        }
        catch (Exception ex)
        {
            return new BarrierStatusResult { IsConnected = false, RawDetail = ex.Message };
        }
    }

    public async Task<bool> CheckConnectionAsync(BarrierConfig config, CancellationToken ct = default)
    {
        try
        {
            using var linkedCts = CreateTimeoutToken(Math.Min(config.TimeoutMs, 3000), ct);
            using var plc = CreatePlc(config);
            await plc.OpenAsync(linkedCts.Token);
            var ok = plc.IsConnected;
            plc.Close();
            return ok;
        }
        catch
        {
            return false;
        }
    }

    #region Private

    private static Plc CreatePlc(BarrierConfig config)
        => new(CpuType.S71200, config.IpAddress.Trim(), config.Port, config.Rack, config.Slot);

    private static CancellationTokenSource CreateTimeoutToken(int timeoutMs, CancellationToken ct)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeoutMs);
        return cts;
    }

    private static async Task<bool?> ReadBitAsync(Plc plc, string address, CancellationToken ct)
    {
        if (!IntgBarrierAddressHelper.TryParseDbBit(address, out var db, out var startByte, out var bit))
            return null;

        var value = await plc.ReadAsync(DataType.DataBlock, db, startByte, VarType.Bit, 1, (byte)bit, ct);
        return value is bool b ? b : null;
    }

    private static BarrierCommandResult Fail(Stopwatch sw, string message)
    {
        sw.Stop();
        return new BarrierCommandResult { Success = false, Message = message, DurationMs = sw.ElapsedMilliseconds };
    }

    #endregion
}