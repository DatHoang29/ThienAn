using Microsoft.AspNetCore.Hosting;
using Modules.TMS.Core.Entities;

namespace Modules.TMS.Infrastructure.Services.TrafficPlanWorkDetail
{
    public class TrafficPlanWorkDetailService : IScoped
    {
        private readonly IWebHostEnvironment _hostingEnvironment;
        private BaseRepository<TmsTrafficPlanWorkDetails> _TmsTrafficPlanWorkDetail;
        public TrafficPlanWorkDetailService(IWebHostEnvironment hostingEnvironment, BaseRepository<TmsTrafficPlanWorkDetails> TmsTrafficPlanWorkDetail
            )
        {
            _hostingEnvironment = hostingEnvironment;
            _TmsTrafficPlanWorkDetail = TmsTrafficPlanWorkDetail;
        }

        public async Task deleteTrafficPlanWorkDetail(TmsTrafficPlanWorkDetails trafficPlanWorkDetails)
        {
            await _TmsTrafficPlanWorkDetail.DeleteAsync(c => c.ID == trafficPlanWorkDetails.ID);

        }



    }
}
