namespace Services.Sample.Configuration.Options;

/// <summary>
/// Author: TAC Team
/// Description: Cấu hình API input source (section "Service:Api").
/// Created date: 16/07/2026
/// </summary>
public sealed class ApiOptions
{
    public bool Enabled { get; set; }

    public string Url { get; set; } = string.Empty;

    public int IntervalSeconds { get; set; } = 10;

    public int TimeoutSeconds { get; set; } = 30;
}
