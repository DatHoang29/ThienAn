namespace Modules.Integration.Delivery.Base.Enums
{
    /// <summary>
    /// Author: DatHQ
    /// Description: Các phương thức mở rộng cho ConnectionType.
    /// Created date: 19/10/2026
    /// Modified date: 20/10/2026
    /// </summary>
    public static class ConnectionTypeExtensions
    {
        /// <summary>
        /// Kiểm tra xem loại kết nối có phải kênh tập tin (File) hay không.
        /// </summary>
        public static bool IsFileChannel(this ConnectionType type) => type == ConnectionType.File;

        /// <summary>
        /// Kiểm tra xem loại kết nối nullable có phải kênh tập tin (File) hay không.
        /// </summary>
        public static bool IsFileChannel(this ConnectionType? type) => type == ConnectionType.File;

        /// <summary>
        /// Kiểm tra xem loại kết nối có phải FTPS hay không.
        /// </summary>
        public static bool IsFtps(this ConnectionType type) => type == ConnectionType.Ftps;

        /// <summary>
        /// Kiểm tra xem loại kết nối nullable có phải FTPS hay không.
        /// </summary>
        public static bool IsFtps(this ConnectionType? type) => type == ConnectionType.Ftps;
    }
}
