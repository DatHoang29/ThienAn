using System;

namespace Module.TOLL.Core.Dto.ManualFetch
{
    /// <summary>
    /// Body FE gửi lên khi bấm nút "Lấy dữ liệu thủ công".
    /// FE chỉ gửi khoảng thời gian (nếu có) — loại dữ liệu (DataType) do từng controller quyết định phía server,
    /// nên KHÔNG expose DataType ra API.
    /// FromDate/ToDate là giờ UTC+0 (FE gửi lên); server sẽ convert về giờ local trước khi phát lệnh.
    /// Author: ChuongLP
    /// Created date: 14/09/2026
    /// </summary>
    public class ManualFetchTollRequest
    {
        /// <summary>Thời gian bắt đầu khoảng dữ liệu cần lấy (UTC+0). Bỏ trống với loại dữ liệu không cần khoảng thời gian.</summary>
        public DateTime? FromDate { get; set; }

        /// <summary>Thời gian kết thúc khoảng dữ liệu cần lấy (UTC+0). Bỏ trống với loại dữ liệu không cần khoảng thời gian.</summary>
        public DateTime? ToDate { get; set; }
    }
}
