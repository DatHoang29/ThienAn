using Modules.TMS.Report.Controllers.EventData.Dto;
using Modules.TMS.Report.Infrastructure.Services.VdsImage;

namespace Modules.TMS.Report.Controllers.EventData.Queries
{
    /// <summary>
    /// Author: KienLT
    /// Description: Đọc ảnh sự kiện VDS trước khi trả về Base64.
    /// Created date: 08/08/2026
    /// </summary>
    public sealed class EventDataImageQueryHandler
    {
        private const string EventImageConfigurationKey = "Vds:EventImageDirectory";
        private const string DefaultEventImageDirectory = "/vds/images/event";
        private readonly VdsImageService _vdsImageService;

        public EventDataImageQueryHandler(VdsImageService vdsImageService)
        {
            _vdsImageService = vdsImageService;
        }

        /// <summary>Đọc ảnh sự kiện VDS theo thông tin yêu cầu.</summary>
        public async Task<EventDataImageOutput> HandleAsync(EventDataImageInput command)
        {
            var result = await _vdsImageService.ReadAsync(
                VdsMediaBranch.Event,
                EventImageConfigurationKey,
                DefaultEventImageDirectory,
                command.ID,
                command.DetectTime,
                command.PictureType,
                command.Quality);

            return new EventDataImageOutput
            {
                Base64 = result.Base64,
                PictureType = result.PictureType,
                Url = result.Url
            };
        }
    }
}
