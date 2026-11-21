using Module.TOLL.Core.Constants;
using Module.TOLL.Core.Dto.ChiTietBangGia;
using Module.TOLL.Core.Dto.ManualFetch;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TOLL.Controllers.ChiTietBangGia
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Controller tra cứu chi tiết bảng giá thu phí
    /// Created date: 08/08/2026
    /// </summary>
    [ApiDescriptionSettings(GroupName, Order = 340)]
    public class ChiTietBangGiaController : BaseController
    {
        /// <summary>
        /// Lấy danh sách chi tiết bảng giá thu phí (phân trang)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [DisplayName("Get toll price list detail pagination list")]
        public async Task<SqlSugarPagedList<ChiTietBangGiaOutput>> Page(PageChiTietBangGiaInput input)
        {
            return await MessBus.InvokeAsync<SqlSugarPagedList<ChiTietBangGiaOutput>>(input);
        }

        /// <summary>
        /// Lấy toàn bộ danh sách chi tiết bảng giá thu phí (không phân trang) - dùng cho dropdown
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [DisplayName("Get toll price list detail list")]
        public async Task<List<ChiTietBangGiaOutput>> GetList([FromQuery] ChiTietBangGiaInput input)
        {
            return await MessBus.InvokeAsync<List<ChiTietBangGiaOutput>>(input);
        }

        /// <summary>
        /// Lấy dữ liệu bảng giá (danh mục dùng chung) thủ công từ hệ thống TCS/ETC (qua NATS).
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        [DisplayName("Manual fetch toll price list detail data")]
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
