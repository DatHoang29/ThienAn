using Microsoft.Extensions.Logging;
using Services.Sample.DeviceA;
using Services.Sample.DeviceB;
using Services.Shared.Devices;
using Services.Shared.Logging;
using Shared.Utility.Utilities;

namespace Services.Sample.Features.Devices;

/// <summary>
/// Author: TAC Team
/// Description: Mô phỏng thu thập dữ liệu từ Device A và Device B rồi tổng hợp về một kết quả duy nhất.
/// Mỗi chu kỳ: sinh số đo canonical -> Encode sang frame theo protocol từng thiết bị (mô phỏng thiết bị
/// phát dữ liệu) -> Decode ngược lại qua registry -> tổng hợp. Nhờ vậy cả hai chiều codec đều được chạy thật.
/// Created date: 17/07/2026
/// </summary>
public sealed class DeviceAggregationService(DeviceMessageConverter converter)
{
    public AggregatedReading Acquire(string deviceAId, string deviceBId)
    {
        if (string.IsNullOrWhiteSpace(deviceAId))
            throw new ArgumentException("Device A id is required.", nameof(deviceAId));
        if (string.IsNullOrWhiteSpace(deviceBId))
            throw new ArgumentException("Device B id is required.", nameof(deviceBId));

        var observedAtUtc = BaseUtil.TimeServer.GetTimeServer().ToUniversalTime();

        var readings = new List<DeviceReading>
        {
            AcquireFromDevice(DeviceACodec.TypeCode, DeviceACodec.Version, deviceAId, observedAtUtc),
            AcquireFromDevice(DeviceBCodec.TypeCode, DeviceBCodec.Version, deviceBId, observedAtUtc),
        };

        var values = readings.Select(reading => reading.Value).ToArray();
        var status = readings.All(reading =>
            string.Equals(reading.Status, "ok", StringComparison.OrdinalIgnoreCase))
            ? "ok"
            : "degraded";

        var aggregate = new AggregatedReading(
            Guid.NewGuid().ToString("N"),
            observedAtUtc,
            readings,
            readings.Count,
            Math.Round(values.Average(), 2),
            values.Min(),
            values.Max(),
            status);

        LogManager.Log(
            LogLevel.Debug,
            $"Aggregated {aggregate.DeviceCount} device reading(s): avg={aggregate.Average}, status={aggregate.Status}",
            kind: LogKind.Device);

        return aggregate;
    }

    private DeviceReading AcquireFromDevice(
        string deviceType,
        string protocolVersion,
        string deviceId,
        DateTime observedAtUtc)
    {
        // Số đo giả lập từ cảm biến. Trong hệ thống thật, đây là dữ liệu do thiết bị gửi về.
        var sampledValue = Math.Round(Random.Shared.NextDouble() * 100.0, 2);

        var canonical = new CanonicalDeviceMessage
        {
            DeviceId = deviceId,
            ObservedAtUtc = observedAtUtc,
            Value = sampledValue,
            Status = "ok",
        };

        var target = new DeviceTarget
        {
            DeviceType = deviceType,
            ProtocolVersion = protocolVersion,
            DeviceId = deviceId,
        };

        // Encode: mô phỏng thiết bị đóng gói frame theo protocol riêng.
        var frame = converter.Encode(canonical, target);

        // Decode: pipeline nhận frame và đưa về canonical để xử lý thống nhất.
        var decoded = converter.Decode(frame);

        return new DeviceReading(
            frame.DeviceType,
            frame.ProtocolVersion,
            decoded.DeviceId,
            decoded.Value,
            decoded.Status,
            decoded.ObservedAtUtc);
    }
}
