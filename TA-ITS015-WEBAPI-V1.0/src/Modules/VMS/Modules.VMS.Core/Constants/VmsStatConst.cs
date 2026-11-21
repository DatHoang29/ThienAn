namespace Modules.VMS.Core.Constants
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Hằng số phục vụ luồng lấy/lưu trạng thái bên trong thiết bị VMS/LCS.
    /// Created date: 2026-08-20
    /// </summary>
    public static class VmsStatConst
    {
        /// <summary> Nguồn lấy trạng thái </summary>
        public static class Source
        {
            /// <summary> Người dùng bấm lấy thủ công </summary>
            public const string Manual = "manual";

            /// <summary> Hangfire tự động lấy theo chu kỳ </summary>
            public const string Auto = "auto";
        }

        /// <summary>
        /// Nhóm loại dữ liệu telemetry — mỗi nhóm tương ứng MỘT cột JSON trong VmsEquipmentTelemetry.
        /// FE dựa vào các nhóm này để hiển thị thành từng khối.
        /// </summary>
        public static class Group
        {
            /// <summary> Tài nguyên/vận hành hiển thị: độ sáng, trạng thái nguồn -> cột ResourceMetric </summary>
            public const string Resource = "resource";

            /// <summary> Môi trường: nhiệt độ, độ ẩm, gió, độ nghiêng -> cột EnvironmentMetric </summary>
            public const string Environment = "environment";

            /// <summary> Chẩn đoán/sức khỏe: lỗi pixel, firmware, cửa tủ -> cột DiagnosticMetric </summary>
            public const string Diagnostic = "diagnostic";
        }

        // Lớp Job (cấu hình Hangfire cho job tự động lấy trạng thái) đã BỎ ngày 23/09/2026.
        // Chu kỳ thu thập giờ do dịch vụ ITS.VMS.Worker quản lý qua Telemetry:CollectIntervalMinutes
        // trong appsettings.json của dịch vụ, không còn đọc cron từ SysOpConfig.
    }
}
