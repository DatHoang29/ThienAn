using Module.VideoWall.Core.Entities;

namespace Module.VideoWall.Core.Helper
{
    /// <summary>
    /// Description: Xác định vai trò bộ điều khiển trong tường phân cấp (cascade) từ ParentControllerId,
    ///              thay cho cột Role ("center"/"sub") đã bỏ.
    ///
    ///              Sơ đồ: mọi nguồn đầu vào cắm vào bộ trung tâm → đầu ra bộ trung tâm là đầu vào
    ///              của các bộ con → bộ con nối tới màn hình.
    ///                - Bộ con (sub)        : có ParentControllerId.
    ///                - Bộ trung tâm (center): được ít nhất 1 bộ (chưa xóa) trỏ tới làm cha.
    ///                - Bộ độc lập          : không cha, không con (chế độ standalone — không có center).
    /// Created date: 26/09/2026
    /// </summary>
    public static class VwControllerTopology
    {
        /// <summary>
        /// Description: Bộ điều khiển là bộ con (có bộ cha).
        /// </summary>
        public static bool IsSub(VwController? controller) =>
            controller != null && !string.IsNullOrWhiteSpace(controller.ParentControllerId);

        /// <summary>
        /// Description: Tập ID các bộ trung tâm suy ra từ danh sách bộ điều khiển (đã lọc chưa xóa).
        /// </summary>
        public static HashSet<string> GetCenterIds(IEnumerable<VwController> controllers) =>
            controllers
                .Where(IsSub)
                .Select(u => u.ParentControllerId!)
                .ToHashSet();

        /// <summary>
        /// Description: Bộ điều khiển là bộ trung tâm (có ít nhất 1 bộ con trong danh sách).
        /// </summary>
        public static bool IsCenter(VwController? controller, IEnumerable<VwController> controllers) =>
            controller != null
            && controllers.Any(u => u.ParentControllerId == controller.ID);

        /// <summary>
        /// Description: Lọc truy vấn DB chỉ lấy bộ trung tâm (có bộ con chưa xóa trỏ tới).
        /// </summary>
        public static ISugarQueryable<VwController> WhereIsCenter(this ISugarQueryable<VwController> query) =>
            query.Where(u => SqlFunc.Subqueryable<VwController>()
                .Where(c => c.ParentControllerId == u.ID && c.IsDelete == null)
                .Any());
    }
}
