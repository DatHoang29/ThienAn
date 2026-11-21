namespace Module.VideoWall.Core.Dto.WallPermission
{
    /// <summary>
    /// Description: 1 ô lưới rời rạc trong mảng JSON Config của VwWallPermission.
    ///              Block = VwWallBlock.BlockNo; Col / Row tính trong khối, 0-based.
    /// Created date: 11/09/2026 — cập nhật 22/10/2026 (thêm Block)
    /// </summary>
    public class VwGridCell
    {
        public int Block { get; set; }
        public int Col { get; set; }
        public int Row { get; set; }
    }
}
