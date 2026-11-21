using Microsoft.AspNetCore.Hosting;
using Modules.TMS.Controllers.MapLocations.Dto;
using Modules.TMS.Infrastructure.Services.MapLocation;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TMS.Controllers.MapLocations;

/// <summary>
/// System TmsMapLocation 
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 460)]
public class TmsMapLocationController : BaseController
{
    private readonly IWebHostEnvironment _env;
    private MapLocationService _sv;

    public TmsMapLocationController(IWebHostEnvironment env, MapLocationService sv)
    {
        _env = env;
        _sv = sv;
    }

    /// <summary>
    /// Get TmsMapLocation pagination list
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get  pagination list")]
    public async Task<SqlSugarPagedList<PageTmsMapLocationsOutput>> Page(PageTmsMapLocationsInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<PageTmsMapLocationsOutput>>(input);
    }

    /// <summary>
    /// Get TmsMapLocation list 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get  list")]
    public async Task<List<MapLocationsOutput>> GetList([FromQuery] MapLocationsInput input)
    {
        return await MessBus.InvokeAsync<List<MapLocationsOutput>>(input);
    }

    /// <summary>
    /// Add TmsModuleition
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add ")]
    public async Task AddTmsMapLocation(AddTmsMapLocationInput input)
    {
        if (input?.GeoJson != null)
        {
            // Đường dẫn đến thư mục wwwroot/MapLocation
            var mapLocationPath = Path.Combine(_env.WebRootPath, "MapLocation");

            // Đảm bảo thư mục tồn tại
            if (!Directory.Exists(mapLocationPath))
            {
                Directory.CreateDirectory(mapLocationPath);
            }

            // Tạo tên file .txt dựa trên MapId
            string fileName = input.MapId + ".txt";

            // Đường dẫn đầy đủ đến file .txt
            var filePath = Path.Combine(mapLocationPath, fileName);

            // Lưu nội dung GeoJson vào file .txt
            await System.IO.File.WriteAllTextAsync(filePath, input.GeoJson);

            // Cập nhật thuộc tính GeoJson thành đường dẫn tương đối đến file .txt
            input.GeoJson = Path.Combine("MapLocation", fileName);
        }

        // Gửi yêu cầu qua Message Bus
        await MessBus.InvokeAsync(input);
    }


    /// <summary>
    /// Update TmsMapLocation 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update ")]
    public async Task UpdateTmsMapLocation(UpdateTmsMapLocationInput input)
    {
        if (input?.GeoJson != null)
        {
            // Đường dẫn đến thư mục wwwroot/MapLocation
            var mapLocationPath = Path.Combine(_env.WebRootPath, "MapLocation");

            // Đảm bảo thư mục tồn tại
            if (!Directory.Exists(mapLocationPath))
            {
                Directory.CreateDirectory(mapLocationPath);
            }

            // Tạo tên file .txt dựa trên MapId
            string fileName = input.MapId + ".txt";

            // Đường dẫn đầy đủ đến file .txt
            var filePath = Path.Combine(mapLocationPath, fileName);

            // Lưu nội dung GeoJson vào file .txt, nếu file đã tồn tại thì ghi đè
            await System.IO.File.WriteAllTextAsync(filePath, input.GeoJson);

            // Cập nhật thuộc tính GeoJson thành đường dẫn tương đối đến file .txt
            input.GeoJson = Path.Combine("MapLocation", fileName);
        }

        // Gửi yêu cầu qua Message Bus
        await MessBus.InvokeAsync(input);
    }

    [HttpGet("DataList/{id}")]
    [DisplayName("Retrieve a list of SysConfigData based on field code")]
    public async Task<List<MapLocationsOutput>> GetListByCode(string id)
    {
        var list = await MessBus.InvokeAsync<List<MapLocationsOutput>>(new MapLocationsInput { MapId = id });

        foreach (var item in list)
        {
            item.GeoJsonContent = await _sv.GetGeoJsonContent(list);
        }

        return list;
    }
    /// <summary>
    /// Delete TmsMapLocation 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete ")]
    public async Task DeleteTmsMapLocation(DeleteTmsMapLocationInput input)
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
    public async Task BatchDeleteTmsMapLocation(List<DeleteTmsMapLocationInput> input)
    {
        await MessBus.InvokeAsync(input);
    }



}
