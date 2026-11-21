using System.ComponentModel;

namespace Module.TOLL.Core.Enums
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Enum dùng chung cho module thu phí (TOLL)
    /// Created date: 27/07/2026
    /// </summary>
    public class TollEnums
    {
        /// <summary>
        /// Hướng giao dịch của xe qua trạm
        /// </summary>
        public enum DirectionEnum
        {
            /// <summary>
            /// Xe vào - dữ liệu lấy từ TollTransactionIn
            /// </summary>
            [Description("Xe vào")]
            In = 1,

            /// <summary>
            /// Xe ra - dữ liệu lấy từ TollTransactionOut
            /// </summary>
            [Description("Xe ra")]
            Out = 2
        }

        /// <summary>
        /// Giá trị hướng giao dịch dạng chuỗi.
        /// Dùng khi gán trực tiếp trong biểu thức Select của SqlSugar
        /// (biểu thức được dịch sang SQL nên không gọi được ToString của enum).
        /// Giá trị phải khớp với <see cref="DirectionEnum"/>.
        /// </summary>
        public static class DirectionCode
        {
            /// <summary>Xe vào - khớp DirectionEnum.In</summary>
            public const string In = "1";

            /// <summary>Xe ra - khớp DirectionEnum.Out</summary>
            public const string Out = "2";
        }
    }
}
