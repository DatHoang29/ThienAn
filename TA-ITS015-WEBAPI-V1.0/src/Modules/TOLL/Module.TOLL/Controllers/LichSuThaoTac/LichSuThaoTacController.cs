using Module.TOLL.Core.Constants;
using Module.TOLL.Core.Dto.LichSuThaoTac;
using Module.TOLL.Core.Dto.ManualFetch;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TOLL.Controllers.LichSuThaoTac
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Controller tra cứu lịch sử truy cập ứng dụng của nhân viên thu phí
    /// Created date: 24/09/2026
    /// </summary>
    [ApiDescriptionSettings(GroupName, Order = 250)]
    public class LichSuThaoTacController : BaseController
    {
        /// <summary>
        /// Lấy danh sách lịch sử truy cập (phân trang)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [DisplayName("Get toll access history pagination list")]
        public async Task<SqlSugarPagedList<LichSuThaoTacOutput>> Page(PageLichSuThaoTacInput input)
        {
            return await MessBus.InvokeAsync<SqlSugarPagedList<LichSuThaoTacOutput>>(input);
        }

        /// <summary>
        /// Lấy dữ liệu lịch sử truy cập thủ công từ hệ thống TCS/ETC (qua NATS).
        /// Cần khoảng thời gian (FromDate/ToDate) để giới hạn phạm vi lấy dữ liệu.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        [DisplayName("Manual fetch toll access history data")]
        public async Task<ManualFetchTollOutput> ManualFetch(ManualFetchTollRequest input)
        {
            var command = new ManualFetchTollCommand
            {
                DataTypes = new List<string> { TollSyncDataType.AccessLog },
                FromDate = input.FromDate,
                ToDate = input.ToDate,
            };
            return await MessBus.InvokeAsync<ManualFetchTollOutput>(command);
        }
    }
}
