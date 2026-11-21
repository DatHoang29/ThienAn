using Modules.WP.Core.Entities;
using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Modules.WP.Controllers.Menu.Dto;

/// <summary>
/// Author: HieuNV
/// Description: Tham số lọc danh sách menu (dạng cây). Pid = gốc của cây (mặc định "0")
/// Created date: 25/06/2026
/// </summary>
public class WpMenuInput
{
    public string? Name { get; set; }

    public string? Title { get; set; }

    public string? Type { get; set; }

    public string? Pid { get; set; }
    public BaseEnums.StatusEnum? Status { get; set; }

}

/// <summary>
/// Author: HieuNV
/// Description: Tham số lấy cây menu để chọn menu cha (tree-select)
/// Created date: 25/06/2026
/// </summary>
public class WpMenuTreeSelectInput
{
    /// <summary>
    /// Author: HieuNV
    /// Created date: 25/06/2026
    /// Lọc theo loại menu (tùy chọn)
    /// </summary>
    public string? Type { get; set; }
}

/// <summary>
/// Author: HieuNV
/// Description: Thêm mới menu. Pid = "0" là menu gốc (cha), Pid = ID menu cha là menu con
/// Created date: 25/06/2026
/// </summary>
public class WpAddMenuInput : WpMenu
{
}

/// <summary>
/// Author: HieuNV
/// Description: Cập nhật menu
/// Created date: 25/06/2026
/// </summary>
public class WpUpdateMenuInput : WpAddMenuInput
{
}

/// <summary>
/// Author: HieuNV
/// Description: Xóa menu theo ID
/// Created date: 25/06/2026
/// </summary>
public class WpDeleteMenuInput : BaseIdInput
{
}
