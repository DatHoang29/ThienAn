using System.ComponentModel;
using Modules.FMS.Controllers.EquipmentTelemetry.Dto;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.FMS.Controllers.EquipmentTelemetry
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: API telemetry thiết bị FMS:
    ///              - Tra cứu (phân trang) lịch sử trạng thái đã thu thập.
    ///              - Xóa bản ghi lịch sử.
    /// Created date: 2026-08-27
    /// </summary>
    [ApiController]
    [ApiDescriptionSettings(GroupName)]
    public class FmsEquipmentTelemetryController : BaseController
    {
        /// <summary>
        /// Phân trang lịch sử telemetry thiết bị đã thu thập.
        /// </summary>
        /// <param name="input">Đối tượng PageEquipmentTelemetryInput</param>
        [HttpPost("Page")]
        [DisplayName("List FMS device telemetry history based on pagination conditions")]
        public async Task<SqlSugarPagedList<PageEquipmentTelemetryOutput>> Page(PageEquipmentTelemetryInput input)
        {
            return await MessBus.InvokeAsync<SqlSugarPagedList<PageEquipmentTelemetryOutput>>(input);
        }

        /// <summary>
        /// Phân trang DANH SÁCH THIẾT BỊ kèm telemetry MỚI NHẤT của mỗi thiết bị
        /// (màn giám sát chính: mỗi dòng 1 thiết bị + thông số mới nhất dịch vụ lấy được).
        /// </summary>
        /// <param name="input">Đối tượng PageEquipmentLatestInput</param>
        [HttpPost("PageEquipment")]
        [DisplayName("List FMS equipment with latest telemetry based on pagination conditions")]
        public async Task<SqlSugarPagedList<EquipmentLatestTelemetryOutput>> PageEquipment(PageEquipmentLatestInput input)
        {
            return await MessBus.InvokeAsync<SqlSugarPagedList<EquipmentLatestTelemetryOutput>>(input);
        }

        /// <summary>
        /// Xóa một bản ghi lịch sử telemetry thiết bị.
        /// </summary>
        /// <param name="input">Đối tượng DeleteEquipmentTelemetryInput</param>
        [HttpPost("Delete")]
        [DisplayName("Delete a FMS device telemetry history record")]
        public async Task Delete(DeleteEquipmentTelemetryInput input)
        {
            await MessBus.InvokeAsync(input);
        }
    }
}
