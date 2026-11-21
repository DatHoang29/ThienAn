using Module.TOLL.Core.Dto.LichSuThaoTac;
using Module.TOLL.Infrastructure.Service.Haukiem;
using Modules.TOLL.Core.Entities;
using Modules.TOLL.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Utility.Utilities;

namespace Modules.TOLL.Controllers.LichSuThaoTac.Queries
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Truy vấn lịch sử truy cập ứng dụng của nhân viên thu phí (bảng TollAccessHistory)
    /// Created date: 24/09/2026
    /// </summary>
    public class LichSuThaoTacQueryHandler : IWolverineHandler
    {
        private BaseRepository<TollAccessHistory> _accessRep;
        private TollService _tollService;

        public LichSuThaoTacQueryHandler(
            BaseRepository<TollAccessHistory> accessRep,
            TollService tollService)
        {
            _accessRep = accessRep;
            _tollService = tollService;
        }

        /// <summary>
        /// Danh sách lịch sử truy cập theo điều kiện tìm kiếm (phân trang).
        /// Author: ChuongLP
        /// Date: 24/09/2026
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<SqlSugarPagedList<LichSuThaoTacOutput>> HandleAsync(PageLichSuThaoTacInput input)
        {
            // FE gửi UTC+0 -> chuyển về giờ server trước khi lọc theo AccessTime
            input.StartTime = BaseUtil.TimeServer.GetTimeServer(input.StartTime);
            input.EndTime = BaseUtil.TimeServer.GetTimeServer(input.EndTime);

            if (input.StartTime.HasValue && input.EndTime.HasValue)
            {
                _tollService.CheckDateForSearch(input.StartTime, input.EndTime);
            }

            if (string.Equals(input.Order, "desc", StringComparison.OrdinalIgnoreCase))
            {
                input.DescStr = "desc";
            }

            // OrderBuilder dùng input.Field làm tên cột SQL trực tiếp.
            // FE truyền tên trường Output (camelCase) → phải ánh xạ về tên cột entity thực tế trong DB.
            if (!string.IsNullOrWhiteSpace(input.Field))
            {
                input.Field = input.Field.ToLower() switch
                {
                    "logdatetime"  => nameof(TollAccessHistory.AccessTime),
                    "displaytitle" => nameof(TollAccessHistory.EmployeeName),
                    "actionname"   => nameof(TollAccessHistory.Status),
                    "account"      => nameof(TollAccessHistory.EmployeeCode),
                    "lastsynctime" => nameof(TollAccessHistory.LastSyncTime),
                    _              => input.Field,
                };
            }

            return await _accessRep.AsQueryable()
                .WhereIF(input.StartTime.HasValue, a => a.AccessTime >= input.StartTime)
                .WhereIF(input.EndTime.HasValue, a => a.AccessTime <= input.EndTime)
                .WhereIF(!string.IsNullOrWhiteSpace(input.ActionName), a => a.Status == input.ActionName)
                .WhereIF(!string.IsNullOrWhiteSpace(input.Account), a => a.EmployeeCode.Contains(input.Account))
                .Select(a => new LichSuThaoTacOutput
                {
                    ID = a.ID,
                    LogDateTime = a.AccessTime,
                    DisplayTitle = a.EmployeeName,
                    ActionName = a.Status,
                    Account = a.EmployeeCode,
                    LastSyncTime = a.LastSyncTime
                }, true)
                .OrderBuilder(input, "", BaseConst.SortFieldConst.UpdateOrCreateTime)
                .ToPagedListAsync(input.Page, input.PageSize);
        }
    }
}
