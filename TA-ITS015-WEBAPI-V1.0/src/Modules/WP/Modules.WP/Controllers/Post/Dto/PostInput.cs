

using Microsoft.AspNetCore.Http;
using Modules.WP.Core.Entities;
using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Modules.WP.Controllers.Post.Dto;

/// <summary>
/// Author: HieuNV
/// Description: Các Input DTO cho tính năng bài viết hệ thống WP (Post)
/// Created date: 03/07/2026
/// </summary>
public class PagePostInput : BasePageInput
{

    public string? Title { get; set; }
    
    public string? Category { get; set; }

    public string? Author { get; set; }

    public BaseEnums.StatusEnum? IsDraft { get; set; }

    public string? Code { get; set; }
    public BaseEnums.HeroEnum? IsHero { get; set; }

}
/// <summary>
/// Author: HieuNV
/// Description: Tham số đầu vào của Post
/// Created date: 14/08/2026
/// </summary>

public class PostInput
{
    public string? Title { get; set; }
    public string? Category { get; set; }
    public string? Author { get; set; }
    public BaseEnums.StatusEnum? IsDraft { get; set; }
    public string? Code { get; set; }
    public BaseEnums.HeroEnum? IsHero { get; set; }


}
/// <summary>
/// Author: HieuNV
/// Description: Tham số đầu vào của Post Detail
/// Created date: 14/08/2026
/// </summary>

public class PostDetailInput : BaseIdInput
{
   public BaseEnums.StatusEnum? IsDraft { get; set; }
}
/// <summary>
/// Author: HieuNV
/// Description: Tham số đầu vào của Add Post
/// Created date: 14/08/2026
/// </summary>
public class AddPostInput :WpPost
{
}
/// <summary>
/// Author: HieuNV
/// Description: Tham số đầu vào của Update Post
/// Created date: 14/08/2026
/// </summary>

public class UpdatePostInput : AddPostInput
{

}
/// <summary>
/// Author: HieuNV
/// Description: Tham số đầu vào của Delete Post
/// Created date: 14/08/2026
/// </summary>

public class DeletePostInput : BaseIdInput
{

}
/// <summary>
/// Author: HieuNV
/// Description: Tham số đầu vào của Upload Post Image
/// Created date: 14/08/2026
/// </summary>

public class UploadPostImageInput
{
    public IFormFile File { get; set; }
}
/// <summary>
/// Author: HieuNV
/// Description: Tham số đầu vào của Change Status Post
/// Created date: 14/08/2026
/// </summary>

public class WpChangeStatusPostInput : BaseIdInput
{
    public BaseEnums.HeroEnum? IsHero { get; set; }
}
