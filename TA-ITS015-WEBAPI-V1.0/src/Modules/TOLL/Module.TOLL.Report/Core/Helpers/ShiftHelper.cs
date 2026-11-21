using Modules.TOLL.Core.Entities;
using SqlSugar;

namespace Module.TOLL.Report.Core.Helpers
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Helper lấy tên ca làm việc để hiển thị trên header báo cáo.
    /// FE gửi xuống ShiftId (value của dropdown), BE tra ngược ra tên ca từ danh mục TollShift.
    /// Created date: 13/08/2026
    /// </summary>
    public static class ShiftHelper
    {
        /// <summary>
        /// Nhãn hiển thị khi không chọn ca cụ thể
        /// </summary>
        public const string TatCaCacCa = "Tất cả các ca";

        /// <summary>
        /// Lấy tên ca làm việc theo mã ca.
        /// </summary>
        /// <param name="db">SqlSugar client (thường là _baseRepo.Context)</param>
        /// <param name="shiftId">Mã ca; null/rỗng = xem tất cả các ca</param>
        /// <returns>Tên ca; fallback về chính mã ca nếu không có trong danh mục</returns>
        public static string GetTenCa(this ISqlSugarClient db, string? shiftId)
        {
            if (string.IsNullOrWhiteSpace(shiftId))
                return TatCaCacCa;

            var name = db.Queryable<TollShift>()
                .Where(x => x.ShiftId == shiftId)
                .Select(x => x.Name)
                .First();

            //Không tìm thấy ca trong danh mục thì hiển thị tạm mã ca
            return string.IsNullOrWhiteSpace(name) ? shiftId : name;
        }
    }
}
