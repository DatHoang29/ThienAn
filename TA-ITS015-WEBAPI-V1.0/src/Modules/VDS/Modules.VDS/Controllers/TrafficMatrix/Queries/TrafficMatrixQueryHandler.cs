using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.VDS.Controllers.TrafficMatrix.Dto;
using Modules.VDS.Controllers.TrafficMatrix.Validators;
using Modules.VDS.Infrastructure.Services.TrafficMatrix;
using Shared.DTO.Dtos.Vds;
using Shared.Utility.Utilities;
using Modules.VDS.Infrastructure.Services.TrafficMatrix.Sql;

namespace Modules.VDS.Controllers.TrafficMatrix.Queries
{
    /// <summary>
    /// Author: KienLT
    /// Description: Query handler ma trận thô VDS. Trả các ô <c>(zone, direction, hour)</c>; TMS gom cây
    /// zone + thang màu. FE gửi UTC+0 ⇒ convert qua <c>BaseUtil.TimeServer</c>.
    /// Created date: 03/09/2026
    /// </summary>
    public class TrafficMatrixQueryHandler : IWolverineHandler
    {
        private readonly VdsTrafficMatrixService _matrix;
        private readonly IStringLocalizer _localizer;

        public TrafficMatrixQueryHandler(VdsTrafficMatrixService matrix, IStringLocalizer localizer)
        {
            _matrix = matrix;
            _localizer = localizer;
        }

        public Task<List<VdsTrafficMatrixRow>> HandleAsync(TrafficMatrixRawInput command)
        {
            var from = BaseUtil.TimeServer.ToUtc(command.FromDate) ?? command.FromDate!.Value;
            var to = BaseUtil.TimeServer.ToUtc(command.ToDate) ?? command.ToDate!.Value;

            return _matrix.QueryHourlyAsync(new MatrixQueryBuilder.MatrixFilter
            {
                FromUtc = from,
                ToUtc = to,
                VehicleSize = command.VehicleSize,
                Direction = command.Direction,
                CountAllEventTypes = command.CountAllEventTypes,
            });
        }

        /// <summary>
        /// Author: KienLT
        /// Description: Đường gọi IN-PROCESS từ TMS (<c>TmsTrafficMatrixService</c>) qua
        /// <c>IMessageBus.InvokeAsync</c> — thay Refit <c>IVdsTrafficMatrixApi.HourlyRaw</c> (đã xoá).
        ///
        /// <para>Map request Shared.DTO sang <see cref="TrafficMatrixRawInput"/> rồi chạy ĐÚNG logic của
        /// endpoint HTTP <c>HourlyRaw</c>. Đường HTTP được FluentValidation của MVC kiểm trước khi vào
        /// handler; đường message bus KHÔNG qua MVC nên phải tự chạy cùng validator
        /// (<see cref="TrafficMatrixRawValidator"/>) để giữ nguyên luật bắt buộc From/To + trần cửa sổ.</para>
        ///
        /// <para>DateTime đi nguyên instance (không qua JSON). Hợp đồng vẫn như cũ: bên gọi truyền mốc UTC;
        /// <c>ToUtc</c> ở handler HTTP xử lý Kind Utc/Local/Unspecified giống hệt giá trị đã round-trip JSON.</para>
        /// Created date: 13/10/2026
        /// </summary>
        public Task<List<VdsTrafficMatrixRow>> HandleAsync(VdsTrafficMatrixRequest command)
        {
            var input = new TrafficMatrixRawInput
            {
                FromDate = command.FromDate,
                ToDate = command.ToDate,
                VehicleSize = command.VehicleSize,
                Direction = command.Direction,
                CountAllEventTypes = command.CountAllEventTypes,
            };

            new TrafficMatrixRawValidator(_localizer).ValidateAndThrow(input);

            return HandleAsync(input);
        }
    }
}
