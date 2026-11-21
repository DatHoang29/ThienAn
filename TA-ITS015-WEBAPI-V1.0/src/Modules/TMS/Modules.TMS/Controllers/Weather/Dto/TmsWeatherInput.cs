using Modules.TMS.Core.Entities;
using Shared.Core.Utilities;

namespace Modules.TMS.Controllers.Weather.Dto
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO phân trang
    /// Created date: 01/11/2024
    /// </summary>
    public class PageTmsWeatherInput : BasePageInput
    {
        public DateTime? TimeDetect { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO Tìm kiếm
    /// Created date: 01/11/2024
    /// </summary>
    public class TmsWeatherInput
    {
        public DateTime? TimeDetect { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Decription: DTO thêm mới
    /// Created date: 01/11/2024
    /// </summary>
    public class AddTmsWeatherInput : TmsWeather { }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO cập nhật
    /// Created date: 01/11/2024
    /// </summary>
    public class UpdateTmsWeatherInput : AddTmsWeatherInput { }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO xóa
    /// Created date: 01/11/2024
    /// </summary>
    public class DeleteTmsWeatherInput : BaseIdInput { }
}
