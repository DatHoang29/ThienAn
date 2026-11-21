using Module.TOLL.Core.Dto.HaukiemTransaction;
using Module.TOLL.Infrastructure.Service.Haukiem;

namespace Modules.TOLL.Controllers.HaukiemTransaction.Queries
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Lấy ảnh hậu kiểm (base64) của một lượt xe từ hệ thống TCS
    /// Created date: 25/08/2026
    /// </summary>
    public class GetTransactionImageQueryHandler : IWolverineHandler
    {
        private readonly TollImageService _tollImageService;

        public GetTransactionImageQueryHandler(TollImageService tollImageService)
        {
            _tollImageService = tollImageService;
        }

        /// <summary>
        /// Gọi TCS lấy base64 theo các đường dẫn ảnh lưu trong DB.
        /// Author: ChuongLP
        /// Date: 25/08/2026
        /// </summary>
        public async Task<TransactionImageOutput> HandleAsync(TransactionImageInput input, CancellationToken cancellationToken = default)
        {
            return await _tollImageService.GetImagesAsync(input, cancellationToken);
        }
    }
}
