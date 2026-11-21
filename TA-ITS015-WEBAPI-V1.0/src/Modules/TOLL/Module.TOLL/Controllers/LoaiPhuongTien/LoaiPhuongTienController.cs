using Module.TOLL.Core.Constants;
using Module.TOLL.Core.Dto.LoaiPhuongTien;
using Module.TOLL.Core.Dto.ManualFetch;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TOLL.Controllers.LoaiPhuongTien
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Controller tra cứu loại phương tiện (danh mục ETC)
    /// Created date: 08/08/2026
    /// </summary>
    [ApiDescriptionSettings(GroupName, Order = 330)]
    public class LoaiPhuongTienController : BaseController
    {
        /// <summary>
        /// Lấy danh sách loại phương tiện (phân trang)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [DisplayName("Get toll vehicle type pagination list")]
        public async Task<SqlSugarPagedList<LoaiPhuongTienOutput>> Page(PageLoaiPhuongTienInput input)
        {
            return await MessBus.InvokeAsync<SqlSugarPagedList<LoaiPhuongTienOutput>>(input);
        }

        /// <summary>
        /// Lấy toàn bộ danh sách loại phương tiện (không phân trang) - dùng cho dropdown
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [DisplayName("Get toll vehicle type list")]
        public async Task<List<LoaiPhuongTienOutput>> GetList([FromQuery] LoaiPhuongTienInput input)
        {
            return await MessBus.InvokeAsync<List<LoaiPhuongTienOutput>>(input);
        }

        /// <summary>
        /// Lấy dữ liệu loại phương tiện (danh mục dùng chung) thủ công từ hệ thống TCS/ETC (qua NATS).
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        [DisplayName("Manual fetch toll vehicle type data")]
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
