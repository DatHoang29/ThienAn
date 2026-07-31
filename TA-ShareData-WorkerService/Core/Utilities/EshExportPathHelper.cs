namespace TA_ShareData_WorkerService.Core.Utilities
{
    /// <summary>
    /// Helper hỗ trợ khởi tạo đường dẫn lưu trữ file kết xuất dữ liệu (ESHARE V1)
    /// Author: Đạt
    /// Created date: 27/07/2026
    /// </summary>
    public static class EshExportPathHelper
    {
        /// <summary>
        /// Tạo đường dẫn tương đối lưu trữ file kết xuất dữ liệu cho đối tác
        /// Cú pháp: sharedata/send/{partnerCode}/{yyyyMM}/{dd}/{HH}/{datatypeId}/{datatypeId}_{yyyyMMddHHmmssfff}.json
        /// </summary>
        public static string GenerateExportRelativePath(string partnerCode, string datatypeId, DateTime time, string extension = "json")
        {
            var ext = extension.TrimStart('.').ToLower();
            return $"sharedata/send/{partnerCode}/{time:yyyyMM}/{time:dd}/{time:HH}/{datatypeId}/{datatypeId}_{time:yyyyMMddHHmmssfff}.{ext}";
        }
    }
}
