using Module.TOLL.Core.Constants;
using Module.TOLL.Core.Dto.CaLamViec;
using Module.TOLL.Core.Dto.ManualFetch;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TOLL.Controllers.CaLamViec
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Controller tra cứu thông tin ca làm việc thu phí
    /// Created date: 08/08/2026
    /// </summary>
    [ApiDescriptionSettings(GroupName, Order = 350)]
    public class CaLamViecController : BaseController
    {
        /// <summary>
        /// Lấy danh sách ca làm việc (phân trang)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [DisplayName("Get toll shift pagination list")]
        public async Task<SqlSugarPagedList<CaLamViecOutput>> Page(PageCaLamViecInput input)
        {
            return await MessBus.InvokeAsync<SqlSugarPagedList<CaLamViecOutput>>(input);
        }

        /// <summary>
        /// Lấy toàn bộ danh sách ca làm việc (không phân trang) - dùng cho dropdown
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [DisplayName("Get toll shift list")]
        public async Task<List<CaLamViecOutput>> GetList([FromQuery] CaLamViecInput input)
        {
            return await MessBus.InvokeAsync<List<CaLamViecOutput>>(input);
        }

        /// <summary>
        /// Lấy dữ liệu ca làm việc thủ công từ hệ thống TCS/ETC (qua NATS).
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        [DisplayName("Manual fetch toll shift data")]
        public async Task<ManualFetchTollOutput> ManualFetch(ManualFetchTollRequest input)
        {
            var command = new ManualFetchTollCommand
            {
                DataTypes = new List<string> { TollSyncDataType.Shift },
                FromDate = input.FromDate,
                ToDate = input.ToDate,
            };
            return await MessBus.InvokeAsync<ManualFetchTollOutput>(command);
        }
    }
}
