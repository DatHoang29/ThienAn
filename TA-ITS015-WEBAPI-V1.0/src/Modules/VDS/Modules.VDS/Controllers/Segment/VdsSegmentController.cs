using Modules.VDS.Controllers.Segment.Dto;
using Modules.VDS.Core.Dto;

namespace Modules.VDS.Controllers.Segment;

/// <summary>
/// Author: KienLT
/// Description: API đoạn tuyến VDS. Đọc/ghi dispatch qua MessBus (CQRS).
///
/// <para><b>Từ 22/09/2026 CRUD theo đúng mẫu TMS:</b> <c>Add</c>, <c>Update</c>, <c>Delete</c> tách riêng thay
/// cho một <c>Save</c> gộp. Route <c>/Save</c> KHÔNG còn — client phải gen lại.</para>
/// Created date: 16/09/2026
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 583)]
[ApiController]
public class VdsSegmentController : BaseController
{
    /// <summary>
    /// Get VDS segment list
    /// </summary>
    /// <returns>Danh sách đoạn tuyến A→B theo thứ tự hiển thị.</returns>
    [DisplayName("Segment list")]
    public async Task<List<SegmentOutput>> GetList()
    {
        return await MessBus.InvokeAsync<List<SegmentOutput>>(new SegmentListInput());
    }

    /// <summary>
    /// Add VDS segment
    /// </summary>
    /// <param name="input">Dữ liệu đoạn tuyến mới.</param>
    /// <returns>ID bản ghi vừa thêm và kết quả gửi refresh.</returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Segment add")]
    public async Task<SegmentSaveOutput> AddVdsSegment(AddSegmentInput input)
    {
        return await MessBus.InvokeAsync<SegmentSaveOutput>(input);
    }

    /// <summary>
    /// Update VDS segment
    /// </summary>
    /// <param name="input">Dữ liệu đoạn tuyến kèm ID bản ghi cần sửa.</param>
    /// <returns>ID bản ghi đã sửa và kết quả gửi refresh.</returns>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Segment update")]
    public async Task<SegmentSaveOutput> UpdateVdsSegment(UpdateSegmentInput input)
    {
        return await MessBus.InvokeAsync<SegmentSaveOutput>(input);
    }

    /// <summary>
    /// Delete VDS segment
    /// </summary>
    /// <param name="input">ID đoạn tuyến cần xóa.</param>
    /// <returns>Kết quả xóa mềm và kết quả gửi refresh.</returns>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Segment delete")]
    public async Task<VdsSaveOutput> DeleteVdsSegment(DeleteSegmentInput input)
    {
        return await MessBus.InvokeAsync<VdsSaveOutput>(input);
    }

    /// <summary>
    /// Preview generated VDS segments
    /// </summary>
    /// <returns>Đoạn tính từ km/tuyến/hướng + cấu hình nhóm vds_segment, so với DB (không ghi).</returns>
    [DisplayName("Segment generate preview")]
    public async Task<SegmentGenerateOutput> GeneratePreview()
    {
        return await MessBus.InvokeAsync<SegmentGenerateOutput>(new SegmentPreviewInput());
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Get VDS segment topology — sơ đồ tuyến cho trang tốc độ trung bình (chỉ đọc).
    /// Created date: 20/10/2026
    /// </summary>
    /// <returns>Tuyến (trục, khoảng vị trí), nút giao, camera và đường đi của từng đoạn để vẽ sơ đồ hành trình.</returns>
    [HttpGet("Topology")]
    [DisplayName("Segment topology")]
    public async Task<SegmentTopologyOutput> Topology()
    {
        return await MessBus.InvokeAsync<SegmentTopologyOutput>(new SegmentTopologyInput());
    }

    /// <summary>
    /// Sync generated VDS segments
    /// </summary>
    /// <returns>Kết quả thêm/sửa/tắt đoạn và kết quả gửi refresh.</returns>
    [ApiDescriptionSettings(Name = "GenerateSync"), HttpPost]
    [DisplayName("Segment generate sync")]
    public async Task<SegmentGenerateOutput> GenerateSync()
    {
        return await MessBus.InvokeAsync<SegmentGenerateOutput>(new SegmentSyncInput());
    }
}
