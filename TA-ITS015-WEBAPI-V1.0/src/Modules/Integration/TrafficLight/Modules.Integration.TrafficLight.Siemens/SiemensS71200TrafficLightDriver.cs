using System.Diagnostics;
using Modules.Integration.TrafficLight.Base.Enums;
using Modules.Integration.TrafficLight.Base.Helpers;
using Modules.Integration.TrafficLight.Base.Interfaces;
using Modules.Integration.TrafficLight.Base.Models;
using S7.Net;

namespace Modules.Integration.TrafficLight.Siemens;

/// <summary>
/// Driver điều khiển Đèn tín hiệu giao thông qua PLC Siemens S7-1200.
/// Kế thừa và chuẩn hóa từ hệ thống Cam Lâm - Vĩnh Hảo:
/// - DB number mặc định: DB3.
/// - Byte 0 (DB3.DBB0): Trạng thái ON liên tục (0x01 = Green, 0x02 = Red, 0x00 = Off).
/// - DB3.DBB1 & DB3.DBB2: TimeOn & TimeOff khi chớp Đỏ (DB3.DBB0 = 4).
/// - DB3.DBB5 & DB3.DBB6: TimeOn & TimeOff khi chớp Xanh (DB3.DBB0 = 8).
/// Author: ChuongLP
/// Date: 05/10/2026
/// </summary>
public class SiemensS71200TrafficLightDriver : ITrafficLightIntegration
{
    public TrafficLightVendor Vendor => TrafficLightVendor.SiemensS71200;

    public async Task<bool> CheckConnectionAsync(TrafficLightConfig config, CancellationToken ct = default)
    {
        if (config.PingBeforeConnect)
        {
            var pingOk = await IntgTrafficLightNetHelper.PingAsync(config.IpAddress, 2000).ConfigureAwait(false);
            if (!pingOk) return false;
        }

        try
        {
            using var plc = new Plc(CpuType.S71200, config.IpAddress, config.Port, config.Rack, config.Slot);
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(config.TimeoutMs);

            await plc.OpenAsync(timeoutCts.Token).ConfigureAwait(false);
            var isConnected = plc.IsConnected;
            plc.Close();
            return isConnected;
        }
        catch
        {
            return false;
        }
    }

    public async Task<TrafficLightResult> SetColorAsync(TrafficLightConfig config, TrafficLightColor color, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        if (config.PingBeforeConnect)
        {
            var pingOk = await IntgTrafficLightNetHelper.PingAsync(config.IpAddress, 2000).ConfigureAwait(false);
            if (!pingOk)
            {
                sw.Stop();
                return new TrafficLightResult
                {
                    Success = false,
                    Message = $"Không thể ping tới IP PLC {config.IpAddress}",
                    DurationMs = sw.ElapsedMilliseconds,
                    Color = color
                };
            }
        }

        using var plc = new Plc(CpuType.S71200, config.IpAddress, config.Port, config.Rack, config.Slot);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(config.TimeoutMs);

        try
        {
            await plc.OpenAsync(timeoutCts.Token).ConfigureAwait(false);
            if (!plc.IsConnected)
            {
                sw.Stop();
                return new TrafficLightResult
                {
                    Success = false,
                    Message = $"Bắt tay kết nối PLC S7-1200 thất bại tại {config.IpAddress}:{config.Port}",
                    DurationMs = sw.ElapsedMilliseconds,
                    Color = color
                };
            }

            var db = config.DbNumber;
            // Đọc trạng thái byte 0 hiện tại
            var currentByte = (await plc.ReadBytesAsync(DataType.DataBlock, db, 0, 1, timeoutCts.Token).ConfigureAwait(false))[0];

            switch (color)
            {
                case TrafficLightColor.Off:
                    // Tắt cả xanh lẫn đỏ: bit 0 & bit 1 về 0
                    currentByte = (byte)(currentByte & ~0x03);
                    await plc.WriteBytesAsync(DataType.DataBlock, db, 0, new[] { currentByte }, timeoutCts.Token).ConfigureAwait(false);
                    break;

                case TrafficLightColor.Red:
                    // Bật đỏ (bit 1 = 1), tắt xanh (bit 0 = 0)
                    currentByte = (byte)((currentByte & ~0x01) | 0x02);
                    await plc.WriteBytesAsync(DataType.DataBlock, db, 0, new[] { currentByte }, timeoutCts.Token).ConfigureAwait(false);
                    break;

                case TrafficLightColor.Green:
                    // Bật xanh (bit 0 = 1), tắt đỏ (bit 1 = 0)
                    currentByte = (byte)((currentByte & ~0x02) | 0x01);
                    await plc.WriteBytesAsync(DataType.DataBlock, db, 0, new[] { currentByte }, timeoutCts.Token).ConfigureAwait(false);
                    break;

                case TrafficLightColor.BlinkRed:
                    // Ghi thời gian On/Off vào DB3.DBB1 & DBB2, set lệnh 4
                    await plc.WriteAsync(DataType.DataBlock, db, 1, Convert.ToByte(config.TimeOn), -1, timeoutCts.Token).ConfigureAwait(false);
                    await plc.WriteAsync(DataType.DataBlock, db, 2, Convert.ToByte(config.TimeOff), -1, timeoutCts.Token).ConfigureAwait(false);
                    await plc.WriteAsync(DataType.DataBlock, db, 0, (byte)4, -1, timeoutCts.Token).ConfigureAwait(false);
                    break;

                case TrafficLightColor.BlinkGreen:
                    // Ghi thời gian On/Off vào DB3.DBB5 & DBB6, set lệnh 8
                    await plc.WriteAsync(DataType.DataBlock, db, 5, Convert.ToByte(config.TimeOn), -1, timeoutCts.Token).ConfigureAwait(false);
                    await plc.WriteAsync(DataType.DataBlock, db, 6, Convert.ToByte(config.TimeOff), -1, timeoutCts.Token).ConfigureAwait(false);
                    await plc.WriteAsync(DataType.DataBlock, db, 0, (byte)8, -1, timeoutCts.Token).ConfigureAwait(false);
                    break;

                default:
                    // Yellow / BlinkYellow nếu phần cứng S7 hầm không có dây riêng thì tắt hoặc hỗ trợ mở rộng
                    currentByte = 0;
                    await plc.WriteBytesAsync(DataType.DataBlock, db, 0, new[] { currentByte }, timeoutCts.Token).ConfigureAwait(false);
                    break;
            }

            sw.Stop();
            return new TrafficLightResult
            {
                Success = true,
                Message = $"Đặt trạng thái đèn {color} thành công qua PLC S7-1200",
                DurationMs = sw.ElapsedMilliseconds,
                Color = color
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new TrafficLightResult
            {
                Success = false,
                Message = $"Lỗi khi điều khiển đèn PLC S7-1200: {ex.Message}",
                DurationMs = sw.ElapsedMilliseconds,
                Color = color
            };
        }
        finally
        {
            try { plc.Close(); } catch { }
        }
    }

    public async Task<TrafficLightStatusResult> GetStatusAsync(TrafficLightConfig config, CancellationToken ct = default)
    {
        try
        {
            using var plc = new Plc(CpuType.S71200, config.IpAddress, config.Port, config.Rack, config.Slot);
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(config.TimeoutMs);

            await plc.OpenAsync(timeoutCts.Token).ConfigureAwait(false);
            if (!plc.IsConnected)
            {
                return new TrafficLightStatusResult
                {
                    IsConnected = false,
                    RawDetail = $"Không thể kết nối tới PLC {config.IpAddress}:{config.Port}"
                };
            }

            var bytes = await plc.ReadBytesAsync(DataType.DataBlock, config.DbNumber, 0, 1, timeoutCts.Token).ConfigureAwait(false);
            plc.Close();

            var val = bytes[0];
            TrafficLightColor color;
            if (val == 4) color = TrafficLightColor.BlinkRed;
            else if (val == 8) color = TrafficLightColor.BlinkGreen;
            else if ((val & 0x02) != 0) color = TrafficLightColor.Red;
            else if ((val & 0x01) != 0) color = TrafficLightColor.Green;
            else color = TrafficLightColor.Off;

            return new TrafficLightStatusResult
            {
                IsConnected = true,
                CurrentColor = color,
                PowerStatus = true,
                RawDetail = $"Byte 0 = 0x{val:X2} ({val})"
            };
        }
        catch (Exception ex)
        {
            return new TrafficLightStatusResult
            {
                IsConnected = false,
                RawDetail = $"Lỗi đọc trạng thái S7: {ex.Message}"
            };
        }
    }
}
