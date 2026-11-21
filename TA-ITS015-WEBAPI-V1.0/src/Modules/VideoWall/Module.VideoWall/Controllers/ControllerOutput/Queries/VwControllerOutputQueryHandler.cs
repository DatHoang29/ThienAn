using Mapster;
using Module.VideoWall.Core.Constants;
using Module.VideoWall.Core.Dto.ControllerOutput;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;
using Shared.Infrastructure.Services;
using Wolverine;

namespace Module.VideoWall.Controllers.ControllerOutput.Queries
{
    /// <summary>
    /// Description: Truy vấn cổng ra (kèm bộ điều khiển và màn đang cắm).
    /// Created date: 06/11/2026
    /// </summary>
    public class VwControllerOutputQueryHandler : IWolverineHandler
    {
        private readonly BaseRepository<VwControllerOutput> _vwOutputRep;
        private readonly BaseRepository<VwController> _vwControllerRep;
        private readonly BaseRepository<VwScreen> _vwScreenRep;

        public VwControllerOutputQueryHandler(
            BaseRepository<VwControllerOutput> vwOutputRep,
            BaseRepository<VwController> vwControllerRep,
            BaseRepository<VwScreen> vwScreenRep)
        {
            _vwOutputRep = vwOutputRep;
            _vwControllerRep = vwControllerRep;
            _vwScreenRep = vwScreenRep;
        }

        public async Task<List<VwControllerOutputOutput>> HandleAsync(VwControllerOutputInput command)
        {
            // Đọc thẳng DB, KHÔNG cache: service ITS.VideoWall ghi DB (cập nhật cổng ra) mà không xoá được cache trong bộ nhớ của BE (07/11/2026)
            var all = await LoadAllAsync();

            return all
                .Where(u => string.IsNullOrWhiteSpace(command.ControllerId) || u.ControllerId == command.ControllerId)
                .Where(u => command.OnlyPlaced != true || u.BlockNo != null)
                .ToList();
        }

        public async Task<VwControllerOutputOutput> HandleAsync(VwIdControllerOutputInput command)
        {
            var all = await LoadAllAsync();
            return all.FirstOrDefault(u => u.ID == command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);
        }

        /// <summary>
        /// Cổng ra chưa xoá, sắp xếp: bộ phân phối trước, rồi theo ô trong bố cục (khối, hàng, cột), rồi theo mã bộ điều khiển / cổng.
        /// </summary>
        private async Task<List<VwControllerOutputOutput>> LoadAllAsync()
        {
            var outputs = await _vwOutputRep.AsQueryable().Where(u => u.IsDelete == null).ToListAsync();
            var controllers = (await _vwControllerRep.AsQueryable().Where(u => u.IsDelete == null).ToListAsync())
                .ToDictionary(u => u.ID);
            var outputIds = outputs.Select(u => u.ID).ToList();
            var screens = outputIds.Count == 0 ? [] : await _vwScreenRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.ControllerOutputId != null && outputIds.Contains(u.ControllerOutputId))
                .ToListAsync();
            var screenByOutput = screens.GroupBy(u => u.ControllerOutputId!).ToDictionary(g => g.Key, g => g.First());

            return outputs
                .Select(o =>
                {
                    var dto = o.Adapt<VwControllerOutputOutput>();
                    if (controllers.TryGetValue(o.ControllerId, out var c))
                    {
                        dto.ControllerCode = c.Code;
                        dto.ControllerName = c.Name;
                        dto.ControllerRole = c.Role;
                    }
                    dto.IsMaster = dto.ControllerRole == VwControllerRole.Distribution;
                    if (screenByOutput.TryGetValue(o.ID, out var s))
                    {
                        dto.ScreenId = s.ID;
                        dto.ScreenCode = s.Code;
                        dto.ScreenName = s.Name;
                        dto.SerialNumber = s.SerialNumber;
                        dto.Temperature = s.Temperature;
                        dto.RunTimeHours = s.RunTimeHours;
                        dto.ScreenState = s.ScreenState;
                        dto.LastCheckedAt = s.LastCheckedAt;
                    }
                    return dto;
                })
                .OrderByDescending(u => u.IsMaster)
                .ThenBy(u => u.BlockNo ?? int.MaxValue)
                .ThenBy(u => u.GridRow ?? int.MaxValue)
                .ThenBy(u => u.GridCol ?? int.MaxValue)
                .ThenBy(u => u.ControllerCode)
                .ThenBy(u => u.LocalRow)
                .ThenBy(u => u.LocalCol)
                .ToList();
        }
    }
}
