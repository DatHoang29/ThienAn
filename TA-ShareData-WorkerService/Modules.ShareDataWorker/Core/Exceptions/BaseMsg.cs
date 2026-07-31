namespace TA_ShareData_WorkerService.Core.Exceptions
{
    /// <summary>
    /// Định nghĩa hằng số thông báo lỗi & log cho ShareData Worker Service (Rule 2.1.4)
    /// Author: Đạt
    /// Created date: 31/07/2026
    /// </summary>
    public static class BaseMsg
    {
        public static class Worker
        {
            public const string Started = "🚀 [ShareDataExportService] Started background worker service at: {Time}";
            public const string Stopping = "🛑 [ShareDataExportService] Stopping background worker service at: {Time}";
            public const string CycleError = "❌ Lỗi xảy ra trong chu kỳ chạy của ShareDataExportService. Chi tiết: {Message}";
            public const string ExportError = "❌ Lỗi kết xuất dữ liệu cho Subscription ID {Id}: {Message}";
        }

        public static class ExportValidation
        {
            public const string ProfileNotFound = "Mapping profile không tồn tại hoặc đã bị xóa.";
            public const string FieldMappingEmpty = "Mapping profile không có cấu hình field mapping nào.";
            public const string DataSourceNotFound = "Nguồn dữ liệu không tồn tại.";
            public const string TableEmptyForPicker = "Tên bảng nguồn không được để trống khi chọn loại FIELD_PICKER.";
            public const string QueryEmptyForSavedQuery = "Câu lệnh SQL QueryText không được để trống khi chọn loại SAVED_QUERY.";
            public const string QueryMustStartWithSelect = "QueryText phải bắt đầu bằng SELECT.";
            public const string ForbiddenKeyword = "QueryText chứa từ khóa bị cấm: '{0}'.";
            public const string UnsupportedDataSource = "Loại DataSource không được hỗ trợ.";
            public const string MissingRequiredFields = "Thiếu field bắt buộc: {0}";
        }
    }
}
