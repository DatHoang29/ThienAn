using Module.TOLL.Core.Constants;
using Module.TOLL.Core.Dto.BangGia;
using Module.TOLL.Core.Dto.ManualFetch;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TOLL.Controllers.BangGia
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Controller tra cứu bảng giá thu phí (TollPriceList)
    /// Created date: 07/10/2026
    /// </summary>
    [ApiDescriptionSettings(GroupName, Order = 340)]
    public class BangGiaController : BaseController
    {
        /// <summary>
        /// Lấy danh sách bảng giá thu phí (phân trang)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [DisplayName("Get toll price list pagination list")]
        public async Task<SqlSugarPagedList<BangGiaOutput>> Page(PageBangGiaInput input)
        {
            return await MessBus.InvokeAsync<SqlSugarPagedList<BangGiaOutput>>(input);
        }

        /// <summary>
        /// Lấy toàn bộ danh sách bảng giá thu phí (không phân trang) - dùng cho dropdown
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [DisplayName("Get toll price list")]
        public async Task<List<BangGiaOutput>> GetList([FromQuery] BangGiaInput input)
        {
            return await MessBus.InvokeAsync<List<BangGiaOutput>>(input);
        }

        /// <summary>
        /// Lấy dữ liệu bảng giá (danh mục dùng chung) thủ công từ hệ thống TCS/ETC (qua NATS).
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        [DisplayName("Manual fetch toll price list data")]
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
