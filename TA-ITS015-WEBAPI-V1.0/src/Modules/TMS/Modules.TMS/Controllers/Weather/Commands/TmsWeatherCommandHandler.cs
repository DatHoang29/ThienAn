using Modules.TMS.Controllers.Weather.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Shared.Infrastructure.Extensions;

namespace Modules.TMS.Controllers.Weather.Commands
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 01/11/2024
    /// </summary>
    public class TmsWeatherCommandHandler : IWolverineHandler
    {
        private BaseRepository<TmsWeather> _weatherRepo;

        public TmsWeatherCommandHandler(BaseRepository<TmsWeather> weatherRepo)
        {
            this._weatherRepo = weatherRepo;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Thêm một record
        /// Created date: 01/11/2024
        /// </summary>
        public async Task HandleAsync(AddTmsWeatherInput command)
        {
            await this._weatherRepo.InsertAsync(command.Adapt<TmsWeather>()); // Importance: No log
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Cập nhật một record
        /// Created date: 01/11/2024
        /// </summary>
        public async Task HandleAsync(UpdateTmsWeatherInput command)
        {
            var w = await this._weatherRepo.GetByIdAsync(command.ID);
            if (w == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            await this._weatherRepo.UpdateWithDiffLogAsync(command.Adapt(w));
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Xóa một record
        /// Created date: 01/11/2024
        /// </summary>
        public async Task HandleAsync(DeleteTmsWeatherInput command)
        {
            var w = await this._weatherRepo.GetByIdAsync(command.ID);
            if (w == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            await this._weatherRepo.SoftDeleteAsync<TmsWeather>(w);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Xóa nhiều record
        /// Created date: 01/11/2024
        /// </summary>
        public async Task HandleAsync(List<DeleteTmsWeatherInput> command)
        {
            var ids = command.Select(item => item.ID).ToList();
            var wList = await this._weatherRepo.GetListAsync(w => ids.Contains(w.ID));

            if (wList?.Count() != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            await this._weatherRepo.SoftDeleteAsync<TmsWeather>(w => ids.Contains(w.ID));
        }
    }
}
