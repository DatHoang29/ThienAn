using Module.TOLL.Core.Constants;
using Module.TOLL.Core.Dto.HaukiemTransaction;
using Module.TOLL.Core.Dto.ManualFetch;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TOLL.Controllers.HaukiemTransaction
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Controller hậu kiểm giao dịch thu phí
    /// Created date: 27/07/2026
    /// </summary>
    [ApiDescriptionSettings(GroupName, Order = 300)]
    public class HaukiemTransactionController : BaseController
    {
        /// <summary>
        /// Lấy danh sách giao dịch cần hậu kiểm (phân trang)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [DisplayName("Get post-check transaction pagination list")]
        public async Task<SqlSugarPagedList<ListTransactionOutPut>> Page(GetQueryPostTransactionInput input)
        {
            return await MessBus.InvokeAsync<SqlSugarPagedList<ListTransactionOutPut>>(input);
        }

        /// <summary>
        /// Lấy ảnh hậu kiểm (base64) của một lượt xe từ hệ thống TCS theo đường dẫn ảnh lưu trong DB
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        [DisplayName("Get post-check transaction images (base64 from TCS)")]
        public async Task<TransactionImageOutput> GetImage(TransactionImageInput input)
        {
            return await MessBus.InvokeAsync<TransactionImageOutput>(input);
        }

        /// <summary>
        /// Lấy dữ liệu giao dịch hậu kiểm (xe vào + xe ra) thủ công từ hệ thống TCS/ETC (qua NATS).
        /// Cần khoảng thời gian (FromDate/ToDate) để giới hạn phạm vi lấy giao dịch.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        [DisplayName("Manual fetch toll transaction data")]
        public async Task<ManualFetchTollOutput> ManualFetch(ManualFetchTollRequest input)
        {
            var command = new ManualFetchTollCommand
            {
                DataTypes = new List<string> { TollSyncDataType.TransactionIn, TollSyncDataType.TransactionOut },
                FromDate = input.FromDate,
                ToDate = input.ToDate,
            };
            return await MessBus.InvokeAsync<ManualFetchTollOutput>(command);
        }
    }
}
