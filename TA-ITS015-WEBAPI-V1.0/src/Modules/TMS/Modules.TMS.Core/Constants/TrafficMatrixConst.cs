using System.Globalization;
using Modules.TMS.Core.Dto.TrafficMatrix;

namespace Modules.TMS.Core.Constants
{
    /// <summary>
    /// Author: KienLT
    /// Description: Hằng số của bảng ma trận vận tốc / lưu lượng theo giờ.
    ///
    /// <para><b>Nguyên tắc:</b> file này CHỈ chứa những thứ không phải tham số vận hành — mã định danh
    /// của bản ghi cấu hình và logic đọc chúng. Mọi giá trị có thể phải chỉnh theo dự án (dải màu,
    /// ngưỡng vận tốc / lưu lượng, loại zone làm hàng, danh sách chế độ xem) đều nằm trong DB:</para>
    ///
    /// <list type="bullet">
    /// <item><b>Cấu hình tham số</b> (<c>SysConfigType</c> + <c>SysConfigData</c>): danh sách chế độ xem
    /// và hai bảng dải màu. Dải màu quy ước: <c>Code</c> = khoảng giá trị, <c>Name</c> = nhãn hiển thị,
    /// <c>Value</c> = mã màu (theo đúng tiền lệ của danh mục <c>equipment_state_color</c>),
    /// <c>OrderNo</c> = thứ tự dải.</item>
    /// <item><b>Cấu hình hoạt động</b> (<c>SysOpConfig</c>): loại zone dùng làm hàng của ma trận.</item>
    /// </list>
    ///
    /// <para>Thiếu cấu hình thì tính năng vẫn chạy, chỉ mất phần trang trí / mất mức gom hàng —
    /// xem chú thích ở từng chỗ đọc. Không có giá trị mặc định nào được nhúng vào code.</para>
    ///
    /// Created date: 11/08/2026
    /// </summary>
    public static class TrafficMatrixConst
    {
        /// <summary>Số cột của ma trận: 24 giờ (0..23).</summary>
        public const int HoursPerDay = 24;

        /// <summary>Chỉ số dải khi ô KHÔNG có dữ liệu, hoặc khi chưa cấu hình dải màu nào.</summary>
        public const int NoDataBandIndex = -1;

        /// <summary>
        /// Mã loại cấu hình tham số (<c>SysConfigType.Code</c>). Đây là KHOÁ TRA CỨU, không phải giá trị
        /// nghiệp vụ, nên để trong code.
        /// </summary>
        public static class ConfigType
        {
            /// <summary>Danh sách chế độ xem của bảng ma trận (vận tốc / lưu lượng).</summary>
            public const string Metric = "tms_traffic_matrix_metric";

            /// <summary>Dải màu theo vận tốc.</summary>
            public const string SpeedBand = "tms_traffic_matrix_speed_band";

            /// <summary>Dải màu theo lưu lượng.</summary>
            public const string VolumeBand = "tms_traffic_matrix_volume_band";
        }

        /// <summary>Khoá tra cứu trong bảng cấu hình hoạt động (<c>SysOpConfig</c>).</summary>
        public static class OpConfig
        {
            /// <summary><c>SysOpConfig.GroupCode</c> của nhóm cấu hình bảng ma trận.</summary>
            public const string GroupCode = "tms_traffic_matrix";

            /// <summary>
            /// <c>SysOpConfig.Code</c> — loại zone (<c>tms_zone_type</c>) dùng làm HÀNG của ma trận.
            /// Không cấu hình ⇒ ma trận lấy thẳng zone có dữ liệu làm hàng.
            /// </summary>
            public const string RowZoneType = "row_zone_type";

            /// <summary>
            /// <c>SysOpConfig.Code</c> — <c>TmsZone.ID</c> của nhánh được xem (thường là tuyến).
            /// Chỉ những zone thuộc nhánh này mới lên bảng. Không cấu hình ⇒ lấy toàn bộ.
            /// FE truyền <c>ZoneId</c> sẽ ghi đè giá trị này.
            /// </summary>
            public const string RootZoneId = "root_zone_id";
        }

        /// <summary>
        /// Đọc một dải từ bản ghi cấu hình tham số.
        /// <para>
        /// <paramref name="rangeCode"/> là <c>SysConfigData.Code</c> theo dạng khoảng nửa mở
        /// <c>[from, to)</c>: <c>"-30"</c> = dưới 30, <c>"30-40"</c> = từ 30 đến dưới 40,
        /// <c>"90-"</c> = từ 90 trở lên. Sai định dạng ⇒ trả null để bên gọi bỏ qua bản ghi đó
        /// thay vì làm hỏng cả thang màu.
        /// </para>
        /// </summary>
        public static TrafficMatrixBand? ParseBand(string? rangeCode, string? name, string? color)
        {
            if (string.IsNullOrWhiteSpace(rangeCode)) return null;

            var parts = rangeCode.Split('-');
            if (parts.Length != 2) return null;

            // Phân biệt rõ hai trường hợp: biên ĐỂ TRỐNG là mở có ý đồ, biên SAI ĐỊNH DẠNG là gõ nhầm.
            // Gõ nhầm mà bỏ qua thành biên mở thì cả thang màu bị lệch mà không ai biết.
            if (!TryParseBound(parts[0], out var from)) return null;
            if (!TryParseBound(parts[1], out var to)) return null;

            // Ít nhất một biên phải có, và biên dưới không được vượt biên trên.
            if (from is null && to is null) return null;
            if (from is double f && to is double t && f >= t) return null;

            return new TrafficMatrixBand(from, to, color) { Name = name };
        }

        /// <summary>
        /// Đọc một biên của dải. Rỗng ⇒ true kèm null (biên mở). Có chữ nhưng không phải số ⇒ false.
        /// </summary>
        private static bool TryParseBound(string raw, out double? bound)
        {
            bound = null;

            var text = raw.Trim();
            if (text.Length == 0) return true;

            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                return false;

            bound = value;
            return true;
        }

        /// <summary>
        /// Chỉ số dải chứa <paramref name="value"/>; null hoặc chưa cấu hình dải nào ⇒
        /// <see cref="NoDataBandIndex"/>. Giá trị nằm ngoài mọi dải cũng trả về
        /// <see cref="NoDataBandIndex"/> — thà không tô màu còn hơn tô sai dải.
        /// </summary>
        public static int BandIndexOf(IReadOnlyList<TrafficMatrixBand> bands, double? value)
        {
            if (value is not double v || bands is null || bands.Count == 0) return NoDataBandIndex;

            for (var i = 0; i < bands.Count; i++)
                if (bands[i].Contains(v)) return i;

            return NoDataBandIndex;
        }
    }
}
