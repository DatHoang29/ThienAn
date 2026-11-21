namespace Services.Vds.Sources.Backfill;

/// <summary>
/// Author: KienLT
/// Description: Chính sách bù lịch sử qua VDS API. Tắt mặc định; khi bật, mọi frame lấy về vẫn đi qua
/// local queue <c>vds-data</c> và processor raw hiện tại.
/// Created date: 26/08/2026
/// </summary>
public sealed class BackfillOptions
{
    public const int MaxPageLimit = 500;

    /// <summary>Công tắc tổng. false = không gọi API history ở startup hoặc reconnect.</summary>
    public bool Enabled { get; set; }

    public bool BackfillOnStartup { get; set; } = true;
    public bool BackfillOnReconnect { get; set; } = true;

    /// <summary>
    /// true = history lỗi thì chưa đọc live SSE, để checkpoint không nhảy qua khoảng trống.
    /// </summary>
    public bool RequireSuccessBeforeLive { get; set; } = true;

    // Initial scan only when no durable cursor exists. Existing gaps are never truncated.
    public int BackfillMaxHours { get; set; } = 24;
    public string HistoryPath { get; set; } = "/api/v1/events";
    public int PageLimit { get; set; } = MaxPageLimit;
    public int MaxPages { get; set; } = 40;

    /// <summary>Raw slice chưa dùng media nên mặc định không tải để giảm payload.</summary>
    public bool IncludeMedia { get; set; }

    /// <summary>Lùi cursor để bù các event đến lệch thứ tự quanh ranh giới checkpoint.</summary>
    public int OverlapSeconds { get; set; } = 60;

    public int RequestTimeoutSeconds { get; set; } = 60;

    /// <summary>Trần chờ một frame đi hết Wolverine → QuestDB → checkpoint.</summary>
    public int FrameProcessingTimeoutSeconds { get; set; } = 300;

    /// <summary>Trần response JSON một trang trước khi parse, chống endpoint lỗi làm đầy RAM.</summary>
    public int MaxResponseCharacters { get; set; } = 20_000_000;

    public int EffectiveMaxHours => Math.Clamp(BackfillMaxHours, 1, 24 * 30);
    public int EffectivePageLimit => Math.Clamp(PageLimit, 1, MaxPageLimit);
    public int EffectiveMaxPages => Math.Max(1, MaxPages);
    public int EffectiveOverlapSeconds => Math.Max(0, OverlapSeconds);
    public int EffectiveRequestTimeoutSeconds => Math.Clamp(RequestTimeoutSeconds, 5, 600);
    public int EffectiveFrameProcessingTimeoutSeconds =>
        Math.Clamp(FrameProcessingTimeoutSeconds, 5, 3_600);
    public int EffectiveMaxResponseCharacters =>
        Math.Clamp(MaxResponseCharacters, 1_024, 100_000_000);

    public BackfillOptions Clone() => new()
    {
        Enabled = Enabled,
        BackfillOnStartup = BackfillOnStartup,
        BackfillOnReconnect = BackfillOnReconnect,
        RequireSuccessBeforeLive = RequireSuccessBeforeLive,
        BackfillMaxHours = BackfillMaxHours,
        HistoryPath = HistoryPath,
        PageLimit = PageLimit,
        MaxPages = MaxPages,
        IncludeMedia = IncludeMedia,
        OverlapSeconds = OverlapSeconds,
        RequestTimeoutSeconds = RequestTimeoutSeconds,
        FrameProcessingTimeoutSeconds = FrameProcessingTimeoutSeconds,
        MaxResponseCharacters = MaxResponseCharacters,
    };
}
