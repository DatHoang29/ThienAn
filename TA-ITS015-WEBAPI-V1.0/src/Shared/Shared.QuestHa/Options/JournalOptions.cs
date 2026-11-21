namespace Shared.QuestHa.Options;

/// <summary>
/// Author: KienLT
/// Description: ghi journal theo lô (group commit). Mỗi append trước đây = 1 transaction + sp_getapplock tuần tự
/// toàn cục ⇒ trần ~165–190 ACK/s trên LAB; gom các append đang chờ thành 1 transaction (khoá 1 lần) cho mỗi lô.
/// Created date: 27/10/2026
/// </summary>
public sealed class JournalOptions
{
    /// <summary>Tắt = mỗi append 1 transaction như cũ (để so sánh/khẩn cấp).</summary>
    public bool AppendBatching { get; set; } = true;

    /// <summary>Số append tối đa trong 1 transaction. Không chờ cho đầy lô: lô = những gì đang xếp hàng.</summary>
    public int AppendMaxBatch { get; set; } = 200;

    /// <summary>Trần hàng đợi append chưa ghi; đầy ⇒ từ chối ngay (KHÔNG ACK), không treo tới cạn pool SQL.</summary>
    public int AppendQueueCapacity { get; set; } = 10_000;
}
