using Modules.TMS.Core.Entities;

namespace Modules.TMS.Controllers.EquipmentTypeChart.Dto
{
    /// <summary>
    /// Author: Ngọc Thắng
    /// Created date: 09/08/2024
    /// </summary>
    public class PageValidEquipmentRateOutput : TmsEquipmentType
    {
        public int Rate { get; set; }
        /// <summary>Tổng số thiết bị thuộc loại (dùng cho tooltip khi thu gọn danh sách)</summary>
        public int Total { get; set; }
        /// <summary>Số thiết bị đang hoạt động</summary>
        public int ActiveCount { get; set; }
        public string Result { get; set; }
        public string Color { get; set; }
        public string ColorLabel { get; set; }
    }
}