namespace ITS.Sync.Infrastructure.Persistence;

/// <summary>
/// Hằng số ConfigId của DB nguồn và đích (SqlSugar tách DB theo ConfigId).
/// </summary>
public static class DbConstants
{
    public const string SourceDb = "Source"; // Dev_ITS10
    public const string TargetDb = "Target"; // DEV_ITS015_WP
}
