using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using SqlSugar;

namespace Module.ShareData.Core.Entities
{
    /// <summary>
    /// Description: Nguồn sự kiện nội bộ dùng làm trigger cho đăng ký chế độ EVENT.
    ///              Danh mục do backend sở hữu (biết subscribe NATS + xử lý payload); FE chỉ chọn để gán.
    /// Created date: 2026-08-04
    /// </summary>
    [SugarTable("ShareDataEventSource")]
    public class ShareDataEventSource : EntityTenant
    {

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
