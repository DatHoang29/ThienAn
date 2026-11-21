using Modules.TMS.Controllers.TrafficData.Dto;
using Modules.TMS.Controllers.Zone.Dto;
using Shared.DTO.Dtos.System;

namespace Modules.TMS.Infrastructure.Services.TrafficData
{
    public class TrafficDataService : IWolverineHandler
    {
        private IMessageBus _messageBus;

        public TrafficDataService(IMessageBus messageBus)
        {
            this._messageBus = messageBus;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Lấy traffic-data của một giờ trước đó tính từ thời điểm hiện tại
        /// Created date: 12/11/2024
        /// </summary>
        public List<TmsTrafficDataVolume> BuildTrafficDataVolumeList(List<TrafficDataOutput> dataList, List<SysConfigDataResponse> vehicleTypeList)
        {
            var returnList = new List<TmsTrafficDataVolume>();

            vehicleTypeList?.ForEach(vehicleType =>
            {
                var newList = dataList?
                    .Where(td => td.Type != null && td.Type.Equals(vehicleType.Code, StringComparison.OrdinalIgnoreCase))
                    .GroupBy(td => td.Direction)
                    .Select(gr => new TmsTrafficDataVolume()
                    {
                        Direction = gr.Key,
                        VehicleType = vehicleType.Code,
                        VehicleCount = gr.Count()
                    })
                    .ToList();

                if (newList?.Count() > 0)
                    returnList.AddRange(newList);
            });

            return returnList;
        }

        // ĐÃ BỎ 04/09/2026 (KienLT): `HandleAsync(TmsTrafficAverageChartInput)` từng nằm ở đây và trả
        // `new Random().Next(...)` cho AverageSpeed/SpeedLimit — số bịa. Nó còn TRÙNG message type với
        // `TrafficDataQueryHandler.HandleAsync(TmsTrafficAverageChartInput)`: hai handler cùng nhận một
        // message là nhập nhằng cho Wolverine, không chắc chắn bản nào chạy.
        //
        // Nay chỉ còn MỘT handler ở `TrafficDataQueryHandler`, lấy số thật từ TmsZoneStatus (worker VDS ghi)
        // và ngưỡng từ SysOpConfig — xem `TrafficDataSpeedChartService`.
    }
}
