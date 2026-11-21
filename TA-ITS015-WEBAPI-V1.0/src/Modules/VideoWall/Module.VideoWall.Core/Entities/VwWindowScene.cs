using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using SqlSugar;

namespace Module.VideoWall.Core.Entities
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Window Scene
    /// Created date: 14/08/2026
    /// </summary>
    [SugarTable("VwWindowScene")]
    public class VwWindowScene : EntityTenant
    {
        /// <summary>
        /// Description: Tên cửa sổ kịch bản hiển thị trên VideoWall.
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? Name { get; set; }

        /// <summary>
        /// Description: Mã định danh SceneId thuộc kịch bản (VwScene).
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? SceneId { get; set; }
        
        /// <summary>
        /// Description: Mã định danh SourceId của nguồn tín hiệu (VwSource) gán vào cửa sổ.
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? SourceId { get; set; }

        /// <summary>
        /// Description: Khối chứa cửa sổ (VwWallBlock.ID) — khối thuộc bố cục của kịch bản
        ///              (VwWallBlock.WallTopologyId = VwScene.WallTopologyId). Cửa sổ không vắt qua 2 khối.
        /// Created date: 22/10/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? WallBlockId { get; set; }

        /// <summary>
        /// Description: Số thứ tự của khối (= VwWallBlock.BlockNo của WallBlockId), BE ghi theo WallBlockId.
        ///              Khối trong một bố cục không đổi số ⇒ giữ lại để tính toạ độ / hiển thị không cần join.
        /// Created date: 22/10/2026
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public int? BlockNo { get; set; }

        /// <summary>
        /// Description: Toạ độ X/Y/W/H TRONG khối, đơn vị pixel logic của panel
        ///              (1 ô = VwWallTopology.ScreenWidth × ScreenHeight).
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public int? X { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? Y { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? W { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? H { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? ZIndex { get; set; }

        /// <summary>
        /// Description: Nhãn văn bản (Label) hiển thị trên thanh tiêu đề cửa sổ.
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Label { get; set; }

        /// <summary>
        /// Description: Độ trong suốt Opacity của cửa sổ hiển thị.
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public int? Opacity { get; set; }

        /// <summary>
        /// Description: Trạng thái hiển thị (Visible / Hidden) của cửa sổ kịch bản.
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public BaseEnums.SceneWindowVisible Visible { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? OrderNo { get; set; }

        /// <summary>
        /// Description: Ghi chú Remark bổ sung cho cửa sổ kịch bản.
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Remark { get; set; }
    }
}