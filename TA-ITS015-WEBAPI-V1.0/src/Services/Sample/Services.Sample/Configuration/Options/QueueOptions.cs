namespace Services.Sample.Configuration.Options;

/// <summary>
/// Author: TAC Team
/// Description: Cấu hình message queue (section "Service:Queue").
/// Created date: 16/07/2026
/// </summary>
public sealed class QueueOptions
{
    public int Capacity { get; set; } = 1000;
}
