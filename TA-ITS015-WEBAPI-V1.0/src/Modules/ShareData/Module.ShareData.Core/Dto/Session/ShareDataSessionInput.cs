using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Module.ShareData.Core.Dto.Session
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO phân trang danh sách phiên kết nối
    /// Created date: 2026-08-05
    /// </summary>
    public class ShareDataPageSessionInput : BasePageInput
    {
        public string? PartnerId { get; set; }
        public BaseEnums.SessionDirection? Direction { get; set; }
        public BaseEnums.SessionFsmState? State { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO tìm kiếm danh sách phiên kết nối (không phân trang).
    /// Created date: 2026-08-05
    /// </summary>
    public class ShareDataSessionInput
    {
        public string? ID { get; set; }
        public string? PartnerId { get; set; }
        public BaseEnums.SessionDirection? Direction { get; set; }
        public BaseEnums.SessionFsmState? State { get; set; }
        public bool? LatestPerPartner { get; set; } = true;
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO lấy chi tiết 1 phiên theo ID
    /// Created date: 2026-08-05
    /// </summary>
    public class ShareDataIdSessionInput : BaseIdInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO lấy số liệu tổng hợp cho các thẻ thống kê đầu màn vận hành
    /// Created date: 2026-08-05
    /// </summary>
    public class ShareDataSummarySessionInput
    {
        public string? PartnerId { get; set; }
    }
}
