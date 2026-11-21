using Module.TOLL.Core.Constants;
using Module.TOLL.Core.Dto.LoaiVe;
using Module.TOLL.Core.Dto.ManualFetch;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TOLL.Controllers.LoaiVe
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Controller tra cứu loại vé (danh mục ETC)
    /// Created date: 13/08/2026
    /// </summary>
    [ApiDescriptionSettings(GroupName, Order = 340)]
    public class LoaiVeController : BaseController
    {
        /// <summary>
        /// Lấy danh sách loại vé (phân trang)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [DisplayName("Get toll ticket type pagination list")]
        public async Task<SqlSugarPagedList<LoaiVeOutput>> Page(PageLoaiVeInput input)
        {
            return await MessBus.InvokeAsync<SqlSugarPagedList<LoaiVeOutput>>(input);
        }

        /// <summary>
        /// Lấy toàn bộ danh sách loại vé (không phân trang) - dùng cho dropdown
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [DisplayName("Get toll ticket type list")]
        public async Task<List<LoaiVeOutput>> GetList([FromQuery] LoaiVeInput input)
        {
            return await MessBus.InvokeAsync<List<LoaiVeOutput>>(input);
        }

        /// <summary>
        /// Lấy dữ liệu loại vé (danh mục dùng chung) thủ công từ hệ thống TCS/ETC (qua NATS).
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        [DisplayName("Manual fetch toll ticket type data")]
        public async Task<ManualFetchTollOutput> ManualFetch(ManualFetchTollRequest input)
        {
            var command = new ManualFetchTollCommand
            {
                DataTypes = new List<string> { TollSyncDataType.Master },
                FromDate = input.FromDate,
                ToDate = input.ToDate,
            };
            return await MessBus.InvokeAsync<ManualFetchTollOutput>(command);
        }
    }
}
