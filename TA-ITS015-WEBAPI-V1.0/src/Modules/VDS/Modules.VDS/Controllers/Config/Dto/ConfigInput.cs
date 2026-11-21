using Modules.VDS.Core.Entities;
using Shared.Core.Utilities;

namespace Modules.VDS.Controllers.Config.Dto;

/// <summary>
/// Author: KienLT
/// Description: Truy vấn danh sách nhóm cấu hình VDS (dropdown).
/// Created date: 16/09/2026
/// </summary>
public class VdsConfigGroupInput { }

/// <summary>
/// Author: KienLT
/// Description: Truy vấn phân trang cấu hình VDS theo nhóm/keyword.
///
/// <para>Kế thừa <see cref="BasePageInput"/> theo quy ước thay vì tự khai <c>Page</c>/<c>PageSize</c>.
/// <c>PageSize</c> được đặt lại 50 trong constructor vì mặc định của base là 20 — giữ nguyên hành vi cũ cho
/// client không truyền tham số này.</para>
/// Created date: 16/09/2026
/// </summary>
public class PageVdsConfigInput : BasePageInput
{
    public PageVdsConfigInput()
    {
        PageSize = 50;
    }

    public string? GroupCode { get; set; }
    public string? Keyword { get; set; }
}

/// <summary>
/// Author: KienLT
/// Description: Dữ liệu thêm cấu hình VDS.
///
/// <para><b>Kế thừa thẳng entity theo đúng mẫu <c>Add{Feature}Input : {Entity}</c> của repo</b> — giống
/// <c>AddSysOpConfigInput : SysOpConfig</c> của CfgSystem, feature cùng hình dạng bảng với feature này.
/// Thân lớp rỗng: không thêm, không bớt, không đổi tên field nào.</para>
///
/// <para><b>Kế thừa entity KHÔNG chặn thêm mới.</b> Rà attribute bằng reflection ngày 23/09/2026:
/// <c>EntityBaseId.ID</c> chỉ mang <c>[SugarColumn]</c>, không có <c>[Required]</c> — DataAnnotations của MVC
/// không có gì để chặn. Vụ "không thêm mới được" hồi 22/09/2026 là do <c>BaseIdInput.ID</c> (lớp DTO, KHÔNG
/// phải entity) có <c>[Required]</c>; hai chuyện khác nhau. <c>BaseIdInput</c> vẫn chỉ dùng cho input mà ID
/// BẮT BUỘC, tức là <see cref="DeleteVdsConfigInput"/>.</para>
///
/// <para><b>Vì sao bỏ bản khai tường minh từng field:</b> bản cũ là danh sách trắng tự dựng, lệch mẫu và
/// lệch cả wire format (<c>Status</c> non-nullable với default <c>Enable</c> trong khi cột là nullable). Ràng
/// buộc thật của TenantId/audit do tầng persistence lo, không phải do DTO cắt field.</para>
/// Created date: 16/09/2026
/// </summary>
public class AddVdsConfigInput : VdsConfig { }

/// <summary>
/// Author: KienLT
/// Description: Dữ liệu cập nhật cấu hình VDS.
///
/// <para>Kế thừa <see cref="AddVdsConfigInput"/> theo mẫu <c>Update{Feature}Input : Add{Feature}Input</c>:
/// tập field y hệt, kiểu riêng chỉ để message bus định tuyến đúng nhánh sửa. ID BẮT BUỘC ở nhánh này, và
/// luật đó nằm ở <c>UpdateConfigValidator</c> (FluentValidation) để thông báo đi qua localizer.</para>
/// Created date: 16/09/2026
/// </summary>
public class UpdateVdsConfigInput : AddVdsConfigInput { }

/// <summary>
/// Author: KienLT
/// Description: Xóa cấu hình VDS theo ID.
/// Created date: 16/09/2026
/// </summary>
public class DeleteVdsConfigInput : BaseIdInput { }

/// <summary>
/// Author: KienLT
/// Description: Khoảng tra cứu loại AID chưa ánh xạ, từ 1 đến 90 ngày.
/// Created date: 19/10/2026
/// </summary>
public sealed class UnmappedEventTypesInput
{
    public int Days { get; set; } = 30;
}
