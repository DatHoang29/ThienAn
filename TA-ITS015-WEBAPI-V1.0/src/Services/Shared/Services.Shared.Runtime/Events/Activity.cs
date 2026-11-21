namespace Services.Shared.Events;

/// <summary>
/// Author: TAC Team
/// Description: Mức độ nghiêm trọng của một hoạt động để UI tô màu phù hợp.
/// Created date: 17/07/2026
/// </summary>
public enum ActivityLevel
{
    Info,
    Success,
    Warning,
    Error,
}

/// <summary>
/// Author: TAC Team
/// Description: Một mục hoạt động của luồng xử lý. Được gói trong ActivityNotification
/// và phát qua MediatR (kênh event duy nhất thay cho delegate stream cũ).
/// Created date: 17/07/2026
/// </summary>
public sealed record Activity(
    DateTime TimestampLocal,
    string Stage,
    string Detail,
    ActivityLevel Level);
