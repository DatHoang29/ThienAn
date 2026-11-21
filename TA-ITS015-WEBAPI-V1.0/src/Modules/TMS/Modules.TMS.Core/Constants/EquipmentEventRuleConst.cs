namespace Modules.TMS.Core.Constants
{
    /// <summary>
    /// Constants cho cấu hình hiển thị VMS/LCS theo loại sự kiện (TmsEquipmentEventRule)
    /// Author: ChuongLP
    /// Created date: 16/07/2026
    /// </summary>
    public static class EquipmentEventRuleConst
    {
        /// <summary>
        /// Loại xử lý hiển thị thiết bị theo sự cố.
        /// Lưu ý: giá trị lưu DB là chuỗi tiếng Anh dưới đây.
        /// </summary>
        public static class ProcessType
        {
            /// <summary>
            /// Hiển thị TRƯỚC vị trí sự cố (upstream — thiết bị mà phương tiện đi qua trước khi tới điểm sự cố)
            /// </summary>
            public const string Before = "Before";

            /// <summary>
            /// Hiển thị SAU vị trí sự cố (downstream — thiết bị sau điểm sự cố)
            /// </summary>
            public const string After = "After";
        }
    }
}
