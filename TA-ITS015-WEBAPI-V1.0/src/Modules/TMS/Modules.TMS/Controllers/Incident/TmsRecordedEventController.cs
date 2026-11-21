using Microsoft.AspNetCore.Authorization;
using Modules.TMS.Core.Dto.Incident;
using Modules.TMS.Core.Entities;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TMS.Controllers.Incident
{
    /// <summary>
    /// Author: KienLT
    /// Description: Lịch sử bản tin đã ghi nhận, CHỈ ĐỌC — không bao giờ lộ thao tác của luồng xử lý sự cố.
    ///
    /// <para><b>Từ 22/09/2026 controller mỏng theo đúng mẫu TMS.</b> Trước đó nó tự truy vấn, tự kiểm tham số,
    /// tự khai DTO phân trang ngay trong file này và ném thông báo tiếng Việt cứng. Truy vấn nay ở
    /// <c>Controllers/Incident/Queries/RecordedEventQueryHandler</c>, luật ở
    /// <c>Infrastructure/Services/Incident/Rules/TmsRecordedEventRules</c>, DTO ở
    /// <c>Modules.TMS.Core/Dto/Incident/RecordedEventInput</c>.</para>
    ///
    /// <para><b>Kiểu trả về giữ nguyên <c>SqlSugarPagedList&lt;TmsIncident&gt;</c></b> — cố ý. Mẫu output của
    /// TMS là <c>{Feature}Output : {Entity}</c> (xem <c>PageTmsIncidentOutput</c>), nên JSON của entity và của
    /// output kế thừa nó là như nhau; bọc thêm một lớp rỗng chỉ để đủ cặp file sẽ không thêm thông tin gì mà
    /// vẫn phải gen lại client.</para>
    /// Created date: 22/09/2026
    /// </summary>
    [Authorize]
    [ApiController]
    public class TmsRecordedEventController : BaseController
    {
        /// <summary>
        /// Get recorded event paged list
        /// </summary>
        /// <param name="input">Cửa sổ thời gian và bộ lọc loại/vùng.</param>
        /// <returns>Trang lịch sử bản tin đã ghi nhận, mới nhất trước.</returns>
        [HttpPost("Page")]
        [DisplayName("Recorded event paged list")]
        public async Task<SqlSugarPagedList<TmsIncident>> Page(PageRecordedEventInput input)
        {
            return await MessBus.InvokeAsync<SqlSugarPagedList<TmsIncident>>(input);
        }
    }
}
