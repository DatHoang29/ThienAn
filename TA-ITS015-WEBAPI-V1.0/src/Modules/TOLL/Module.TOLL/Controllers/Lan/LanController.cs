using Module.TOLL.Core.Constants;
using Module.TOLL.Core.Dto.Lan;
using Module.TOLL.Core.Dto.ManualFetch;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TOLL.Controllers.Lan
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Controller tra cứu thông tin làn thu phí
    /// Created date: 08/08/2026
    /// </summary>
    [ApiDescriptionSettings(GroupName, Order = 320)]
    public class LanController : BaseController
    {
        /// <summary>
        /// Lấy danh sách làn thu phí (phân trang)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [DisplayName("Get toll lane pagination list")]
        public async Task<SqlSugarPagedList<LanOutput>> Page(PageLanInput input)
        {
            return await MessBus.InvokeAsync<SqlSugarPagedList<LanOutput>>(input);
        }

        /// <summary>
        /// Lấy toàn bộ danh sách làn thu phí (không phân trang) - dùng cho dropdown
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [DisplayName("Get toll lane list")]
        public async Task<List<LanOutput>> GetList([FromQuery] LanInput input)
        {
            return await MessBus.InvokeAsync<List<LanOutput>>(input);
        }

        /// <summary>
        /// Lấy dữ liệu làn thu phí thủ công từ hệ thống TCS/ETC (qua NATS).
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        [DisplayName("Manual fetch toll lane data")]
        public async Task<ManualFetchTollOutput> ManualFetch(ManualFetchTollRequest input)
        {
            var command = new ManualFetchTollCommand
            {
                DataTypes = new List<string> { TollSyncDataType.Lane },
                FromDate = input.FromDate,
                ToDate = input.ToDate,
            };
            return await MessBus.InvokeAsync<ManualFetchTollOutput>(command);
        }
    }
}
