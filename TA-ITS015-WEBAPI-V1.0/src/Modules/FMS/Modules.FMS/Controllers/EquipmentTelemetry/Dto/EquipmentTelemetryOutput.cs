using System;
using Modules.FMS.Core.Entities;

namespace Modules.FMS.Controllers.EquipmentTelemetry.Dto
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Dòng dữ liệu phân trang lịch sử telemetry thiết bị FMS.
    /// Created date: 2026-08-27
    /// </summary>
    public class PageEquipmentTelemetryOutput : FmsEquipmentTelemetry
    {
    }

    /// <summary>
    /// Author: ChuongLP
    /// Description: 1 dòng danh sách thiết bị + telemetry MỚI NHẤT (null nếu dịch vụ chưa lấy được lần nào).
    ///              CollectedTime = thời điểm dịch vụ lấy được telemetry mới nhất.
    /// Created date: 2026-09-10
    /// </summary>
    public class EquipmentLatestTelemetryOutput
    {
        public string? EquipmentId { get; set; }
        public string? EquipmentCode { get; set; }
        public string? EquipmentName { get; set; }
        public string? EquipmentTypeId { get; set; }
        public string? Vendor { get; set; }
        /// <summary> Trạng thái thiết bị hiện tại (TmsEquipment.EquipmentState) </summary>
        public int? EquipmentState { get; set; }
        public int? KmNumber { get; set; }
        public int? MetNumber { get; set; }
        /// <summary> JSON thông số tài nguyên/vận hành mới nhất </summary>
        public string? ResourceMetric { get; set; }
        /// <summary> JSON thông số môi trường mới nhất </summary>
        public string? EnvironmentMetric { get; set; }
        /// <summary> JSON thông số chẩn đoán/sức khỏe mới nhất </summary>
        public string? DiagnosticMetric { get; set; }
        /// <summary> Lần lấy mới nhất thành công hay không </summary>
        public bool? Succeeded { get; set; }
        /// <summary> Thời điểm lấy được telemetry mới nhất </summary>
        public DateTime? CollectedTime { get; set; }
        public string? Message { get; set; }

        /// <summary>
        /// Thời điểm BẮT ĐẦU mất kết nối (chỉ có giá trị khi bản ghi mới nhất lấy thất bại):
        /// = thời điểm sớm nhất trong chuỗi thất bại liên tiếp hiện tại (không có lần thành công nào sau đó).
        /// FE dùng để hiển thị "Mất kết nối từ ...".
        /// </summary>
        public DateTime? DisconnectedSince { get; set; }
    }
}
