namespace Modules.VDS.Core.Options;

/// <summary>
/// Author: KienLT
/// Description: Cấu hình hàng đợi ảnh đăng ký VDS (section VdsRegistrationImages).
/// Created date: 02/10/2026
/// </summary>
public sealed class VdsRegistrationImageOptions
{
    public const string Section = "VdsRegistrationImages";
    public string StateDirectory { get; set; } = Path.Combine(AppContext.BaseDirectory, "state", "vds-registration-images");
    public int WaitTimeoutSeconds { get; set; } = 600;
    public int PollSeconds { get; set; } = 5;
    public int BatchSize { get; set; } = 100;
    public int EffectiveWaitSeconds => Math.Clamp(WaitTimeoutSeconds, 1, 86400);
    public int EffectivePollSeconds => Math.Clamp(PollSeconds, 1, 60);
    public int EffectiveBatchSize => Math.Clamp(BatchSize, 1, 1000);
}
