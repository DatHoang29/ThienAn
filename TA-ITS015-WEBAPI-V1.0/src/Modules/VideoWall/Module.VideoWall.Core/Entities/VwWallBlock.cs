using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using SqlSugar;

namespace Module.VideoWall.Core.Entities
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Khối màn hình của tường — một mảng màn liền nhau có lưới cột × hàng riêng.
    ///              Tường gồm 1..n khối xếp ngang trái → phải theo BlockNo. Vị trí màn hình
    ///              (VwScreen) và cửa sổ kịch bản (VwWindowScene) đều tính TRONG khối.
    /// Created date: 22/10/2026
    /// </summary>
    [SugarTable("VwWallBlock")]
    public class VwWallBlock : EntityTenant
    {
        /// <summary>
        /// Description: Tường chứa khối (VwWallTopology.ID).
        /// Created date: 22/10/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? WallTopologyId { get; set; }

        /// <summary>
        /// Description: Số thứ tự khối, 0-based, trái → phải (hiển thị A, B, C...).
        ///              Duy nhất trong một tường; VwScreen / VwWindowScene tham chiếu khối qua số này.
        /// Created date: 22/10/2026
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public int? BlockNo { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? Name { get; set; }

        /// <summary>
        /// Description: Số cột màn của khối.
        /// Created date: 22/10/2026
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public int? Cols { get; set; }

        /// <summary>
        /// Description: Số hàng màn của khối.
        /// Created date: 22/10/2026
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public int? Rows { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Remark { get; set; }

        /// <summary>
        /// Description: Mảng JSON List&lt;VwBlockControllerCell&gt;: ô nào của khối do bộ điều khiển nào phụ trách
        ///              (nhiều bộ trên 1 khối, không chồng ô). null = chưa gán. Không chép khi nhân bản bố cục.
        /// Created date: 07/11/2026
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDataType = StaticConfig.CodeFirst_BigString)]
        public string? ControllerConfig { get; set; }
    }
}
