using System.Data.Common;

namespace ShareDataWorker.Infrastructure.Logging
{
    /// <summary>
    /// Description: Lọc thông báo lỗi trước khi ghi vào nhật ký hiển thị trên giao diện.
    ///              Lỗi gốc của máy chủ CSDL thường nêu tên bảng, cột, ràng buộc hoặc cả câu SQL nên chỉ được ghi ở nhật ký máy chủ.
    /// Created date: 22/10/2026
    /// </summary>
    public static class SafeErrorMessage
    {
        public const string Database = "Lỗi truy vấn cơ sở dữ liệu. Xem chi tiết trong nhật ký máy chủ.";

        /// <summary>
        /// Description: Trả thông báo an toàn để ghi vào nhật ký giao diện: lỗi CSDL (ở bất kỳ tầng lỗi lồng nào) thay bằng câu chung, lỗi khác giữ nguyên.
        /// </summary>
        public static string ForLog(Exception ex)
        {
            for (var current = ex; current != null; current = current.InnerException)
            {
                if (current is DbException || current is SqlSugar.SqlSugarException)
                    return Database;
            }

            return ex.Message;
        }
    }
}
