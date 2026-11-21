namespace Shared.QuestHa.Interfaces;

/// <summary>
/// Author: KienLT
/// Description: Nguồn trạng thái node cho router (mặc định đọc QuestNodeStatus, cache StatusCacheTtl). Nguồn có dự phòng
/// (JournalFallbackStatusSource) báo thêm đang dùng dự phòng và mốc coverage để router/dashboard hiển thị.
/// Created date: 09/10/2026
/// </summary>
internal interface INodeStatusSource
{
    /// <summary>Trạng thái hiện tại của mọi node, theo tên node.</summary>
    Task<IReadOnlyDictionary<string, NodeStatusOutput>> GetAsync(CancellationToken ct = default);

    /// <summary>True khi trạng thái đang lấy từ nguồn dự phòng; nguồn không có dự phòng luôn false.</summary>
    bool UsingFallback => false;

    /// <summary>Mốc SQL xác nhận node đã nhận hết journal lần cuối (null = chưa từng hoặc nguồn không theo dõi).</summary>
    DateTime? CoveredUntil(string node) => null;
}
