using Module.TOLL.Core.Constants;
using Module.TOLL.Core.Dto.ManualFetch;
using Module.TOLL.Core.Dto.Tram;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TOLL.Controllers.Tram
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Controller tra cứu thông tin trạm thu phí
    /// Created date: 08/08/2026
    /// </summary>
    [ApiDescriptionSettings(GroupName, Order = 310)]
    public class TramController : BaseController
    {
        /// <summary>
        /// Lấy danh sách trạm thu phí (phân trang)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [DisplayName("Get toll station pagination list")]
        public async Task<SqlSugarPagedList<TramOutput>> Page(PageTramInput input)
        {
            return await MessBus.InvokeAsync<SqlSugarPagedList<TramOutput>>(input);
        }

        /// <summary>
        /// Lấy toàn bộ danh sách trạm thu phí (không phân trang) - dùng cho dropdown
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [DisplayName("Get toll station list")]
        public async Task<List<TramOutput>> GetList([FromQuery] TramInput input)
        {
            return await MessBus.InvokeAsync<List<TramOutput>>(input);
        }

        /// <summary>
        /// Gán / bỏ gán đơn vị TMS (TmsWorkUnit, Type = toll) cho trạm thu phí. RefId rỗng = bỏ gán.
        /// Author: HieuNV
        /// Date: 07/10/2026
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        [DisplayName("Map toll station to TMS work unit")]
        public async Task MapWorkUnit(MapWorkUnitTramInput input)
        {
            await MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Lấy dữ liệu trạm thu phí thủ công từ hệ thống TCS/ETC (qua NATS).
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        [DisplayName("Manual fetch toll station data")]
        public async Task<ManualFetchTollOutput> ManualFetch(ManualFetchTollRequest input)
        {
            var command = new ManualFetchTollCommand
            {
                DataTypes = new List<string> { TollSyncDataType.Station },
                FromDate = input.FromDate,
                ToDate = input.ToDate,
            };
            return await MessBus.InvokeAsync<ManualFetchTollOutput>(command);
        }
    }
}
