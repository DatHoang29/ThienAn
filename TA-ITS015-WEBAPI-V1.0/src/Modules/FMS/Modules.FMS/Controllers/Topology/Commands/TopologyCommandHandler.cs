using Modules.DataTransporter.ITS.Infrastructure.Services;
using Modules.FMS.Controllers.Topology.Dto;
using Wolverine;

namespace Modules.FMS.Controllers.Topology.Commands
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Xử lý các lệnh (command) của Topology.
    ///     Phát lệnh qua kênh NATS ta.its.data.control để dịch vụ DeviceMonitor thu thập lại
    ///     topology mới nhất và ghi ra file; FE sau đó gọi lại API đọc file để hiển thị.
    /// Created date: 07/09/2026
    /// </summary>
    public class TopologyCommandHandler : IWolverineHandler
    {
        private const string PayloadTypeRequest = "request";
        private const string PayloadActionSyncTopology = "sync-topology";

        /// <summary>
        /// Yêu cầu lấy dữ liệu Topology thủ công.
        /// Chỉ phát tín hiệu điều khiển (fire-and-forget) qua NATS; việc thu thập & ghi file
        /// do dịch vụ DeviceMonitor thực hiện phía nhận.
        /// </summary>
        public Task HandleAsync(SyncTopologyInput command)
        {
            // Payload phải khác null (BroadcastData bỏ qua nếu data == null).
            var payload = new
            {
                Type = PayloadTypeRequest,
                Action = PayloadActionSyncTopology,
                RequestTime = DateTime.Now,
            };

            ItsDataTransporter.BroadcastData(ItsDataTransporterChannel.ControlFms, payload);

            return Task.CompletedTask;
        }
    }
}
