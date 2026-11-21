using Modules.TMS.Controllers.Map.Dto;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TMS.Controllers.Map;

/// <summary>
///  Tms Map 
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 460)]
public class TmsMapController : BaseController
{
    //private TmsMapService _TmsMapService;

    //public TmsMapController(TmsMapService TmsMapService)
    //{
    //    _TmsMapService = TmsMapService;
    //}

    /// <summary>
    /// Get Tms Map pagination list
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get  pagination list")]
    public async Task<SqlSugarPagedList<PageMapOutput>> Page(PageMapInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<PageMapOutput>>(input);
    }

    /// <summary>
    /// Get Tms Map list 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get list")]
    public async Task<List<MapOutput>> GetList([FromQuery] MapInput input)
    {
        return await MessBus.InvokeAsync<List<MapOutput>>(input);
    }

    /// <summary>
    /// Add Tms Map
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add ")]
    public async Task AddMap(AddMapInput input)
    {
        //if (input.Type == 1)
        //{
        //    string MapImage = await _TmsMapService.SaveImageAsync(input.Url);
        //    input.Url = MapImage;
        //}
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Update Tms Map 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update")]
    public async Task UpdateMap(UpdateMapInput input)
    {
        //if (input.Type == 1)
        //{
        //    string MapImage = await _TmsMapService.SaveImageAsync(input.Url);
        //    input.Url = MapImage;
        //}
        await MessBus.InvokeAsync(input);

    }

    /// <summary>
    /// Delete Tms Map 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete")]
    public async Task DeleteMap(DeleteMapInput input)
    {
        await MessBus.InvokeAsync(input);

    }

    /// <summary>
    /// Batch delete Tms Maps 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    [DisplayName("Batch delete ")]
    public async Task BatchDeleteMap(List<DeleteMapInput> input)
    {
        await MessBus.InvokeAsync(input);
    }



}
