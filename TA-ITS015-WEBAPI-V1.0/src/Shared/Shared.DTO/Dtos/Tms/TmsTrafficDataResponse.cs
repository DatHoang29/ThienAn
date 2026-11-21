using Shared.DTO.Dtos.Base;
using System;

namespace Shared.DTO.Dtos.Tms
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO thông tin cơ bản TmsTrafficData
    /// </summary>
    public class TmsTrafficDataResponseBase : BaseEntityDto
    {
        //TmsTrafficData {
        // DetectTime datetime null
        // Location varchar(16) null
        // Lane varchar(10)
        // Speed float(4,1) null
        // IpAddress varchar(32) null
        // Height integer null
        // Width integer null
        // Length integer null
        // AlarmHeight boolean null def(false)
        // AlarmWidth boolean null def(false)
        // ID varchar(64) unique
        // EquipmentId varchar(64) null
        // LicensePlate varchar(32) null
        // Type varchar(16) null
        // Direction varchar(16) null
        // AlarmLength boolean null def(false)
        // AlarmSpeed boolean null def(false)
        // ZoneId varchar(64) null
        //}

        public DateTime? DetectTime { get; set; }
        public string? Location { get; set; }
        public string? Lane { get; set; }
        public float? Speed { get; set; }
        public string? IpAddress { get; set; }
        public int? Height { get; set; }
        public int? Width { get; set; }
        public int? Length { get; set; }
        public bool? AlarmHeight { get; set; }
        public bool? AlarmWidth { get; set; }
        public string? EquipmentId { get; set; }
        public string? LicensePlate { get; set; }
        public string? Type { get; set; }
        public string? Direction { get; set; }
        public bool? AlarmLength { get; set; }
        public bool? AlarmSpeed { get; set; }
        public string? ZoneId { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO thông tin mở rộng TmsEquipment
    /// </summary>
    public class TmsTrafficDataResponse : TmsTrafficDataResponseBase
    {
    }
}
