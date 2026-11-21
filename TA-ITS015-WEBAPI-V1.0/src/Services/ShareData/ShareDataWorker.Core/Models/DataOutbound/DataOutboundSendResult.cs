namespace ShareDataWorker.Core.Models.DataOutbound
{
    /// <summary>
    /// Author: DatHQ
    /// Description: Kết quả thực thi gửi hoặc lưu trữ gói tin kết xuất (Bước 3: Transport).
    ///              IsPossiblyDelivered: gói đã rời Worker mà không nhận được trả lời (đứt kết nối giữa chừng, hết giờ chờ), đối tác có thể đã nhận.
    /// Created date: 16/09/2026
    /// Modified date: 13/11/2026
    /// </summary>
    public sealed record DataOutboundSendResult(
        bool Success,
        long ByteSize,
        string? RelativePath,
        string? ErrorMessage = null,
        string? DetailJson = null,
        string? AlertCode = null,
        IReadOnlyList<string>? Warnings = null,
        bool IsPossiblyDelivered = false);
}
