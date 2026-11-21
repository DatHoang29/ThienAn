namespace Modules.FMS.Controllers.Topology.Dto;

/// <summary>
/// Input yêu cầu lấy dữ liệu Topology thủ công (có phản hồi kết quả qua NATS).
/// FE bấm nút -> BE phát {Type="request", Action="sync-topology", RequestId} lên kênh ta.its.control.fms
/// -> dịch vụ DeviceMonitor thu thập lại topology, ghi file, rồi phát {Type="result", RequestId, Success, Message}.
/// Author: ChuongLP
/// Created date: 14/09/2026
/// </summary>
public class ManualFetchTopologyInput
{
}

/// <summary>Kết quả trả về ngay cho FE sau khi BE đã phát lệnh (fire-and-forget). FE đối chiếu RequestId với gói Type="result" nhận qua NATS.</summary>
public class ManualFetchTopologyOutput
{
    /// <summary>Mã truy vết yêu cầu — FE lắng nghe kết quả trên NATS theo mã này.</summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>Thông báo ngắn xác nhận đã tiếp nhận yêu cầu.</summary>
    public string Message { get; set; } = string.Empty;
}
