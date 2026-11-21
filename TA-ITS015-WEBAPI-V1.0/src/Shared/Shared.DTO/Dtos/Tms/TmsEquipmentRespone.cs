using Shared.DTO.Dtos.Base;

namespace Shared.DTO.Dtos.Tms
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO thông tin cơ bản TmsEquipment
    /// </summary>
    public class TmsEquipmentResponseBase : BaseEntityDto
    {
        //TmsEquipment {
	       // ID varchar(64) pk
	       // Code string null
	       // Name string(64) null
	       // Ip string(32) null
	       // Port string(8) null
	       // Url string(128) null
	       // Username string(64) null
	       // Password string(64) null
	       // Status integer null
	       // KmNumber integer null
	       // MetNumber integer null
	       // EquipmentTypeId varchar(64) null > TmsEquipmentType.ID
	       // LaneId varchar(64) null > TmsLane.ID
	       // DirectionId string(16) null
	       // ActiveDirection string(16) null
	       // Location string(16) null def(0)
	       // Remark string(1024) null
	       // OrderNo integer null
	       // EquiptState string(16) null
	       // Sdk string(16) null
	       // Manufacturer string(128) null
	       // ActiveSize string(64) null
        //}

        public string Name { get; set; }
        public string Ip { get; set; }
        public string Port { get; set; }
        public string Url { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int? Status { get; set; }
        public int? KmNumber { get; set; }
        public int? MetNumber { get; set; }
        public string EquipmentTypeId { get; set; }
        public string LaneId { get; set; }
        public int? DirectionId { get; set; }
        public int? ActiveDirection { get; set; }
        public string? Location { get; set; }
        public string Remark { get; set; }
        public int? OrderNo { get; set; }
        public int? EquipmentState { get; set; }
		public string Config { get; set; }
		public string Vendor { get; set; }
	}

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO thông tin mở rộng TmsEquipment
    /// </summary>
    public class TmsEquipmentResponse : TmsEquipmentResponseBase
    {
        public string? ZoneId { get; set; }
        public string? ZoneName { get; set; }
    }
}
