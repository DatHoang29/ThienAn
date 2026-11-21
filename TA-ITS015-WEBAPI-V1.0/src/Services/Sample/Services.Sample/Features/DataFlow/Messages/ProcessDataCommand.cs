namespace Services.Sample.Features.DataFlow.Messages;

/// <summary>
/// Author: TAC Team
/// Description: Command đẩy dữ liệu đầu vào (từ NATS/API/device/simulation) vào pipeline xử lý.
/// Thay thế RawMessage DTO cũ — giờ là record immutable, route qua Wolverine local queue "sample-data"
/// đến <see cref="ProcessDataCommandHandler"/>. Source = "nats" | "api" | "devices" | "simulation".
/// Created date: 20/07/2026
/// </summary>
public sealed record ProcessDataCommand(
    string Source,
    string MessageId,
    DateTime OccurredAtUtc,
    string PayloadJson);
