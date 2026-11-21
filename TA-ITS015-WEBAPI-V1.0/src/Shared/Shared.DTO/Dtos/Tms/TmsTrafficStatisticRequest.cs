using Shared.DTO.Dtos.Base;
using System;

namespace Shared.DTO.Dtos.Tms
{
    public class TmsTrafficStatisticRequest : BaseEntityDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }
}