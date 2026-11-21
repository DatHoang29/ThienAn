namespace Modules.TMS.Report.Controllers.OverSpeed.Dto
{
    /// <summary>
    /// Author: KienLT
    /// Description: Dữ liệu JSON cho template <c>OverSpeedReport.frx</c>: nhóm theo camera (kèm đoạn), mỗi nhóm có
    /// danh sách lượt vi phạm. Các giá trị có thể thiếu (ngưỡng, mức vượt) để dạng chuỗi đã định dạng — JSON null
    /// trong cột số làm FastReport lỗi khi tính biểu thức.
    /// <para>Các lớp trong file này GIỮ tên dù không theo đuôi <c>Output</c> (quy tắc DTO) —
    /// template <c>.frx</c> bind theo tên nguồn dữ liệu/thuộc tính, đổi tên phải sửa template cùng lúc.</para>
    /// Created date: 16/10/2026
    /// </summary>
    public class OverSpeedReport
    {
        public OverSpeedParameters Parameters { get; set; } = new();
        public OverSpeedColumnNames Columns { get; set; } = new();
        public OverSpeedSummary Summary { get; set; } = new();
        public List<OverSpeedGroupOutput> DataList { get; set; } = new();
    }

    public class OverSpeedParameters
    {
        public string ReportTitle { get; set; } = string.Empty;
        public string ReportLogo { get; set; } = string.Empty;
        public string ReportSign { get; set; } = string.Empty;
        public string DataDate { get; set; } = string.Empty;
        public string ReportDate { get; set; } = string.Empty;
        public string PageLabel { get; set; } = string.Empty;
        public string ReportUser { get; set; } = string.Empty;
        public string ReportFooter { get; set; } = string.Empty;
        public string FilterText { get; set; } = string.Empty;
        public string SpeedLimitNote { get; set; } = string.Empty;
    }

    public class OverSpeedColumnNames
    {
        public string Idx { get; set; } = "Idx";
        public string Time { get; set; } = "Time";
        public string LicensePlate { get; set; } = "LicensePlate";
        public string VehicleType { get; set; } = "VehicleType";
        public string VehicleSize { get; set; } = "VehicleSize";
        public string VehicleColor { get; set; } = "VehicleColor";
        public string Location { get; set; } = "Location";
        public string Speed { get; set; } = "Speed";
        public string SpeedLimit { get; set; } = "SpeedLimit";

        /// <summary>Nhãn ngưỡng KHÔNG viết hoa cho dòng tiêu đề nhóm.</summary>
        public string SpeedLimitLabel { get; set; } = "SpeedLimitLabel";

        public string Exceed { get; set; } = "Exceed";
        public string Zone { get; set; } = "Zone";
        public string Camera { get; set; } = "Camera";
        public string Direction { get; set; } = "Direction";
        public string TotalViolation { get; set; } = "TotalViolation";
        public string DistinctPlate { get; set; } = "DistinctPlate";
        public string MaxSpeed { get; set; } = "MaxSpeed";
        public string AvgExceed { get; set; } = "AvgExceed";
        public string Violation { get; set; } = "Violation";
    }

    public class OverSpeedSummary
    {
        public int TotalViolation { get; set; }
        public int DistinctPlate { get; set; }
        public string MaxSpeed { get; set; } = string.Empty;
        public string AvgExceed { get; set; } = string.Empty;
    }

    /// <summary>Một nhóm = một camera (đoạn chính của camera quyết định ngưỡng).</summary>
    public class OverSpeedGroupOutput
    {
        /// <summary>Dòng tiêu đề nhóm đã ghép sẵn: "Đoạn - Camera (Hướng: ...)", bỏ phần hướng khi không có.</summary>
        public string Title { get; set; } = string.Empty;

        public string Zone { get; set; } = string.Empty;
        public string Camera { get; set; } = string.Empty;
        public string Direction { get; set; } = string.Empty;
        public string SpeedLimit { get; set; } = string.Empty;
        public int Count { get; set; }
        public string MaxSpeed { get; set; } = string.Empty;
        public List<OverSpeedItemOutput> Items { get; set; } = new();
    }

    public class OverSpeedItemOutput
    {
        public int Idx { get; set; }
        public string Time { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        /// <summary>Cỡ xe đã dịch (theo chiều dài: &lt;6m / 6–12m / &gt;12m); rỗng khi VDS không suy được cỡ.</summary>
        public string VehicleSize { get; set; } = string.Empty;

        public string VehicleColor { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Speed { get; set; } = string.Empty;
        public string SpeedLimit { get; set; } = string.Empty;

        /// <summary>Mức vượt gộp km/h và %, ví dụ <c>+10(11%)</c>; rỗng khi chưa có ngưỡng.</summary>
        public string Exceed { get; set; } = string.Empty;

        /// <summary>Mức vượt (%) dạng số cho điều kiện tô màu của template; 0 khi chưa có ngưỡng.</summary>
        public double ExceedPercentValue { get; set; }
    }
}
