using Modules.TMS.Report.Controllers.TrafficAnalysis.Dto;
using Modules.TMS.Report.Infrastructure.Services.VdsImage;
using NewLife;
using Shared.Utility.Utilities;

namespace Modules.TMS.Report.Controllers.TrafficAnalysis.Queries
{
    /// <summary>
    /// Author: KienLT
    /// Description: Đọc ảnh dữ liệu phân tích giao thông VDS trước khi trả về Base64.
    /// Created date: 08/08/2026
    /// </summary>
    public sealed class TrafficAnalysisImageQueryHandler
    {
        private const string DataImageConfigurationKey = "Vds:DataImageDirectory";
        private const string DefaultDataImageDirectory = "vds/images/data";
        private readonly VdsImageService _vdsImageService;

        public TrafficAnalysisImageQueryHandler(VdsImageService vdsImageService)
        {
            _vdsImageService = vdsImageService;
        }

        /// <summary>Đọc ảnh dữ liệu phân tích giao thông VDS theo thông tin yêu cầu.</summary>
        public async Task<TrafficAnalysisImageOutput> HandleAsync(TrafficAnalysisImageInput command)
        {
            //DateTime utcDateTime = DateTime.SpecifyKind(  command.DetectTime.Value, DateTimeKind.Utc);
            //command.DetectTime = BaseUtil.TimeServer.GetTimeServer(utcDateTime);
            //command.DetectTime = command.DetectTime.Value.AddHours(+7);
            var result = await _vdsImageService.ReadAsync(
                VdsMediaBranch.Data,
                DataImageConfigurationKey,
                DefaultDataImageDirectory,
                command.ID,
                command.DetectTime,
                command.PictureType,
                command.Quality);

            return new TrafficAnalysisImageOutput
            {
                Base64 = result.Base64,
                PictureType = result.PictureType,
                Url = result.Url
            };
        }
    }
}
