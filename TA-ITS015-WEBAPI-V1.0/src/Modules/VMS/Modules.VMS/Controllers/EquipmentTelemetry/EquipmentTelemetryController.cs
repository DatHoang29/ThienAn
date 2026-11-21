using Modules.VMS.Controllers.EquipmentTelemetry.Dto;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.VMS.Controllers.EquipmentTelemetry
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: API telemetry thiết bị VMS/LCS:
    ///              - Đọc ảnh/đọc trạng thái thủ công từ thiết bị thực tế (để đối chiếu với DB).
    ///              - Tra cứu lịch sử trạng thái đã thu thập tự động (Hangfire).
    /// Created date: 2026-08-20
    /// </summary>
    [ApiController]
    [ApiDescriptionSettings(GroupName)]
    public class EquipmentTelemetryController : BaseController
    {
        /// <summary>
        /// Đọc ảnh hiện đang hiển thị trên thiết bị (thủ công), trả base64 để FE đối chiếu với DB.
        /// </summary>
        /// <param name="input">Đối tượng ReadDeviceImageInput</param>
        [HttpPost("ReadImage")]
        [DisplayName("Read the image currently displayed on the VMS/LCS device")]
        public async Task<ReadDeviceImageOutput> ReadImage(ReadDeviceImageInput input)
        {
            return await MessBus.InvokeAsync<ReadDeviceImageOutput>(input);
        }

        /// <summary>
        /// Đọc trạng thái bên trong thiết bị (thủ công), gom nhóm theo nội dung.
        /// </summary>
        /// <param name="input">Đối tượng GetDeviceStatInput</param>
        [HttpPost("GetStat")]
        [DisplayName("Read the internal status of the VMS/LCS device")]
        public async Task<GetDeviceStatOutput> GetStat(GetDeviceStatInput input)
        {
            return await MessBus.InvokeAsync<GetDeviceStatOutput>(input);
        }

        /// <summary>
        /// Phân trang lịch sử telemetry thiết bị đã thu thập.
        /// </summary>
        /// <param name="input">Đối tượng PageVmsEquipmentTelemetryInput</param>
        [HttpPost("Page")]
        [DisplayName("List VMS/LCS device telemetry history based on pagination conditions")]
        public async Task<SqlSugarPagedList<PageVmsEquipmentTelemetryOutput>> Page(PageVmsEquipmentTelemetryInput input)
        {
            return await MessBus.InvokeAsync<SqlSugarPagedList<PageVmsEquipmentTelemetryOutput>>(input);
        }

        /// <summary>
        /// Xóa một bản ghi lịch sử telemetry thiết bị.
        /// </summary>
        /// <param name="input">Đối tượng DeleteVmsEquipmentTelemetryInput</param>
        [HttpPost("Delete")]
        [DisplayName("Delete a VMS/LCS device telemetry history record")]
        public async Task Delete(DeleteVmsEquipmentTelemetryInput input)
        {
            await MessBus.InvokeAsync(input);
        }

        // 2 API StartSchedule / StopSchedule đã BỎ ngày 23/09/2026.
        // Việc lập lịch thu thập telemetry chuyển sang dịch vụ ITS.VMS.Worker: dịch vụ tự hỏi BE danh sách
        // thiết bị rồi tự đọc theo chu kỳ Telemetry:CollectIntervalMinutes và đẩy số liệu về BE lưu DB.
        // BE chỉ còn bị động nhận, nên không có gì để bật/tắt ở đây nữa.
    }
}
