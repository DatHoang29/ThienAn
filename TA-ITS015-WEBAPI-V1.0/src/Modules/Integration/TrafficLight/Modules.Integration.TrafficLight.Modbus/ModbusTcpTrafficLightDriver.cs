using System.Diagnostics;
using System.Net.Sockets;
using Modules.Integration.TrafficLight.Base.Enums;
using Modules.Integration.TrafficLight.Base.Helpers;
using Modules.Integration.TrafficLight.Base.Interfaces;
using Modules.Integration.TrafficLight.Base.Models;
using NModbus;

namespace Modules.Integration.TrafficLight.Modbus;

/// <summary>
/// Driver điều khiển Đèn tín hiệu giao thông qua giao thức Modbus TCP.
/// Kế thừa và chuẩn hóa từ hệ thống Cam Lâm - Vĩnh Hảo (SignalLightManager.cs):
/// - Sử dụng WriteSingleCoil / ReadCoils cho RedOutput, YellowOutput, GreenOutput, FmOutput.
/// - Cổng Modbus TCP mặc định: 502 (hoặc cấu hình tùy ý).
/// Author: ChuongLP
/// Date: 05/10/2026
/// </summary>
public class ModbusTcpTrafficLightDriver : ITrafficLightIntegration
{
    public TrafficLightVendor Vendor => TrafficLightVendor.ModbusTcp;

    public async Task<bool> CheckConnectionAsync(TrafficLightConfig config, CancellationToken ct = default)
    {
        return await IntgTrafficLightNetHelper.TestPortAsync(config.IpAddress, config.Port > 0 ? config.Port : 502, config.TimeoutMs).ConfigureAwait(false);
    }

    public async Task<TrafficLightResult> SetColorAsync(TrafficLightConfig config, TrafficLightColor color, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var port = config.Port > 0 ? config.Port : 502;

        try
        {
            using var tcpClient = new TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(config.TimeoutMs);

            await tcpClient.ConnectAsync(config.IpAddress.Trim(), port, cts.Token).ConfigureAwait(false);

            var factory = new ModbusFactory();
            var master = factory.CreateMaster(tcpClient);

            bool yellowState = false;
            bool redState = false;
            bool greenState = false;

            switch (color)
            {
                case TrafficLightColor.Off:
                    yellowState = false;
                    redState = false;
                    greenState = false;
                    break;

                case TrafficLightColor.Red:
                case TrafficLightColor.BlinkRed:
                    yellowState = false;
                    redState = true;
                    greenState = false;
                    break;

                case TrafficLightColor.Yellow:
                case TrafficLightColor.BlinkYellow:
                    yellowState = true;
                    redState = false;
                    greenState = false;
                    break;

                case TrafficLightColor.Green:
                case TrafficLightColor.BlinkGreen:
                    yellowState = false;
                    redState = false;
                    greenState = true;
                    break;
            }

            // Ghi trạng thái Coil (Lưu ý: ngõ ra trong CLVH là 1-based, Modbus address là 0-based)
            if (config.YellowOutput > 0)
                await master.WriteSingleCoilAsync(1, (ushort)(config.YellowOutput - 1), yellowState).ConfigureAwait(false);

            if (config.RedOutput > 0)
                await master.WriteSingleCoilAsync(1, (ushort)(config.RedOutput - 1), redState).ConfigureAwait(false);

            if (config.GreenOutput > 0)
                await master.WriteSingleCoilAsync(1, (ushort)(config.GreenOutput - 1), greenState).ConfigureAwait(false);

            sw.Stop();
            return new TrafficLightResult
            {
                Success = true,
                Message = $"Đặt trạng thái đèn {color} thành công qua Modbus TCP",
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
                Message = $"Lỗi Modbus TCP: {ex.Message}",
                DurationMs = sw.ElapsedMilliseconds,
                Color = color
            };
        }
    }

    public async Task<TrafficLightStatusResult> GetStatusAsync(TrafficLightConfig config, CancellationToken ct = default)
    {
        var port = config.Port > 0 ? config.Port : 502;
        try
        {
            using var tcpClient = new TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(config.TimeoutMs);

            await tcpClient.ConnectAsync(config.IpAddress.Trim(), port, cts.Token).ConfigureAwait(false);

            var factory = new ModbusFactory();
            var master = factory.CreateMaster(tcpClient);

            bool yellow = false;
            bool red = false;
            bool green = false;

            if (config.YellowOutput > 0)
            {
                var coils = await master.ReadCoilsAsync(1, (ushort)(config.YellowOutput - 1), 1).ConfigureAwait(false);
                yellow = coils[0];
            }

            if (config.RedOutput > 0)
            {
                var coils = await master.ReadCoilsAsync(1, (ushort)(config.RedOutput - 1), 1).ConfigureAwait(false);
                red = coils[0];
            }

            if (config.GreenOutput > 0)
            {
                var coils = await master.ReadCoilsAsync(1, (ushort)(config.GreenOutput - 1), 1).ConfigureAwait(false);
                green = coils[0];
            }

            TrafficLightColor color;
            if (red) color = TrafficLightColor.Red;
            else if (yellow) color = TrafficLightColor.Yellow;
            else if (green) color = TrafficLightColor.Green;
            else color = TrafficLightColor.Off;

            return new TrafficLightStatusResult
            {
                IsConnected = true,
                CurrentColor = color,
                PowerStatus = true,
                RawDetail = $"Red={red}, Yellow={yellow}, Green={green}"
            };
        }
        catch (Exception ex)
        {
            return new TrafficLightStatusResult
            {
                IsConnected = false,
                RawDetail = $"Lỗi đọc Modbus: {ex.Message}"
            };
        }
    }
}
