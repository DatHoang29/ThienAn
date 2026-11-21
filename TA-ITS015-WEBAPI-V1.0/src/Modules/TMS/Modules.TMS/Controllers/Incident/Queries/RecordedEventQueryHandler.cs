using Modules.TMS.Infrastructure.Services.Incident.Rules;
using Modules.TMS.Infrastructure.Services.Incident.Classification;
using Modules.TMS.Core.Dto.Incident;
using Modules.TMS.Core.Constants;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Modules.TMS.Infrastructure.Services.Incident;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TMS.Controllers.Incident.Queries
{
    /// <summary>
    /// Author: KienLT
    /// Description: Handler tra cứu lịch sử bản tin đã ghi nhận — chỉ nhóm <c>event</c>, không có luồng xử lý
    /// sự cố.
    ///
    /// <para><b>Trước 22/09/2026 toàn bộ truy vấn này nằm trong <c>TmsRecordedEventController</c></b>, kèm cả
    /// kiểm tham số và khai DTO. Nay tách theo đúng mẫu <c>Controllers/{Feature}/Queries</c> của TMS: controller
    /// chỉ dispatch qua message bus.</para>
    ///
    /// <para>Điều kiện nhận dạng nhóm <c>event</c> giữ NGUYÊN của bản cũ: ưu tiên cột <c>EventGroup</c> đã
    /// chốt, chỉ tra <c>TmsEventType</c> cho bản ghi legacy chưa có cột đó. Dùng
    /// <see cref="IncidentEventGroupConst"/> thay chuỗi <c>"event"</c> viết tay như bản cũ.</para>
    /// Created date: 22/09/2026
    /// </summary>
    public class RecordedEventQueryHandler : IWolverineHandler
    {
        private BaseRepository<TmsIncident> _incidentRepo;
        private IncidentGroupResolver _groupResolver;

        public RecordedEventQueryHandler(BaseRepository<TmsIncident> incidentRepo, IncidentGroupResolver groupResolver)
        {
            this._incidentRepo = incidentRepo;
            this._groupResolver = groupResolver;
        }

        /// <summary>Phân trang bản tin đã ghi nhận theo cửa sổ thời gian và bộ lọc.</summary>
        public async Task<SqlSugarPagedList<TmsIncident>> HandleAsync(PageRecordedEventInput command)
        {
            TmsRecordedEventRules.Normalize(command);

            var result = await this._incidentRepo.AsQueryable()
                .Where(x => x.IsDelete == null
                    && (x.EventGroup == IncidentEventGroupConst.Event
                        || (x.EventGroup == null && SqlFunc.Subqueryable<TmsEventType>()
                            .Where(t => t.ID == x.EventTypeId && t.IsDelete == null
                                && t.Type.Trim().ToLower() == IncidentEventGroupConst.Event).Any())))
                .Where(x => x.StartDate >= command.From && x.StartDate <= command.To)
                .WhereIF(!string.IsNullOrWhiteSpace(command.EventTypeId), x => x.EventTypeId == command.EventTypeId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.ZoneId), x => x.ZoneId == command.ZoneId)
                .OrderByDescending(x => x.StartDate).OrderByDescending(x => x.ID)
                .ToPagedListAsync(command.Page, command.PageSize);

            await this._groupResolver.PopulateGroupsAsync(result.Records);
            return result;
        }
    }
}
