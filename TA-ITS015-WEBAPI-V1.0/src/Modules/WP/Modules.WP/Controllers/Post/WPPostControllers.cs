using Mapster;
using Microsoft.AspNetCore.Mvc;
using Modules.WP.Controllers;
using Modules.WP.Controllers.Post.Dto;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using System.ComponentModel;
using Wolverine;

namespace Modules.WP.Controllers.Post;

/// <summary>
/// Author: HieuNV
/// Description: WP Post - quản lý bài viết hệ thống WP
/// Created date: 03/07/2026
/// </summary>
[ApiDescriptionSettings(GroupName,Order = 460)]
public class WpPostController : BaseController
{

    /// <summary>
    /// Author: HieuNV
    /// Description: Get Post pagination list
    /// Created date: 14/08/2026
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get  pagination list")]
    public async Task<SqlSugarPagedList<PagePostOutput>> Page(PagePostInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<PagePostOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Get Post list
    /// Created date: 14/08/2026
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get  list")]
    public async Task<List<PostOutput>> GetList([FromQuery] PostInput input)
    {
        return await MessBus.InvokeAsync<List<PostOutput>>(input);
    }
    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy details
    /// Created date: 14/08/2026
    /// </summary>

    [DisplayName("Get  details")]
    public async Task<PostDetailOutput> GetDetails([FromQuery] PostDetailInput input)
    {
        return await MessBus.InvokeAsync<PostDetailOutput>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Add Post
    /// Created date: 14/08/2026
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add Post")]
    public async Task AddPost(AddPostInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Update Post
    /// Created date: 14/08/2026
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update Post")]
    public async Task UpdatePost(UpdatePostInput input)
    {
        await MessBus.InvokeAsync(input);
    }
    /// <summary>
    /// Author: HieuNV
    /// Description: Change Status
    /// Created date: 14/08/2026
    /// </summary>

    [ApiDescriptionSettings(Name = "WpChangeStatusPost")]
    [HttpPost]
    [DisplayName("Change Publish Status")]
    public async Task<WpChangeStatusPostOutput> ChangeStatus(WpChangeStatusPostInput input)
    {
        return await MessBus.InvokeAsync<WpChangeStatusPostOutput>(input);
    }
    /// <summary>
    /// Author: HieuNV
    /// Description: Upload Image
    /// Created date: 14/08/2026
    /// </summary>

    [ApiDescriptionSettings(Name = "WPUploadImage")]
    [HttpPost]
    [DisplayName("Upload Post Image")]
    public async Task<PostUploadImageOutput> UploadImage(
     [FromForm] UploadPostImageInput input)
    {
        return await MessBus.InvokeAsync<PostUploadImageOutput>(input);
    }
    /// <summary>
    /// Author: HieuNV
    /// Description: Delete Post
    /// Created date: 14/08/2026
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete Post")]
    public async Task DeletePost(DeletePostInput input)
    {
        await MessBus.InvokeAsync(input);

    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Batch delete Post
    /// Created date: 14/08/2026
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    [DisplayName("Batch delete Post ")]
    public async Task BatchDeletePost(List<DeletePostInput> input)
    {
        await MessBus.InvokeAsync(input);
    }
}
