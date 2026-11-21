using Modules.TMS.Controllers.VehicleFilterHistory.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Utility.Utilities;

namespace Modules.TMS.Controllers.VehicleFilterHistory.Queries
{
    /// <summary>
    /// Author: ThangNN
    /// Create date: 13/11/2024
    /// </summary>
    public class TmsVehicleFilterHistoryQueryHandler : IWolverineHandler
    {
        private BaseRepository<TmsVehicleFilterHistory> _vehicleFilterHistoryRepo;

        public TmsVehicleFilterHistoryQueryHandler(BaseRepository<TmsVehicleFilterHistory> vehicleFilterHistoryRepo)
        {
            this._vehicleFilterHistoryRepo = vehicleFilterHistoryRepo;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: List theo phân trang
        /// Created date: 13/11/2024
        ///
        /// <para>25/09/2026: chuẩn hoá <c>FromDate</c>/<c>ToDate</c> bằng <c>ToUtc</c> trước khi so với
        /// <c>DetectTime</c>. Cột này theo hợp đồng là UTC (worker ghi), còn giá trị vào đây phụ thuộc cách
        /// model binding dựng <c>DateTime</c>: body JSON cho <c>Kind = Utc</c>, query string lại cho
        /// <c>Kind = Local</c> đã dịch sẵn. So thẳng thì cùng một khoảng ngày cho hai kết quả khác nhau.</para>
        /// </summary>
        public async Task<SqlSugarPagedList<PageTmsVehicleFilterHistoryOutput>> HandleAsync(PageTmsVehicleFilterHistoryInput command)
        {
            var fromDate = BaseUtil.TimeServer.ToUtc(command.FromDate);
            var toDate = BaseUtil.TimeServer.ToUtc(command.ToDate);
            var tvfhList = await this._vehicleFilterHistoryRepo.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.LicensePlate), tvfh => tvfh.LicensePlate != null && tvfh.LicensePlate.Contains(command.LicensePlate))
                .WhereIF(command.Direction != null, tvfh => tvfh.Direction == command.Direction)
                .WhereIF(command.Type != null, tvfh => tvfh.Type == command.Type)
                .WhereIF(fromDate != null, tvfh => tvfh.DetectTime >= fromDate)
                .WhereIF(toDate != null, tvfh => tvfh.DetectTime <= toDate)
                .Select((st) => new PageTmsVehicleFilterHistoryOutput() { }, true)
                .OrderBuilder(command)
                .ToPagedListAsync(command.Page, command.PageSize);

            return tvfhList;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: List theo tìm kiếm
        /// Created date: 13/11/2024
        /// </summary>
        public async Task<List<TmsVehicleFilterHistoryOutput>> HandleAsync(TmsVehicleFilterHistoryInput command)
        {
            var fromDate = BaseUtil.TimeServer.ToUtc(command.FromDate);
            var toDate = BaseUtil.TimeServer.ToUtc(command.ToDate);
            var tvfhList = await this._vehicleFilterHistoryRepo.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.LicensePlate), tvfh => tvfh.LicensePlate != null && tvfh.LicensePlate.Contains(command.LicensePlate))
                .WhereIF(command.Direction != null, tvfh => tvfh.Direction == command.Direction)
                .WhereIF(command.Type != null, tvfh => tvfh.Type == command.Type)
                .WhereIF(fromDate != null, tvfh => tvfh.DetectTime >= fromDate)
                .WhereIF(toDate != null, tvfh => tvfh.DetectTime <= toDate)
                .OrderBy(tvfh => tvfh.CreateTime)
                .Select(tvfh => new TmsVehicleFilterHistoryOutput() { }, true)
                .ToListAsync();

            return tvfhList;
        }
    }
}
