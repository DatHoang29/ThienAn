
using Modules.WP.Core.Entities;
using Shared.Core.Utilities;
using Shared.DTO.Enums;
using Shared.Utility.Helpers;

namespace Modules.WP.Controllers.Post.Dto;

/// <summary>
/// Author: HieuNV
/// Description: Các Output DTO cho tính năng bài viết hệ thống WP (Post)
/// Created date: 03/07/2026
/// </summary>
public class PagePostOutput : WpPost
{
}
/// <summary>
/// Author: HieuNV
/// Description: Dữ liệu trả về của Post
/// Created date: 14/08/2026
/// </summary>

public class PostOutput : WpPost
{
}
/// <summary>
/// Author: HieuNV
/// Description: Dữ liệu trả về của Post Detail
/// Created date: 14/08/2026
/// </summary>

public class PostDetailOutput : WpPost
{
}
/// <summary>
/// Author: HieuNV
/// Description: Dữ liệu trả về của Post Upload Image
/// Created date: 14/08/2026
/// </summary>

public class PostUploadImageOutput
{
    public string Url { get; set; }

    public string Name { get; set; }
}
/// <summary>
/// Author: HieuNV
/// Description: Dữ liệu trả về của Change Status Post
/// Created date: 14/08/2026
/// </summary>

public class WpChangeStatusPostOutput : BaseIdInput
{
    public BaseEnums.HeroEnum? IsHero { get; set; }
}