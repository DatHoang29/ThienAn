using System;
using Shared.DTO.Dtos.Base;

namespace Shared.DTO.Dtos.Vds
{
    /// <summary>
    /// Author: KienLT
    /// Description: Request đọc các lượt xe vượt tốc độ tức thời (<c>EventType = Speeding</c> do camera AID gắn cờ)
    /// — cho báo cáo FastReport "Phương tiện vượt quá tốc độ" của TMS.Report.
    ///
    /// <para><c>FromDate</c>/<c>ToDate</c> là UTC. <c>EquipmentIds</c> rỗng = mọi camera; bên gọi tự quy bộ lọc
    /// vùng thành danh sách camera vì quan hệ camera-vùng là n-n.</para>
    /// Created date: 16/10/2026
    /// </summary>
    public class VdsOverSpeedRequest : BaseEntityDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public string[] EquipmentIds
        {
            get => _equipmentIds;
            set => _equipmentIds = value ?? [];
        }

        public string? Direction { get; set; }
        public string? VehicleSize { get; set; }
        public string? LicensePlate { get; set; }

        private string[] _equipmentIds = [];
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Một lượt xe vượt tốc độ. <c>DetectTime</c> là UTC (Kind = Utc); report đổi sang giờ server.
    /// SpeedMax là ngưỡng tối đa (km/h) camera áp lúc vi phạm; null khi bản ghi chưa lưu ngưỡng.
    /// Created date: 16/10/2026
    /// </summary>
    public class VdsOverSpeedRow
    {
        public DateTime DetectTime { get; set; }
        public long VendorEventId { get; set; }
        public string? EquipmentId { get; set; }
        public string? ZoneId { get; set; }
        public string? Direction { get; set; }
        public string? Lane { get; set; }
        public string? LicensePlate { get; set; }
        public string? VehicleType { get; set; }
        public string? VehicleColor { get; set; }
        public string? VehicleSize { get; set; }
        public double? Speed { get; set; }
        public int? KmNumber { get; set; }
        public int? MetNumber { get; set; }

        public int? SpeedMax { get; set; }
    }
}
