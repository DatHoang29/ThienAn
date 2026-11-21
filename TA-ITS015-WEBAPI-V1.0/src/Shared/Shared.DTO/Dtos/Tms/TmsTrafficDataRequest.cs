using Shared.DTO.Dtos.Base.Request;
using System;

namespace Shared.DTO.Dtos.Tms
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO tham số query danh sách TmsTrafficData
    /// </summary>
    public class TmsTrafficDataListRequest : BasePageReqDto {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
