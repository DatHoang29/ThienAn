using Shared.DTO.Dtos.Base;

namespace Shared.DTO.Dtos.Tms
{
    /// <summary>
    /// Author: Ngọc Thắng
    /// Description: DTO thông tin cơ bản TmsZone
    /// </summary>
    public class TmsZoneResponseBase : BaseEntityDto
    {
        //TmsZone {
        //	ID varchar(64) pk
        //	Name string(64) null
        //	Pid varchar(64) null def(0) > TmsZone.ID
        //	FromKmNumber integer null
        //	FromMetNumber integer null
        //	ToKmNumber integer null
        //	ToMetNumber integer null
        //	Type string(16) null def(1)
        //	Status integer null
        //	LaneId integer null > TmsLane.ID
        //	Remark string(256) null
        //	Direction string(64) null
        //}

        public string Name { get; set; }
        public string Pid { get; set; }
        public int? FromKmNumber { get; set; }
        public int? FromMetNumber { get; set; }
        public int? ToKmNumber { get; set; }
        public int? ToMetNumber { get; set; }
        public string Type { get; set; }
        public int? Status { get; set; }
        public int? LaneId { get; set; }
        public string Remark { get; set; }
    }

    /// <summary>
    /// Author: Ngọc Thắng
    /// Description: DTO thông tin mở rộng TmsZone
    /// </summary>
    public class TmsZoneResponse : TmsZoneResponseBase
    {
    }
}