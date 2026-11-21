namespace Services.Sample.Features.Devices;

/// <summary>
/// Author: TAC Team
/// Description: Một số đo đã giải mã từ một thiết bị (đã đưa về canonical).
/// Created date: 17/07/2026
/// </summary>
public sealed record DeviceReading(
    string DeviceType,
    string ProtocolVersion,
    string DeviceId,
    double Value,
    string Status,
    DateTime ObservedAtUtc);

/// <summary>
/// Author: TAC Team
/// Description: Kết quả tổng hợp từ nhiều thiết bị (Device A + Device B ...) trong một chu kỳ acquire.
/// Created date: 17/07/2026
/// </summary>
public sealed record AggregatedReading(
    string AggregateId,
    DateTime AggregatedAtUtc,
    IReadOnlyList<DeviceReading> Readings,
    int DeviceCount,
    double Average,
    double Min,
    double Max,
    string Status);
