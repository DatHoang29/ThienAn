using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Core.Entities
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Lưu sự kiện điều khiển thiết bị theo sự cố (chạy song song TmsSignalLog)
    /// Created date: 02/07/2026
    /// </summary>
    [SugarTable("TmsIncidentControl")]
    public class TmsIncidentControl : EntityTenant
    {
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? IncidentId { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? EquipmentId { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? State { get; set; }
        // THÊM: độ ưu tiên khi sự cố chiếm giữ thiết bị (số nhỏ = ưu tiên cao, 1 là cao nhất)
        [SugarColumn(IsNullable = true)]
        public int? Priority { get; set; }

        // ChuongLP - 19/10/2026: nội dung (ảnh) sự cố này gửi lên thiết bị. Thiết bị chỉ hiện 1 nội dung (LCS) dùng để
        // trả lại ảnh của sự cố còn lại khi sự cố đang hiển thị kết thúc.
        [SugarColumn(IsNullable = true, Length = EntityConst.Length512, ColumnDescription = "Nội dung sự cố gửi lên thiết bị")]
        public string? Url { get; set; }
    }
}
