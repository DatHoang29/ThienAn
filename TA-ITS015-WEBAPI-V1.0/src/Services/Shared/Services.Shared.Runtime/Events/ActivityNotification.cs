using MediatR;

namespace Services.Shared.Events;

/// <summary>
/// Author: TAC Team
/// Description: Event nội bộ phát khi có hoạt động mới (acquire/aggregate/store/nats/...).
/// Consumer subscribe qua INotificationHandler, tránh coupling trực tiếp vào publisher.
/// Created date: 17/07/2026
/// </summary>
public sealed record ActivityNotification(Activity Activity) : INotification;
