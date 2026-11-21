using Modules.TOLL.Core.Entities;
using SqlSugar;

namespace Module.TOLL.Report.Core.Helpers
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Helper lấy tên trạm thu phí để hiển thị trên header báo cáo.
    /// FE gửi xuống StationId (value của dropdown), BE tra ngược ra tên trạm từ danh mục TollStation.
    /// Created date: 13/08/2026
    /// </summary>
    public static class StationHelper
    {
        /// <summary>
        /// Nhãn hiển thị khi không chọn trạm cụ thể
        /// </summary>
        public const string TatCaCacTram = "Tất cả các trạm";

        /// <summary>
        /// Lấy tên trạm theo mã trạm.
        /// </summary>
        /// <param name="db">SqlSugar client (thường là _baseRepo.Context)</param>
        /// <param name="stationId">Mã trạm; null/rỗng = xem tất cả các trạm</param>
        /// <returns>Tên trạm; fallback về chính mã trạm nếu không có trong danh mục</returns>
        public static string GetTenTram(this ISqlSugarClient db, string? stationId)
        {
            if (string.IsNullOrWhiteSpace(stationId))
                return TatCaCacTram;

            var cleanId = stationId.Replace("-", "");
            var name = db.Queryable<TollStation>()
                .Where(x => x.StationId == stationId || x.StationBE == stationId || (x.StationBE != null && x.StationBE == cleanId) || (x.StationId != null && x.StationId.Replace("-", "") == cleanId))
                .Select(x => x.Name)
                .First();

            //Không tìm thấy trạm trong danh mục thì hiển thị tạm mã trạm
            return string.IsNullOrWhiteSpace(name) ? stationId : name;
        }
    }
}
