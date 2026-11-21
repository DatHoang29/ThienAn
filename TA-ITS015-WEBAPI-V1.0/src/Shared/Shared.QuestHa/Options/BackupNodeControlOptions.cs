namespace Shared.QuestHa.Options;

/// <summary>
/// Author: KienLT
/// Description: Điều khiển Windows service local, phải cấu hình tường minh.
/// Created date: 28/10/2026
/// </summary>
public sealed class BackupNodeControlOptions
{
    public string RootPath { get; set; } = "";
    public string ServiceName { get; set; } = "";
}
