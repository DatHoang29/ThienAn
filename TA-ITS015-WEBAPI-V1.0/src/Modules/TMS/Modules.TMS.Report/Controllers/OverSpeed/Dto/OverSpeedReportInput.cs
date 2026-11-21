using Modules.Report.Base.Core.Dto;

namespace Modules.TMS.Report.Controllers.OverSpeed.Dto
{
    /// <summary>
    /// Author: KienLT
    /// Description: Tham số render báo cáo phương tiện vượt quá tốc độ. <c>FromDate</c>/<c>ToDate</c> FE gửi UTC.
    /// <c>ZoneIdList</c>/<c>EquipmentIdList</c> rỗng = tất cả; có cả hai thì lấy giao.
    /// Created date: 16/10/2026
    /// </summary>
    public class OverSpeedRenderInput : BaseReportInput
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string[]? ZoneIdList { get; set; }
        public string[]? EquipmentIdList { get; set; }
        public string? Direction { get; set; }

        /// <summary>Cỡ xe: short / medium / long.</summary>
        public string? VehicleSize { get; set; }

        public string? LicensePlate { get; set; }

        /// <summary>Chỉ lấy lượt vượt ngưỡng hiện hành từ X km/h trở lên; lượt chưa có ngưỡng bị loại khi đặt.</summary>
        public double? MinExceedKmh { get; set; }
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Tham số export báo cáo phương tiện vượt quá tốc độ.
    /// Created date: 16/10/2026
    /// </summary>
    public class OverSpeedExportInput : OverSpeedRenderInput
    {
        public string? Format { get; set; }
    }
}
