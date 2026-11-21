using Microsoft.Extensions.Localization;
using Modules.TMS.Core.Entities;
using Modules.VDS.Controllers.SSEDemo.Dto;
using Modules.VDS.Core.Exceptions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Sse;

namespace Modules.VDS.Controllers.SSEDemo.Queries;

/// <summary>
/// Author: KienLT
/// Description: Lấy dữ liệu thiết bị công khai cho demo Server-Sent Events.
/// Created date: 10/09/2026
/// </summary>
public class SseDemoQueryHandler : IWolverineHandler
{
    private readonly SqlSugarRepository<TmsEquipment> _equipmentRepository;
    private readonly IStringLocalizer _localizer;

    public SseDemoQueryHandler(SqlSugarRepository<TmsEquipment> equipmentRepository, IStringLocalizer localizer)
    {
        _equipmentRepository = equipmentRepository;
        _localizer = localizer;
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Lấy ảnh chụp danh sách thiết bị đã lược bỏ thông tin kết nối thiết bị.
    /// Created date: 10/09/2026
    /// </summary>
    public async Task<SseDemoSnapshotOutput> HandleAsync(SseDemoInput query, CancellationToken cancellationToken)
    {
        var name = query.Name;

        var equipment = await _equipmentRepository.AsQueryable()
            .Where(e => e.IsDelete == null)
            .WhereIF(!string.IsNullOrWhiteSpace(name), e => e.Name.Contains(name!))
            .OrderBy(e => e.ID)
            .Select(e => new SseDemoEquipmentOutput
            {
            },true)
            .ToListAsync(cancellationToken);

        return new SseDemoSnapshotOutput
        {
            Timestamp = DateTimeOffset.UtcNow,
            Total = equipment.Count,
            Data = equipment
        };
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Trả về một frame SSE do handler tự quyết định. Stream đóng lại khi không còn thiết bị nào hoạt động.
    ///
    /// <para>Hai câu thông báo đi qua <see cref="IStringLocalizer"/>: chúng được gửi TỚI CLIENT trong frame
    /// SSE nên là chuỗi hiển thị, không phải log.</para>
    /// Created date: 10/09/2026
    /// </summary>
    public async Task<SseFrame> HandleAsync(SseDemoTickInput query, CancellationToken cancellationToken)
    {
        var total = await _equipmentRepository.AsQueryable()
            .Where(e => e.IsDelete == null)
            .CountAsync(cancellationToken);

        if (total == 0)
            return SseFrame.Ok(null).WithCode(204)
                .WithMessage(_localizer[BaseMsg.Vds.Message.SseNoEquipment].Value)
                .WithEvent("done").Last();

        return SseFrame
            .Ok(new SseDemoTickOutput { At = DateTimeOffset.UtcNow, Total = total })
            .WithMessage(_localizer[BaseMsg.Vds.Message.SseEquipmentCount, total].Value);
    }
}
