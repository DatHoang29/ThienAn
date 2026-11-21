using Microsoft.AspNetCore.Hosting;
using Modules.TMS.Controllers.MapLocations.Dto;
namespace Modules.TMS.Infrastructure.Services.MapLocation
{
    public class MapLocationService : IScoped
    {
        private readonly IWebHostEnvironment _env;

        public MapLocationService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string> GetGeoJsonContent(List<MapLocationsOutput> listTmsMapLocationsOutput)
        {
            var pathGeoJson = listTmsMapLocationsOutput.Select(x => x.GeoJson).FirstOrDefault();
            if (pathGeoJson != null)
            {
                try
                {
                    var filePath = Path.Combine(_env.WebRootPath, pathGeoJson);
                    if (!System.IO.File.Exists(filePath))
                    {
                        return null;
                    }

                    // Read the content of the file and assign it to a new property or replace the GeoJson field with the content
                    var geoJsonContent = await System.IO.File.ReadAllTextAsync(filePath);
                    return geoJsonContent;
                }
                catch (Exception ex)
                {
                    return null;
                }
            }
            return null;
        }

    }
}
