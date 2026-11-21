using Microsoft.AspNetCore.Authorization;
using Modules.VDS.Controllers.Config.Dto;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.VDS.Controllers.Config;

/// <summary>
/// Author: KienLT
/// Description: API cấu hình VDS — CRUD ĐẦY ĐỦ theo dòng, cho MỌI nhóm của bảng <c>VdsConfig</c>.
///
/// <para><b>Ranh giới với <c>vdsConfiguration</c></b> (hai feature cùng ghi một bảng, KHÔNG trùng chức năng):</para>
/// <list type="bullet">
/// <item><b>Feature này (<c>Config</c>)</b> — dành cho quản trị: thêm/sửa/xóa theo <c>ID</c>, phạm vi là mọi
/// nhóm (danh mục camera, mapping loại sự kiện, mapping cỡ xe, thời lượng skip/dedup/display, tham số vận
/// hành). Luật nghiệp vụ dùng chung <c>VdsConfigRules</c>.</item>
/// <item><b><c>Configuration</c></b> — dành cho vận hành: chỉ hai nhóm <c>vds_config</c> và
/// <c>vds_config_default</c>, upsert THEO MÃ (không cần ID), và kiểm tra riêng từng mã chặt hơn (tốc độ
/// 0–300, chu kỳ thống kê 1–60, thư mục media phải là đường dẫn tuyệt đối, danh sách camera phải tồn tại).
/// Kèm endpoint <c>Refresh</c> để phát lại bản tin.</item>
/// </list>
/// <para>Không gom hai feature: gom lại thì hoặc mất lớp kiểm tra chặt của đường vận hành, hoặc mất CRUD
/// theo dòng của đường quản trị.</para>
///
/// <para><b>Hợp đồng HTTP của Add/Update là kiểu trần</b> (<c>string</c> ID và <c>bool</c>), không phải DTO:
/// controller bóc <c>VdsConfigAddResult</c>/<c>VdsConfigWriteResult</c> — hai kiểu đó là KẾT QUẢ HANDLER nội
/// bộ. Cố ý giữ nguyên để không phải gen lại client; đừng đọc tên kiểu rồi suy ra hình dạng JSON.</para>
/// Created date: 16/09/2026
/// </summary>
[Authorize]
[ApiDescriptionSettings(GroupName, Order = 580)]
[ApiController]
public class VdsConfigController : BaseController
{
    /// <summary>
    /// Author: KienLT
    /// Description: Các loại AID chưa ánh xạ trong 1..60 ngày gần nhất.
    /// Created date: 19/10/2026
    /// </summary>
    [HttpGet("UnmappedEventTypes")]
    public Task<List<UnmappedEventTypeOutput>> UnmappedEventTypes([FromQuery] UnmappedEventTypesInput input)
        => MessBus.InvokeAsync<List<UnmappedEventTypeOutput>>(input);

    /// <summary>
    /// Get VDS config group list
    /// </summary>
    /// <returns>Danh sách nhóm cấu hình cho dropdown.</returns>
    [HttpGet("Groups")]
    [DisplayName("Config group list")]
    public async Task<List<VdsConfigGroupOutput>> Groups()
    {
        return await MessBus.InvokeAsync<List<VdsConfigGroupOutput>>(new VdsConfigGroupInput());
    }

    /// <summary>
    /// Get VDS config pagination list
    /// </summary>
    /// <param name="input">Nhóm và từ khóa tìm kiếm, kèm tham số phân trang.</param>
    /// <returns>Trang danh sách cấu hình; giá trị của mã bí mật được che.</returns>
    [DisplayName("Config paged list")]
    public async Task<SqlSugarPagedList<VdsConfigOutput>> Page(PageVdsConfigInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<VdsConfigOutput>>(input);
    }

    /// <summary>
    /// Add VDS config
    /// </summary>
    /// <param name="input">Dữ liệu cấu hình; ID để trống.</param>
    /// <returns>ID của bản ghi vừa tạo.</returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Config add")]
    public async Task<string> AddVdsConfig(AddVdsConfigInput input)
    {
        return (await MessBus.InvokeAsync<VdsConfigAddResult>(input)).Id;
    }

    /// <summary>
    /// Update VDS config
    /// </summary>
    /// <param name="input">Dữ liệu cấu hình; ID bắt buộc.</param>
    /// <returns>Có ghi được dòng nào hay không.</returns>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Config update")]
    public async Task<bool> UpdateVdsConfig(UpdateVdsConfigInput input)
    {
        return (await MessBus.InvokeAsync<VdsConfigWriteResult>(input)).Success;
    }

    /// <summary>
    /// Delete VDS config
    /// </summary>
    /// <param name="input">ID bản ghi cần xóa mềm; bản ghi hệ thống không cho xóa.</param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Config delete")]
    public async Task DeleteVdsConfig(DeleteVdsConfigInput input)
    {
        await MessBus.InvokeAsync(input);
    }
}
