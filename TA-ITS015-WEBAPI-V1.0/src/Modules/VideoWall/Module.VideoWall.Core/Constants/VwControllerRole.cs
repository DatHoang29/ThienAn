namespace Module.VideoWall.Core.Constants
{
    /// <summary>
    /// Description: Vai trò bộ điều khiển (VwController.Role) — mã của SysConfigData loại videowall_controller_role.
    ///              - Distribution (Bộ phân phối): nhận nguồn, cổng ra nối sang bộ hiển thị. Chỉ làm cha, không có cha.
    ///              - Display (Bộ hiển thị): cổng ra nối tới màn. Bắt buộc có cha là Bộ phân phối.
    ///              - Standalone (Bộ độc lập): tự nhận nguồn, tự nối màn. Không cha, không con.
    ///              Bộ hiển thị / độc lập mới được gán vào ô của bố cục (VwWallBlock.ControllerConfig).
    /// Created date: 07/11/2026
    /// </summary>
    public static class VwControllerRole
    {
        public const string Distribution = "distribution";
        public const string Display = "display";
        public const string Standalone = "standalone";

        public static readonly string[] All = [Distribution, Display, Standalone];

        /// <summary>Có cổng ra nối tới màn ⇒ được gán vào ô của bố cục.</summary>
        public static bool DrivesScreens(string? role) => role == Display || role == Standalone;
    }
}
