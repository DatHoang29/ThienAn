using Modules.VDS.Core.Dto;

namespace Modules.VDS.Controllers.Configuration.Dto;

/// <summary>
/// Author: KienLT
/// Description: Kết quả gửi refresh; chưa có ACK nghiệp vụ từ Service.Vds.
///
/// <para>Chỉ còn là tên riêng của feature cho hợp đồng trung lập <see cref="VdsRefreshOutput"/> — giữ tên để
/// không đổi schema đang có.</para>
/// Created date: 08/09/2026
/// </summary>
public class ConfigurationRefreshOutput : VdsRefreshOutput { }

/// <summary>
/// Author: KienLT
/// Description: Kết quả lưu/xóa cấu hình và gửi refresh sau khi ghi dữ liệu.
/// Created date: 08/09/2026
/// </summary>
public class ConfigurationSaveOutput : VdsSaveOutput { }

/// <summary>
/// Author: KienLT
/// Description: Một tham số vận hành VDS trả ra cho trang cấu hình.
///
/// <para>Khai tường minh từng trường thay vì trả entity <c>VdsConfig</c>: entity kế thừa <c>EntityTenant</c>
/// nên kéo theo <c>TenantId</c>, <c>IsDelete</c>, <c>RowStatus</c>, <c>CreateUId</c>, <c>UpdateUId</c> —
/// không cần cho trang cấu hình và không nên công khai cấu trúc lưu trữ ra ngoài.</para>
/// Created date: 21/09/2026
/// </summary>
public class VdsConfigurationOutput
{
    public string ID { get; set; } = "";
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Value { get; set; }
    public int? OrderNo { get; set; }
    public string? Remark { get; set; }
    [System.Text.Json.Serialization.JsonConverter(typeof(Shared.Utility.Serialization.ServerTimeToUtcConverter))]
    public DateTime? UpdateTime { get; set; }
}
