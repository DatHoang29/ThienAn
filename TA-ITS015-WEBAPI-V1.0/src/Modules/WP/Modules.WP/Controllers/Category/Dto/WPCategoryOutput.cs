
using Modules.WP.Core.Entities;
using Shared.Core.Utilities;
using Shared.DTO.Enums;
using Shared.Utility.Helpers;

namespace Modules.WP.Controllers.Category.Dto;

/// <summary>
/// Author: HieuNV
/// Description: Các Output DTO cho tính năng danh mục hệ thống WP (Category)
/// Created date: 03/07/2026
/// </summary>
public class WpPageCategoryOutput : WpCategory
{
}
/// <summary>
/// Author: HieuNV
/// Description: Dữ liệu trả về của Category
/// Created date: 14/08/2026
/// </summary>

public class WpCategoryOutput : WpCategory
{
}
/// <summary>
/// Author: HieuNV
/// Description: Dữ liệu trả về của Category Detail
/// Created date: 14/08/2026
/// </summary>

public class CategoryDetailOutput : WpCategory
{
}
/// <summary>
/// Author: HieuNV
/// Description: Dữ liệu trả về của Change Status Category
/// Created date: 14/08/2026
/// </summary>

public class WpChangeStatusCategoryOutput : BaseIdInput
{
    public BaseEnums.HeroEnum? IsHero { get; set; }
}