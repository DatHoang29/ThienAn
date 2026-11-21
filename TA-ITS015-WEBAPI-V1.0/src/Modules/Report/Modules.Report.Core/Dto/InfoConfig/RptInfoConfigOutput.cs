using Modules.Report.Core.Entities;

namespace Modules.Report.Core.Dto.InfoConfig
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: DTO thông tin cấu hình báo cáo
    /// Created date: 27/07/2026
    /// </summary>
    public class RptInfoConfigOutput : RptInfoConfig { }

    /// <summary>
    /// Author: ChuongLP
    /// Description: DTO thông tin cấu hình báo cáo (phân trang)
    /// Created date: 27/07/2026
    /// </summary>
    public class PageRptInfoConfigOutput : RptInfoConfig { }

    /// <summary>
    /// Author: ChuongLP
    /// Description: Cấu hình đã gom nhóm theo GroupCode -> Code -> Value.
    /// Đây chính là cấu trúc được serialize vào JSON của FastReport.
    /// Ví dụ: { "header": { "logo": "...", "title": "..." }, "footer": { "left_name": "..." } }
    /// Created date: 27/07/2026
    /// </summary>
    public class RptInfoConfigDataOutput : Dictionary<string, Dictionary<string, string?>>
    {
    }
}
