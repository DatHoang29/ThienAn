using System;

namespace Module.TOLL.Core.Dto.LichSuThaoTac
{
    /// <summary>
    /// Output hiển thị 1 bản ghi lịch sử truy cập (map theo entity TollAccessHistory).
    /// Tên trường khớp với các cột lưới phía FE (lichSuTruyCap) để phục vụ sắp xếp.
    /// Author: ChuongLP
    /// Created date: 24/09/2026
    /// </summary>
    public class LichSuThaoTacOutput
    {
        /// <summary>
        /// ID bản ghi trong ITS
        /// </summary>
        public string? ID { get; set; }

        /// <summary>
        /// Thời gian truy cập (map từ AccessTime) - cột "Thời gian" trên FE
        /// </summary>
        public DateTime? LogDateTime { get; set; }

        /// <summary>
        /// Tên hiển thị (map từ EmployeeName) - cột "Tên hiển thị" trên FE
        /// </summary>
        public string? DisplayTitle { get; set; }

        /// <summary>
        /// Tên hành động (map từ Status: Login/Logout) - cột "Tên hành động" trên FE
        /// </summary>
        public string? ActionName { get; set; }

        /// <summary>
        /// Tài khoản / mã nhân viên (map từ EmployeeCode) - cột "Tài khoản" trên FE
        /// </summary>
        public string? Account { get; set; }

        /// <summary>
        /// Thời điểm ITS đồng bộ bản ghi
        /// </summary>
        public DateTime? LastSyncTime { get; set; }
    }
}
