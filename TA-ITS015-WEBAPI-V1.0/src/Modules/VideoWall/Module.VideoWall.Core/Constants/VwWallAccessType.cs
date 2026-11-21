namespace Module.VideoWall.Core.Constants
{
    /// <summary>
    /// Description: Kiểu quyền vùng màn hình hiệu lực của người dùng đang đăng nhập (trả về qua API GetMy).
    /// Created date: 27/09/2026
    /// </summary>
    public static class VwWallAccessType
    {
        /// <summary>Toàn quyền cả tường: SuperAdmin, tiến trình nền, cờ BypassPermission hoặc bản ghi IsFullAccess = true.</summary>
        public const string Full = "full";

        /// <summary>Chỉ được thao tác trong các ô lưới được cấp (AllowedCells).</summary>
        public const string Area = "area";

        /// <summary>Chưa được cấp vùng nào (không có bản ghi hoặc cấu hình rỗng / lỗi) ⇒ không được thao tác.</summary>
        public const string None = "none";
    }
}
