using Microsoft.AspNetCore.Hosting;
using Modules.TMS.Core.Entities;

namespace Modules.TMS.Infrastructure.Services.TmsEquipments
{
    public class TmsEquipmentService : IScoped
    {
        private readonly IWebHostEnvironment _hostingEnvironment;
        private BaseRepository<TmsEquipment> _tmsEquipment;
        private BaseRepository<TmsEquipmentType> _tmsEquipmentType;
        private BaseRepository<TmsModule> _tmsModule;
        public TmsEquipmentService(IWebHostEnvironment hostingEnvironment, BaseRepository<TmsEquipment> tmsEquipment,
            BaseRepository<TmsEquipmentType> tmsEquipmentType, BaseRepository<TmsModule> tmsModule)
        {
            _hostingEnvironment = hostingEnvironment;
            _tmsEquipment = tmsEquipment;
            _tmsEquipmentType = tmsEquipmentType;
            _tmsModule = tmsModule;
        }

        public async Task<List<TmsEquipment>> GetEquipmentTypeByEquipmentId(string code)
        {
            // If no code is provided, return null immediately
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            // Query the equipment type based on the provided code
            var equipmentType = await _tmsEquipmentType.AsQueryable()
                .Where(et => et.Code == code)
                .FirstAsync();

            // If the equipment type is null, return null
            if (equipmentType == null)
            {
                return null;
            }

            // Get the list of equipment based on the equipment type ID
            var equipmentList = await _tmsEquipment.AsQueryable()
                .Where(m => m.EquipmentTypeId == equipmentType.ID)
                .ToListAsync();

            // Return the list of equipment, or null if the list is empty
            return equipmentList.Any() ? equipmentList : null;
        }



    }
}
