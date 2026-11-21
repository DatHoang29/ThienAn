using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Services.Sample.Features.DataFlow.Messages;
using Services.Sample.Features.Devices;
using Services.Shared.Events;
using Services.Shared.Hosting;
using Services.Shared.Logging;
using Wolverine;

namespace Services.Sample.Manager;

/// <content>
/// Author: TAC Team
/// Phần Simulation của <see cref="SampleServiceManager"/>: implement <see cref="IServiceSimulator"/>.
/// Tách ra file riêng để không trộn logic giả lập vào lifecycle chính — pattern mẫu cho các manager
/// lớn có nhiều nhóm chức năng (mỗi nhóm 1 file .partial.cs).
/// Created date: 20/07/2026
/// </content>
public sealed partial class SampleServiceManager
{
    private static readonly JsonSerializerOptions SimulationPayloadOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Sinh 1 batch dữ liệu thiết bị giả lập (random readings) và đẩy vào common queue + phát activity
    /// — mô phỏng 1 chu kỳ <see cref="DeviceAcquisitionRunner"/> mà không cần device/NATS thật.
    /// Dùng để operator (WPF) test pipeline DataWorker + persistence + activity feed khi môi trường thật chưa sẵn sàng.
    /// Idempotent: mỗi lần gọi sinh batch mới với AggregateId unique.
    /// </summary>
    public async Task SimulateBatchAsync(CancellationToken cancellationToken = default)
    {
        // Resolve lazily qua IServiceProvider — tránh nhồi dependency chỉ dùng trong Simulation vào constructor.
        var bus = _services.GetRequiredService<IMessageBus>();
        var events = _services.GetRequiredService<ActivityPublisher>();

        // Đọc cấu hình device hiện tại để dùng DeviceAId/DeviceBId cho giống thật.
        var devices = _configManager.Current.Devices;
        var deviceAId = string.IsNullOrWhiteSpace(devices.DeviceAId) ? "DEVICE-A-01" : devices.DeviceAId;
        var deviceBId = string.IsNullOrWhiteSpace(devices.DeviceBId) ? "DEVICE-B-01" : devices.DeviceBId;

        var nowUtc = DateTime.UtcNow;

        // Sinh 2 reading giả (Device A + Device B) — random 0-100, status "ok".
        var readingA = new DeviceReading(
            DeviceType: "DeviceA",
            ProtocolVersion: "v1",
            DeviceId: deviceAId,
            Value: Math.Round(Random.Shared.NextDouble() * 100.0, 2),
            Status: "ok",
            ObservedAtUtc: nowUtc);

        var readingB = new DeviceReading(
            DeviceType: "DeviceB",
            ProtocolVersion: "v1",
            DeviceId: deviceBId,
            Value: Math.Round(Random.Shared.NextDouble() * 100.0, 2),
            Status: "ok",
            ObservedAtUtc: nowUtc);

        var readings = new List<DeviceReading> { readingA, readingB };
        var values = readings.Select(r => r.Value).ToList();

        var aggregate = new AggregatedReading(
            AggregateId: $"sim-{nowUtc:yyyyMMddHHmmssfff}",
            AggregatedAtUtc: nowUtc,
            Readings: readings,
            DeviceCount: readings.Count,
            Average: Math.Round(values.Average(), 2),
            Min: values.Min(),
            Max: values.Max(),
            Status: "ok");

        // Phát activity chi tiết từng reading + aggregate để UI thấy rõ flow.
        foreach (var reading in readings)
        {
            events.Publish(
                "Simulate",
                $"{reading.DeviceType}/{reading.ProtocolVersion} {reading.DeviceId}: "
                + $"value={reading.Value}, status={reading.Status}");
        }

        events.Publish(
            "Simulate",
            $"{aggregate.DeviceCount} thiết bị (giả lập) -> avg={aggregate.Average}, "
            + $"min={aggregate.Min}, max={aggregate.Max}, status={aggregate.Status}",
            ActivityLevel.Success);

        // Serialize + publish ProcessDataCommand qua Wolverine local queue — handler xử lý như message từ nguồn thật.
        var payloadJson = JsonSerializer.Serialize(aggregate, SimulationPayloadOptions);
        var message = new ProcessDataCommand(
            "simulation",
            aggregate.AggregateId,
            aggregate.AggregatedAtUtc,
            payloadJson);

        await bus.PublishAsync(message).ConfigureAwait(false);

        events.Publish("Simulate", $"Đã đẩy batch giả lập {aggregate.AggregateId} vào hàng đợi xử lý.");

        LogManager.Info(
            $"Simulated batch {aggregate.AggregateId}: avg={aggregate.Average}, deviceCount={aggregate.DeviceCount}",
            kind: LogKind.Device);
    }
}
