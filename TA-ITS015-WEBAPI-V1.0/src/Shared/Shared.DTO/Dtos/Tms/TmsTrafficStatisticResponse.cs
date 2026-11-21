using Shared.DTO.Dtos.Base;
using System;
using System.Globalization;

namespace Shared.DTO.Dtos.Tms
{
    public class TmsTrafficStatisticResponseBase : BaseEntityDto
    {
        public string? Direction { get; set; }
        public int? KmNumber { get; set; }
        public int? MetNumber { get; set; }
        public float? AvgSpeed { get; set; }
        public int? LocationDetail { get; set; }
        public DateTime? FromTime { get; set; }
        public DateTime? ToTime { get; set; }
        public string? Location { get; set; }
        public string? Remark { get; set; }
        public string? IpAddress { get; set; }
        public string? EquipmentId { get; set; }
        public int? TotalVehicleNumber { get; set; }
        public int? SVehicleNumber { get; set; }
        public int? MVehicleNumber { get; set; }
        public int? LVehicleNumber { get; set; }
        public int? OVehicleNumber { get; set; }
        public string? SpaceOccupy { get; set; }
        public string? TimeOccupy { get; set; }
        public string? SpaceHeadway { get; set; }
        public string? TimeHeadway { get; set; }
        public string? StopTime { get; set; }
        public string? QueueLength { get; set; }
    }

    public class TmsTrafficStatisticResponse : TmsTrafficStatisticResponseBase
    {
        public string? EquipmentId { get; set;}
        public float? AvgSpaceOccupy
        {
            get
            {
                float result = 0;
                try
                {
                    if (!string.IsNullOrEmpty(this.SpaceOccupy) && this.SpaceOccupy != "0")
                        result = float.Parse(this.SpaceOccupy, CultureInfo.InvariantCulture
                        );
                }
                catch
                {

                }
               

                return result;
            }
        }
    }
}
