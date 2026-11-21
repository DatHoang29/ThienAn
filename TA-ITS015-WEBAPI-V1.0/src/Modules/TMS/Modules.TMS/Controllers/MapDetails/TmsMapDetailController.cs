using Microsoft.AspNetCore.Hosting;
using Modules.TMS.Controllers.MapDetails.Dto;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TMS.Controllers.MapDetails;

/// <summary>
/// System TmsMapDetail 
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 460)]
public class TmsMapDetailController : BaseController
{
    private readonly IWebHostEnvironment _env;

    public TmsMapDetailController(IWebHostEnvironment env)
    {
        _env = env;
    }

    /// <summary>
    /// Get TmsMapDetail pagination list
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get  pagination list")]
    public async Task<SqlSugarPagedList<PageTmsMapDetailsOutput>> Page(PageTmsMapDetailsInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<PageTmsMapDetailsOutput>>(input);
    }

    /// <summary>
    /// Get TmsMapDetail list 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get  list")]
    public async Task<List<MapDetailsOutput>> GetList([FromQuery] MapDetailsInput input)
    {
        return await MessBus.InvokeAsync<List<MapDetailsOutput>>(input);
    }

    /// <summary>
    /// Add TmsModuleition
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add ")]
    public async Task AddTmsMapDetail(AddTmsMapDetailInput input)
    {


        // Gửi yêu cầu qua Message Bus
        await MessBus.InvokeAsync(input);
    }


    /// <summary>
    /// Update TmsMapDetail 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update ")]
    public async Task UpdateTmsMapDetail(UpdateTmsMapDetailInput input)
    {


        // Gửi yêu cầu qua Message Bus
        await MessBus.InvokeAsync(input);
    }

    [HttpGet("DataList/{id}")]
    [DisplayName("Retrieve a list of SysConfigData based on field code")]
    public async Task<List<MapDetailsOutput>> GetListByCode(string id)
    {
        var list = await MessBus.InvokeAsync<List<MapDetailsOutput>>(new MapDetailsInput { MapLocationId = id });

        foreach (var item in list)
        {
            if (!string.IsNullOrEmpty(item.GeoJsonContent))
            {
                var filePath = Path.Combine(_env.WebRootPath, item.GeoJsonContent);


                // Read the content of the file and assign it to a new property or replace the GeoJson field with the content
                item.GeoJsonContent = await System.IO.File.ReadAllTextAsync(filePath);

            }
        }

        return list;
    }
    /// <summary>
    /// Delete TmsMapDetail 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete ")]
    public async Task DeleteTmsMapDetail(DeleteTmsMapDetailInput input)
    {
        await MessBus.InvokeAsync(input);

    }

    /// <summary>
    /// Batch delete TmsModuleitions 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    [DisplayName("Batch delete ")]
    public async Task BatchDeleteTmsMapDetail(List<DeleteTmsMapDetailInput> input)
    {
        await MessBus.InvokeAsync(input);
    }



}
