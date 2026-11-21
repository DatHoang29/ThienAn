using Modules.FMS.Controllers.Topology.Dto;
using System.ComponentModel;

namespace Modules.FMS.Controllers.Topology;

/// <summary>
/// Author: ChuongLP
/// Description: API Topology - quản lý đường dẫn thư mục config và trả nội dung file JSON
///     mô tả quan hệ cha - con của thiết bị bên Nagios cho FE.
/// Created date: 08/07/2026
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 455)]
public class FmsTopologyController : BaseController
{
    /// <summary>
    /// Lấy danh sách Topology
    /// Author: ChuongLP
    /// Date: 2026-07-09
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get Topology list")]
    public async Task<List<TopologyOutput>> GetList([FromQuery] TopologyInput input)
    {
        return await MessBus.InvokeAsync<List<TopologyOutput>>(input);
    }

    /// <summary>
    /// Lấy nội dung file config JSON
    /// Author: ChuongLP
    /// Date: 2026-07-09
    /// </summary>
    /// <param name="id">ID bản ghi Topology</param>
    /// <returns></returns>
    [DisplayName("Get Data Topology config")]
    public async Task<List<TopologyConfigOutput>> GetDataConfig([FromQuery] TopologyConfigInput input)
    {
        return await MessBus.InvokeAsync<List<TopologyConfigOutput>>(input);
    }

    /// <summary>
    /// Yêu cầu lấy dữ liệu Topology thủ công.
    /// FE bấm nút -> BE phát lệnh qua kênh NATS ta.its.data.control để dịch vụ DeviceMonitor
    /// thu thập topology mới nhất và ghi ra file; FE sau đó gọi lại GetDataConfig để hiển thị.
    /// Author: ChuongLP
    /// Date: 2026-09-07
    /// </summary>
    [HttpPost]
    [DisplayName("Sync Topology data")]
    public async Task SyncTopology(SyncTopologyInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Lấy dữ liệu Topology thủ công (có phản hồi kết quả qua NATS).
    /// FE bấm nút -> BE phát lệnh (Action="sync-topology") qua kênh ta.its.control.fms -> dịch vụ DeviceMonitor thu thập lại
    /// topology, ghi file, rồi phát kết quả về cho FE. Trả về RequestId ngay để FE đối chiếu kết quả.
    /// Author: ChuongLP
    /// Date: 2026-09-14
    /// </summary>
    [HttpPost]
    [DisplayName("Manual fetch Topology data")]
    public async Task<ManualFetchTopologyOutput> ManualFetch(ManualFetchTopologyInput input)
    {
        return await MessBus.InvokeAsync<ManualFetchTopologyOutput>(input);
    }

}
