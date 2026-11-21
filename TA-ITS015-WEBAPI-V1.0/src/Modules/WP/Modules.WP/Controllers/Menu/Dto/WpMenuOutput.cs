using Modules.WP.Core.Entities;

namespace Modules.WP.Controllers.Menu.Dto;

/// <summary>
/// Author: HieuNV
/// Description: Menu item cho danh sách (dạng cây)
/// Created date: 25/06/2026
/// </summary>
public class WpMenuOutput : WpMenu
{
}

/// <summary>
/// Author: HieuNV
/// Description: Node cây menu rút gọn dùng cho tree-select chọn menu cha
/// Created date: 25/06/2026
/// </summary>
public class WpMenuTreeSelectOutput
{
    public string ID { get; set; }

    public string? Title { get; set; }

    public string? Pid { get; set; }

    public List<WpMenuTreeSelectOutput> Children { get; set; }
}
