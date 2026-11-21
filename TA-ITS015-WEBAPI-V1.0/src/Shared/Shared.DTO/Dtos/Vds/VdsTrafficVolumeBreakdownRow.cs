using System;
using Shared.DTO.Dtos.Base;

namespace Shared.DTO.Dtos.Vds
{
    /// <summary>
    /// Author: KienLT
    /// Description: Request đếm xe theo giờ tách direction × vehicle_size — cho report ngày/tháng.
    /// Created date: 03/09/2026
    /// </summary>
    public class VdsTrafficVolumeBreakdownRequest : BaseEntityDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public bool CountAllEventTypes { get; set; }
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Một ô đếm giờ: số lượt xe của một <c>(giờ, direction, vehicle_size)</c>.
    /// <c>BucketTime</c> là đầu giờ UTC; report đổi sang giờ server trước khi phân ca/ngày.
    /// Created date: 03/09/2026
    /// </summary>
    public class VdsTrafficVolumeBreakdownRow
    {
        public DateTime BucketTime { get; set; }
        public string? Direction { get; set; }

        /// <summary>Cỡ xe: short/medium/long, hoặc rỗng khi không suy được cỡ.</summary>
        public string? VehicleSize { get; set; }

        public int VehicleCount { get; set; }
    }
}
