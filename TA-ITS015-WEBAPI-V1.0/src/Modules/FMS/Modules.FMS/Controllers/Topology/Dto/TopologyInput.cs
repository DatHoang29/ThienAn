using Shared.Core.Utilities;

namespace Modules.FMS.Controllers.Topology.Dto;

/// <summary>
/// Input lọc danh sách Topology
/// </summary>
public class TopologyInput
{
    public string Name { get; set; }
    public string Code { get; set; }
}

/// <summary>
/// Input lấy nội dung file config (JSON) theo ID bản ghi Topology
/// </summary>
public class TopologyConfigInput 
{
}

/// <summary>
/// Input yêu cầu lấy dữ liệu Topology thủ công.
/// FE bấm nút -> BE phát lệnh qua kênh NATS ta.its.data.control để dịch vụ DeviceMonitor
/// thu thập topology mới nhất và ghi ra file.
/// </summary>
public class SyncTopologyInput
{
}

