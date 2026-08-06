using SqlSugar;
using Modules.ShareDataWorker.Core.Constants;

namespace Modules.ShareDataWorker.Core.Entities
{
    /// <summary>
    /// Phiên kết lộ kết nối trao đổi dữ liệu.
    /// Author: Đạt
    /// Created date: 05/08/2026
    /// </summary>
    [SugarTable("ShareDataSession")]
    public class ShareDataSession : EntityTenant
    {
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? PartnerId { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? SessionToken { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length16)]
        public string? Status { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? ConnectedAt { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? DisconnectedAt { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Remark { get; set; }
    }
}
