using Modules.WP.Core.Entities;
using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Modules.WP.Controllers.Category.Dto;

/// <summary>
/// Author: HieuNV
/// Description: Các Input DTO cho tính năng danh mục hệ thống WP (Category)
/// Created date: 03/07/2026
/// </summary>
public class WpPageCategoryInput : BasePageInput
{

    public string? Name { get; set; }
    
    public string? Author { get; set; }

    public string? Note { get; set; }

    public string? Code { get; set; }
     
    public BaseEnums.HeroEnum? IsHero { get; set; }
}
/// <summary>
/// Author: HieuNV
/// Description: Tham số đầu vào của Category
/// Created date: 14/08/2026
/// </summary>

public class WpCategoryInput 
{
    public string? Name { get; set; }

    public string? Author { get; set; }

    public string? Note { get; set; }

    public string? Code { get; set; }
    public bool IncludeDelete { get; set; }
    public BaseEnums.HeroEnum? IsHero { get; set; }

}
/// <summary>
/// Author: HieuNV
/// Description: Tham số đầu vào của Add Category
/// Created date: 14/08/2026
/// </summary>

public class WpAddCategoryInput : WpCategory
{
}
/// <summary>
/// Author: HieuNV
/// Description: Tham số đầu vào của Update Category
/// Created date: 14/08/2026
/// </summary>

public class WpUpdateCategoryInput : WpAddCategoryInput
{

}
/// <summary>
/// Author: HieuNV
/// Description: Tham số đầu vào của Delete Category
/// Created date: 14/08/2026
/// </summary>

public class WpDeleteCategoryInput : BaseIdInput
{

}
/// <summary>
/// Author: HieuNV
/// Description: Tham số đầu vào của Change Status Category
/// Created date: 14/08/2026
/// </summary>

public class WpChangeStatusCategoryInput : BaseIdInput
{
    public BaseEnums.HeroEnum? IsHero { get; set; }
}
