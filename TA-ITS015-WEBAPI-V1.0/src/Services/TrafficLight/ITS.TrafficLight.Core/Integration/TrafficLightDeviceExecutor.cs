using System.Diagnostics;
using ITS.TrafficLight.Core.Messaging;
using Modules.Integration.TrafficLight;
using Modules.Integration.TrafficLight.Base.Enums;
using Modules.Integration.TrafficLight.Base.Models;

namespace ITS.TrafficLight.Core.Integration;

/// <summary>
/// Thực thi lệnh điều khiển Đèn tín hiệu giao thông ra phần cứng qua IntgTrafficLightManager.
/// Hỗ trợ cả Siemens S7-1200 và Modbus TCP.
/// Author: ChuongLP
/// Date: 05/10/2026
/// </summary>
public class TrafficLightDeviceExecutor
{
    private readonly IntgTrafficLightManager _manager;

    public TrafficLightDeviceExecutor(IntgTrafficLightManager manager)
    {
        _manager = manager;
    }

    public async Task<TrafficLightTargetResult> ExecuteAsync(
        string action,
        TrafficLightColor color,
        TrafficLightControlTarget target,
        bool simulate,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        if (simulate)
        {
            await Task.Delay(150, ct);
            sw.Stop();
            return new TrafficLightTargetResult
            {
                EquipmentId = target.EquipmentId,
                EquipmentCode = target.EquipmentCode,
                Success = true,
                Message = $"[GIẢ LẬP] Thực thi lệnh {action} ({color}) thành công",
                DurationMs = sw.ElapsedMilliseconds,
                Color = color.ToString(),
                PowerStatus = true
            };
        }

        var config = new TrafficLightConfig
        {
            Code = target.EquipmentCode,
            IpAddress = target.Ip.Trim(),
            Port = target.Port,
            Rack = target.Rack,
            Slot = target.Slot,
            DbNumber = target.DbNumber,
            TimeOn = target.TimeOn,
            TimeOff = target.TimeOff,
            RedOutput = target.RedOutput,
            YellowOutput = target.YellowOutput,
            GreenOutput = target.GreenOutput,
            FmOutput = target.FmOutput,
            TimeoutMs = target.TimeoutMs,
            PingBeforeConnect = target.PingBeforeConnect
        };

        try
        {
            switch (action.ToLowerInvariant())
            {
                case TrafficLightControlAction.SetColor:
                    var setRes = await _manager.SetColorAsync(target.Vendor, config, color, ct).ConfigureAwait(false);
                    sw.Stop();
                    return new TrafficLightTargetResult
                    {
                        EquipmentId = target.EquipmentId,
                        EquipmentCode = target.EquipmentCode,
                        Success = setRes.Success,
                        Message = setRes.Message,
                        DurationMs = setRes.DurationMs,
                        Color = color.ToString()
                    };

                case TrafficLightControlAction.GetStatus:
                    var statRes = await _manager.GetStatusAsync(target.Vendor, config, ct).ConfigureAwait(false);
                    sw.Stop();
                    return new TrafficLightTargetResult
                    {
                        EquipmentId = target.EquipmentId,
                        EquipmentCode = target.EquipmentCode,
                        Success = statRes.IsConnected,
                        Message = statRes.IsConnected ? "Đọc trạng thái đèn thành công" : "Không thể đọc trạng thái",
                        DurationMs = sw.ElapsedMilliseconds,
                        Color = statRes.CurrentColor.ToString(),
                        PowerStatus = statRes.PowerStatus,
                        RawDetail = statRes.RawDetail
                    };

                case TrafficLightControlAction.CheckConnect:
                    var connOk = await _manager.CheckConnectionAsync(target.Vendor, config, ct).ConfigureAwait(false);
                    sw.Stop();
                    return new TrafficLightTargetResult
                    {
                        EquipmentId = target.EquipmentId,
                        EquipmentCode = target.EquipmentCode,
                        Success = connOk,
                        Message = connOk ? "Kết nối thiết bị đèn tốt" : "Mất kết nối tới thiết bị đèn",
                        DurationMs = sw.ElapsedMilliseconds
                    };

                default:
                    sw.Stop();
                    return new TrafficLightTargetResult
                    {
                        EquipmentId = target.EquipmentId,
                        EquipmentCode = target.EquipmentCode,
                        Success = false,
                        Message = $"Lệnh không được hỗ trợ: {action}",
                        DurationMs = sw.ElapsedMilliseconds
                    };
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new TrafficLightTargetResult
            {
                EquipmentId = target.EquipmentId,
                EquipmentCode = target.EquipmentCode,
                Success = false,
                Message = $"Ngoại lệ khi điều khiển đèn: {ex.Message}",
                DurationMs = sw.ElapsedMilliseconds
            };
        }
    }
}
