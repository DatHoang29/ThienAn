using Modules.Samplev2.Controllers.Category.Dto;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.Samplev2.Controllers.Category;

/// <summary>
/// Sample Category 
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 100)]
public class SampCategoryController : BaseController
{


    /// <summary>
    /// Get Category pagination list
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get pagination list")]
    public async Task<SqlSugarPagedList<PageCategoryOutput>> Page(PageCategoryInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<PageCategoryOutput>>(input);
    }

    /// <summary>
    /// Get Category list 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get list")]
    public async Task<List<CategoryOutput>> GetList([FromQuery] CategoryInput input)
    {
        return await MessBus.InvokeAsync<List<CategoryOutput>>(input);
    }

    /// <summary>
    /// Add Category
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add")]
    public async Task AddCategory(AddCategoryInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Update Category 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update")]
    public async Task UpdateCategory(UpdateCategoryInput input)
    {
        await MessBus.InvokeAsync(input);

    }

    /// <summary>
    /// Delete Category 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete")]
    public async Task DeleteCategory(DeleteCategoryInput input)
    {
        await MessBus.InvokeAsync(input);

    }

    /// <summary>
    /// Batch delete Category
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    [DisplayName("Batch delete")]
    public async Task BatchDeleteCategory(List<DeleteCategoryInput> input)
    {
        await MessBus.InvokeAsync(input);
    }


}
