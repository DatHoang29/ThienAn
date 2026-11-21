namespace Modules.FMS.Core.Constants
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Hằng số phục vụ luồng lấy/lưu trạng thái bên trong thiết bị của phân hệ FMS.
    /// Created date: 2026-08-27
    /// </summary>
    public static class TelemetryConst
    {
        /// <summary> Nguồn lấy trạng thái </summary>
        public static class Source
        {
            /// <summary> Người dùng bấm lấy thủ công </summary>
            public const string Manual = "manual";

            /// <summary> Tự động lấy theo chu kỳ </summary>
            public const string Auto = "auto";
        }

        /// <summary>
        /// Nhóm loại dữ liệu telemetry — mỗi nhóm tương ứng MỘT cột JSON trong FmsEquipmentTelemetry.
        /// FE dựa vào các nhóm này để hiển thị thành từng khối.
        /// </summary>
        public static class Group
        {
            /// <summary> Tài nguyên/vận hành -> cột ResourceMetric </summary>
            public const string Resource = "resource";

            /// <summary> Môi trường -> cột EnvironmentMetric </summary>
            public const string Environment = "environment";

            /// <summary> Chẩn đoán/sức khỏe -> cột DiagnosticMetric </summary>
            public const string Diagnostic = "diagnostic";
        }
    }
}
