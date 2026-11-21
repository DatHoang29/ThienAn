using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using SqlSugar;

namespace Module.VideoWall.Core.Entities
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Scene
    /// Created date: 14/08/2026
    /// </summary>
    [SugarTable("VwScene")]
    public class VwScene : EntityTenant
    {
        /// <summary>
        /// Description: Tên kịch bản hiển thị.
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? Name { get; set; }

        /// <summary>
        /// Description: Đánh dấu kịch bản mặc định (IsDefault).
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public BaseEnums.DefaultEnum? IsDefault { get; set; }

        /// <summary>
        /// Description: Mã OutputId == SID định danh trên thiết bị phần cứng ISAPI .
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? OutputId { get; set; }

        /// <summary>
        /// Description: Đường dẫn ảnh đại diện thu nhỏ (Thumbnail) của kịch bản.
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Thumbnail { get; set; }

        /// <summary>
        /// Description: Trạng thái kích hoạt (Enable / Disable) của kịch bản.
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public BaseEnums.StatusEnum? Status { get; set; }

        /// <summary>
        /// Description: Cờ trạng thái đang chạy thật trên thiết bị (Activate / DeActivate).
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public BaseEnums.ActiveScene? ActiveScene { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? OrderNo { get; set; }

        /// <summary>
        /// Description: Ghi chú Remark bổ sung cho kịch bản.
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Remark { get; set; }

        /// <summary>
        /// Description: Bố cục tường mà kịch bản được dựng trên (VwWallTopology.ID). Gán khi tạo, không đổi.
        ///              Chỉ kịch bản thuộc bố cục đang dùng (Status = Dùng) mới được kích hoạt.
        /// Created date: 22/10/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? WallTopologyId { get; set; }
    }
}