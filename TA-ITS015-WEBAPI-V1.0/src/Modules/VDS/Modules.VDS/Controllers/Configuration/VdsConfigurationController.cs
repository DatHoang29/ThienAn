using Modules.VDS.Controllers.Configuration.Dto;

namespace Modules.VDS.Controllers.Configuration;

/// <summary>
/// Author: KienLT
/// Description: API tham số VẬN HÀNH VDS — upsert theo mã, chỉ hai nhóm <c>vds_config</c> và
/// <c>vds_config_default</c>. Đọc/ghi dispatch qua MessBus (CQRS).
///
/// <para><b>Ranh giới với <c>vdsConfig</c></b>: feature này là mặt tiền hẹp cho người vận hành — không cần
/// biết ID, chỉ gửi <c>Code</c> + <c>Value</c>, và mỗi mã có luật riêng chặt hơn luật chung (tốc độ 0–300,
/// chu kỳ thống kê 1–60 phút, thư mục media phải là đường dẫn tuyệt đối, danh sách camera phải tồn tại trong
/// <c>TmsEquipment</c>, thời lượng mặc định phải có hậu tố s/m/h/d). Mã không nằm trong danh sách hỗ trợ bị
/// từ chối thẳng.</para>
/// <para>CRUD theo dòng cho MỌI nhóm nằm ở <c>vdsConfig</c>. Hai feature cùng ghi bảng <c>VdsConfig</c>
/// nhưng khác đối tượng dùng và khác mức kiểm tra, nên không gom.</para>
/// Created date: 16/09/2026
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 581)]
[ApiController]
public class VdsConfigurationController : BaseController
{
    /// <summary>
    /// Get VDS operational configuration list
    /// </summary>
    /// <returns>Danh sách tham số vận hành (nhóm vds_config); giá trị của mã bí mật được che.</returns>
    [DisplayName("Configuration list")]
    public async Task<List<VdsConfigurationOutput>> GetList()
    {
        return await MessBus.InvokeAsync<List<VdsConfigurationOutput>>(new VdsConfigurationListInput());
    }

    /// <summary>
    /// Resend VDS refresh message by code
    /// </summary>
    /// <param name="code">Mã refresh trong dải 0..6; body giữ nguyên là một số nguyên trần.</param>
    /// <returns>Kết quả gửi trên từng kênh transport; chưa có ACK nghiệp vụ từ worker.</returns>
    [HttpPost("Refresh")]
    [DisplayName("Configuration refresh")]
    public async Task<ConfigurationRefreshOutput> Refresh([FromBody, Range(0, 6)] int code)
    {
        return await MessBus.InvokeAsync<ConfigurationRefreshOutput>(new ConfigurationRefreshInput { Code = code });
    }

    /// <summary>
    /// Save one VDS operational parameter
    /// </summary>
    /// <param name="input">Mã và giá trị tham số; mỗi mã có luật kiểm tra riêng.</param>
    /// <returns>Kết quả lưu và kết quả gửi refresh.</returns>
    [ApiDescriptionSettings(Name = "Save"), HttpPost]
    [DisplayName("Configuration save")]
    public async Task<ConfigurationSaveOutput> SaveVdsConfiguration(VdsConfigValueInput input)
    {
        return await MessBus.InvokeAsync<ConfigurationSaveOutput>(input);
    }
}
