using SqlSugar;
using Modules.ShareDataWorker.Core.Constants;

namespace Modules.ShareDataWorker.Core.Entities
{
    /// <summary>
    /// Nguồn sự kiện nội bộ dùng làm trigger cho đăng ký chế độ EVENT.
    /// Author: Đạt
    /// Created date: 05/08/2026
    /// </summary>
    [SugarTable("ShareDataEventSource")]
    public class ShareDataEventSource : EntityTenant
    {
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? Code { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? Name { get; set; }

        /// <summary>Subject NATS nội bộ mà phân hệ lắng nghe — VD 'ta.its.data.incident'.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? Subject { get; set; }

        /// <summary>Mã loại dữ liệu (gói tin) gắn với sự kiện này (101–111).</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length16)]
        public string? DatatypeCode { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Description { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Remark { get; set; }
    }
}
