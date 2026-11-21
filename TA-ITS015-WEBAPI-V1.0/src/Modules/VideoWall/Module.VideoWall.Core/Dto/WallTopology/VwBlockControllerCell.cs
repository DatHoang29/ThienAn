namespace Module.VideoWall.Core.Dto.WallTopology
{
    /// <summary>
    /// Description: 1 ô trong mảng JSON VwWallBlock.ControllerConfig: ô (Col, Row) của khối do bộ điều khiển nào phụ trách.
    ///              Col / Row tính trong khối, 0-based. Một khối có thể có nhiều bộ (mỗi ô một bộ, không chồng nhau).
    ///              Ô không có trong mảng = ô không thuộc bộ nào. Chỉ gán Bộ hiển thị / Bộ độc lập.
    ///              Gán cổng ra vào ô làm riêng, không lưu ở đây.
    /// Created date: 07/11/2026
    /// </summary>
    public class VwBlockControllerCell
    {
        public int Col { get; set; }
        public int Row { get; set; }
        public string ControllerId { get; set; } = string.Empty;
    }
}
