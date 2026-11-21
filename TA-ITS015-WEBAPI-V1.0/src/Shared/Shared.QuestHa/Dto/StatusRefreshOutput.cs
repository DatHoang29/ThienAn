using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Kết quả MỘT lượt làm mới nguồn SQL, bất biến và được publish nguyên tử: trạng thái các node, đầu journal
/// đã commit, thành công/lỗi, thời điểm <see cref="HeadReadUtc"/> (ghi ngay TRƯỚC khi đọc đầu journal) và generation tăng dần.
/// Request lấy đúng một object rồi xử lý — không bao giờ ghép dictionary của lượt này với cờ/mốc của lượt khác.
/// Created date: 12/10/2026
/// </summary>
internal sealed record StatusRefreshOutput(
    long Generation,
    IReadOnlyDictionary<string, NodeStatusOutput> Statuses,
    bool Succeeded,
    DateTime? HeadReadUtc,
    long? HeadSeq,
    DateTime RefreshUtc = default);
