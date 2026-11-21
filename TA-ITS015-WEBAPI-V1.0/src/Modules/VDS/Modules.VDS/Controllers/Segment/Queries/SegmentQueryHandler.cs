using Modules.TMS.Core.Entities;
using Modules.VDS.Controllers.Segment.Dto;
using Modules.VDS.Infrastructure;
using Modules.VDS.Infrastructure.Services.Segment;
using Shared.Infrastructure.Extensions;

namespace Modules.VDS.Controllers.Segment.Queries;

/// <summary>
/// Author: KienLT
/// Description: Query handler đọc danh sách đoạn tuyến VDS, xem trước đoạn A→B tự sinh và sơ đồ tuyến (không ghi DB).
/// Created date: 16/09/2026
/// </summary>
public class SegmentQueryHandler : IWolverineHandler
{
    private readonly BaseRepository<TmsTrafficSegment> _repo;
    private readonly VdsSegmentGenerateService _generate;

    public SegmentQueryHandler(BaseRepository<TmsTrafficSegment> repo, VdsSegmentGenerateService generate)
    {
        _repo = repo;
        _generate = generate;
    }

    /// <summary>
    /// Danh sách đoạn tuyến A→B theo thứ tự hiển thị.
    ///
    /// <para>Output kế thừa entity nên <c>Select(..., true)</c> tự map đủ cột — không còn khai tay từng field
    /// và không còn đổi <c>Status</c> thành <c>Enabled</c>.</para>
    /// </summary>
    public Task<List<SegmentOutput>> HandleAsync(SegmentListInput command)
        => _repo.AsQueryable().Where(x => x.IsDelete == null)
            .OrderBy(x => x.OrderNo).OrderBy(x => x.ID)
            .Select(x => new SegmentOutput(), true)
            .ToListAsync();

    /// <summary>
    /// Author: KienLT
    /// Description: Tính đoạn tự sinh và so với DB, không ghi. Chuyển từ SegmentGenerateHandler ngày 13/10/2026.
    /// Created date: 29/09/2026
    /// </summary>
    public Task<SegmentGenerateOutput> HandleAsync(SegmentPreviewInput command) => _generate.PreviewAsync();

    /// <summary>
    /// Author: KienLT
    /// Description: Sơ đồ tuyến (tuyến, nút giao, camera, đoạn kèm đường đi) cho sơ đồ hành trình; chỉ đọc.
    /// Created date: 20/10/2026
    /// </summary>
    public Task<SegmentTopologyOutput> HandleAsync(SegmentTopologyInput command) => _generate.TopologyAsync();
}
