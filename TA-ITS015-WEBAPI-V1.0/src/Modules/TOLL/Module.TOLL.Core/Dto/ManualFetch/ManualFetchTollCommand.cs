using System;
using System.Collections.Generic;

namespace Module.TOLL.Core.Dto.ManualFetch
{
    /// <summary>
    /// Lệnh (message Wolverine) yêu cầu lấy dữ liệu thu phí thủ công.
    /// Mỗi controller dựng lệnh này với danh sách loại dữ liệu cố định của mình rồi dispatch qua MessBus;
    /// handler sẽ phát tín hiệu lên kênh NATS ta.its.control.toll cho dịch vụ ITS.Toll.Worker xử lý.
    /// Author: ChuongLP
    /// Created date: 14/09/2026
    /// </summary>
    public class ManualFetchTollCommand
    {
        /// <summary>Các loại dữ liệu cần lấy (giá trị lấy từ <see cref="Constants.TollSyncDataType"/>).</summary>
        public List<string> DataTypes { get; set; } = new();

        /// <summary>Thời gian bắt đầu (UTC+0 từ FE) — handler sẽ convert về giờ local trước khi phát lệnh.</summary>
        public DateTime? FromDate { get; set; }

        /// <summary>Thời gian kết thúc (UTC+0 từ FE) — handler sẽ convert về giờ local trước khi phát lệnh.</summary>
        public DateTime? ToDate { get; set; }
    }
}
