using Modules.Report.Core.Entities;
using Shared.Core.Utilities;

namespace Modules.Report.Core.Dto.InfoConfig
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: DTO tham số phân trang
    /// Created date: 27/07/2026
    /// </summary>
    public class PageRptInfoConfigInput : BasePageInput
    {
        public string? ReportCode { get; set; }
        public string? GroupCode { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Value { get; set; }
    }

    /// <summary>
    /// Author: ChuongLP
    /// Description: DTO tham số tìm kiếm danh sách
    /// Created date: 27/07/2026
    /// </summary>
    public class RptInfoConfigInput
    {
        public string? ReportCode { get; set; }
        public string? GroupCode { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Value { get; set; }
    }

    /// <summary>
    /// Author: ChuongLP
    /// Description: DTO lấy cấu hình đã gom nhóm để truyền vào file báo cáo
    /// Created date: 27/07/2026
    /// </summary>
    public class RptInfoConfigDataInput
    {
        /// <summary>
        /// Mã báo cáo cần lấy cấu hình. Kết quả = cấu hình dùng chung ('*') + cấu hình riêng của báo cáo (ghi đè).
        /// </summary>
        public string? ReportCode { get; set; }
    }

    /// <summary>
    /// Author: ChuongLP
    /// Description: DTO thêm mới
    /// Created date: 27/07/2026
    /// </summary>
    public class AddRptInfoConfigInput : RptInfoConfig { }

    /// <summary>
    /// Author: ChuongLP
    /// Description: DTO cập nhật
    /// Created date: 27/07/2026
    /// </summary>
    public class UpdateRptInfoConfigInput : AddRptInfoConfigInput { }

    /// <summary>
    /// Author: ChuongLP
    /// Description: DTO xóa theo id
    /// Created date: 27/07/2026
    /// </summary>
    public class DeleteRptInfoConfigInput : BaseIdInput { }

    /// <summary>
    /// Author: ChuongLP
    /// Description: DTO lấy một record theo id
    /// Created date: 27/07/2026
    /// </summary>
    public class RptInfoConfigSingleInput
    {
        public string? Id { get; set; }
        public string? ReportCode { get; set; }
        public string? GroupCode { get; set; }
        public string? Code { get; set; }
    }
}
