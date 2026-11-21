using Module.TOLL.Core.Dto.ManualFetch;
using Module.TOLL.Infrastructure.Service.Cache;
using Modules.DataTransporter.ITS.Infrastructure.Services;
using Shared.Utility.Utilities;

namespace Modules.TOLL.Controllers.Sync.Commands
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Xử lý lệnh lấy dữ liệu thu phí thủ công.
    ///     Chỉ phát tín hiệu điều khiển (fire-and-forget) qua kênh NATS ta.its.control.toll với field Type="request";
    ///     dịch vụ ITS.Toll.Worker nhận lệnh, gọi lại API TCS theo từng loại dữ liệu + khoảng thời gian rồi lưu DB,
    ///     sau đó phát gói tin Type="result" để FE (subscribe trực tiếp NATS) hiển thị kết quả.
    /// Created date: 14/09/2026
    /// </summary>
    public class ManualFetchTollCommandHandler : IWolverineHandler
    {
        private readonly TollCacheService _tollCacheService;

        public ManualFetchTollCommandHandler(TollCacheService tollCacheService)
        {
            _tollCacheService = tollCacheService;
        }

        public Task<ManualFetchTollOutput> HandleAsync(ManualFetchTollCommand command)
        {
            var dataTypes = (command.DataTypes ?? new List<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (dataTypes.Count == 0)
                throw Oops.Oh("Danh sách loại dữ liệu cần lấy đang trống.");

            // FE gửi thời gian dạng UTC+0 -> convert về giờ local của server trước khi phát lệnh cho TCS.
            var fromLocal = BaseUtil.TimeServer.GetTimeServer(command.FromDate);
            var toLocal = BaseUtil.TimeServer.GetTimeServer(command.ToDate);

            if (fromLocal.HasValue && toLocal.HasValue && fromLocal > toLocal)
                throw Oops.Oh("Khoảng thời gian không hợp lệ: 'FromDate' phải nhỏ hơn hoặc bằng 'ToDate'.");

            var requestId = Guid.NewGuid().ToString("N");

            // Payload phải khác null (BroadcastData bỏ qua nếu data == null). Dùng field "Type" để phân biệt 2 chiều trên cùng 1 kênh.
            var payload = new
            {
                Type = "request",
                RequestId = requestId,
                DataTypes = dataTypes,
                FromDate = fromLocal,
                ToDate = toLocal,
                RequestTime = BaseUtil.TimeServer.GetTimeServer(),
            };

            // Xoá cache danh mục ngay khi bấm nút. Dịch vụ ghi DB sau đó nên cache còn được xoá thêm
            // 1 lần khi nhận gói "result" (xem AddTollInfrastructure) — tránh cache bị nạp lại dữ liệu cũ
            // trong lúc dịch vụ đang kéo dữ liệu. ChuongLP - 28/09/2026
            _tollCacheService.ClearByDataTypes(dataTypes);

            ItsDataTransporter.BroadcastData(ItsDataTransporterChannel.ControlToll, payload);

            var output = new ManualFetchTollOutput
            {
                RequestId = requestId,
                DataTypes = dataTypes,
                Message = "Đã gửi yêu cầu lấy dữ liệu thủ công. Vui lòng chờ kết quả xử lý.",
            };

            return Task.FromResult(output);
        }
    }
}
