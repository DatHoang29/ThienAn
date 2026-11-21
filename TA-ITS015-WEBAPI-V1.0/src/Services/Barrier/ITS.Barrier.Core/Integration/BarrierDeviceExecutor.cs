using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ITS.Barrier.Core.Messaging;
using Modules.Integration.Barrier;
using Modules.Integration.Barrier.Base.Enums;
using Modules.Integration.Barrier.Base.Models;

namespace ITS.Barrier.Core.Integration;

/// <summary>
/// Thực thi lệnh điều khiển ra thiết bị phần cứng Barrier qua IntgBarrierManager.
/// Author: ChuongLP
/// Date: 05/10/2026
/// </summary>
public class BarrierDeviceExecutor
{
    private readonly IntgBarrierManager _manager;

    public BarrierDeviceExecutor(IntgBarrierManager manager)
    {
        _manager = manager;
    }

    public async Task<BarrierTargetResult> ExecuteAsync(string action, BarrierControlTarget target, bool simulate, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        if (simulate)
        {
            await Task.Delay(150, ct);
            sw.Stop();
            return new BarrierTargetResult
            {
                EquipmentId = target.EquipmentId,
                EquipmentCode = target.EquipmentCode,
                Success = true,
                Message = $"[GIẢ LẬP] Thực thi lệnh {action} thành công",
                DurationMs = sw.ElapsedMilliseconds,
                Status = action == BarrierControlAction.Open ? "Opened" : "Closed"
            };
        }

        var config = new BarrierConfig
        {
            Code = target.EquipmentCode,
            IpAddress = target.Ip.Trim(),
            Port = target.Port,
            Rack = target.Rack,
            Slot = target.Slot,
            OpenBit = target.OpenBit,
            CloseBit = target.CloseBit,
            StatusBit = target.StatusBit,
            LoopBarrierBit = target.LoopBarrierBit,
            TimeoutMs = target.TimeoutMs,
            PingBeforeConnect = target.PingBeforeConnect,
            PulseResetMs = target.PulseResetMs
        };

        try
        {
            switch (action.ToLowerInvariant())
            {
                case BarrierControlAction.Open:
                    var openRes = await _manager.OpenAsync(config, ct);
                    sw.Stop();
                    return new BarrierTargetResult
                    {
                        EquipmentId = target.EquipmentId,
                        EquipmentCode = target.EquipmentCode,
                        Success = openRes.Success,
                        Message = openRes.Message,
                        DurationMs = openRes.DurationMs,
                        Status = openRes.Success ? "Opened" : "Unknown"
                    };

                case BarrierControlAction.Close:
                    var closeRes = await _manager.CloseAsync(config, ct);
                    sw.Stop();
                    return new BarrierTargetResult
                    {
                        EquipmentId = target.EquipmentId,
                        EquipmentCode = target.EquipmentCode,
                        Success = closeRes.Success,
                        Message = closeRes.Message,
                        DurationMs = closeRes.DurationMs,
                        Status = closeRes.Success ? "Closed" : "Unknown"
                    };

                case BarrierControlAction.GetStatus:
                    var statRes = await _manager.GetStatusAsync(config, ct);
                    sw.Stop();
                    return new BarrierTargetResult
                    {
                        EquipmentId = target.EquipmentId,
                        EquipmentCode = target.EquipmentCode,
                        Success = statRes.IsConnected,
                        Message = statRes.IsConnected ? "Đọc trạng thái thành công" : "Không thể đọc trạng thái",
                        DurationMs = sw.ElapsedMilliseconds,
                        Status = statRes.Status.ToString(),
                        LoopDetected = statRes.LoopDetected,
                        RawDetail = statRes.RawDetail
                    };

                case BarrierControlAction.CheckConnect:
                    var connOk = await _manager.CheckConnectionAsync(config, ct);
                    sw.Stop();
                    return new BarrierTargetResult
                    {
                        EquipmentId = target.EquipmentId,
                        EquipmentCode = target.EquipmentCode,
                        Success = connOk,
                        Message = connOk ? "Kết nối PLC S7 tốt" : "Mất kết nối tới PLC",
                        DurationMs = sw.ElapsedMilliseconds
                    };

                default:
                    sw.Stop();
                    return new BarrierTargetResult
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
            return new BarrierTargetResult
            {
                EquipmentId = target.EquipmentId,
                EquipmentCode = target.EquipmentCode,
                Success = false,
                Message = $"Ngoại lệ khi điều khiển: {ex.Message}",
                DurationMs = sw.ElapsedMilliseconds
            };
        }
    }
}
