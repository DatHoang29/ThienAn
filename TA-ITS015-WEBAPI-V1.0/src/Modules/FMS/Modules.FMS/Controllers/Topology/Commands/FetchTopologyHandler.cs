using Microsoft.Extensions.Localization;
using Modules.DataTransporter.ITS.Infrastructure.Services;
using Modules.FMS.Controllers.Topology.Dto;
using Modules.FMS.Core.Exceptions;
using Shared.Utility.Utilities;
using Wolverine;

namespace Modules.FMS.Controllers.Topology.Commands
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Xử lý lệnh lấy dữ liệu Topology thủ công (có phản hồi kết quả).
    ///     Phát tín hiệu {Type="request", Action="sync-topology", RequestId} qua kênh NATS ta.its.control.fms;
    ///     dịch vụ DeviceMonitor thu thập lại topology, ghi file, rồi phát {Type="result", RequestId, ...}
    ///     để FE (subscribe trực tiếp) hiển thị kết quả và nạp lại sơ đồ.
    /// Created date: 14/09/2026
    /// </summary>
    public class FetchTopologyHandler : IWolverineHandler
    {
        private const string PayloadTypeRequest = "request";
        private const string PayloadActionSyncTopology = "sync-topology";

        private readonly IStringLocalizer _localizer;

        public FetchTopologyHandler(IStringLocalizer localizer)
        {
            _localizer = localizer;
        }

        public Task<ManualFetchTopologyOutput> HandleAsync(ManualFetchTopologyInput command)
        {
            var requestId = Guid.NewGuid().ToString("N");

            // Payload phải khác null (BroadcastData bỏ qua nếu data == null). Dùng field "Type" để phân biệt 2 chiều trên cùng 1 kênh.
            var payload = new
            {
                Type = PayloadTypeRequest,
                Action = PayloadActionSyncTopology,
                RequestId = requestId,
                RequestTime = BaseUtil.TimeServer.GetTimeServer(),
            };

            ItsDataTransporter.BroadcastData(ItsDataTransporterChannel.ControlFms, payload);

            var output = new ManualFetchTopologyOutput
            {
                RequestId = requestId,
                Message = _localizer[BaseMsg.FmsTopology.Message.FetchRequested],
            };

            return Task.FromResult(output);
        }
    }
}
