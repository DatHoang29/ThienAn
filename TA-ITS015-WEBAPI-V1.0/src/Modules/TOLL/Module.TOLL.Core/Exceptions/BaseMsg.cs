namespace Modules.TOLL.Core.Exceptions
{
    /// <summary>
    /// Author: ChuongLP
    /// Created date: 23/07/2026
    /// </summary>
    public class BaseMsg
    {
        protected class Prefix
        {
            public const string Message = "lz.message.toll.";
            public const string Validation = "lz.validation.toll.";
            public const string Exception = "lz.exception.toll.";
            public const string Entity = "lz.entity.toll.";
        }

        public class Entity
        {
            // Ví dụ: public const string LaneId = Prefix.Entity + "laneId";
        }

        /// <summary>
        /// Thông báo lỗi nghiệp vụ (bản dịch lưu trong SysTerminology, Name = "lz.exception", Code = "toll.xxx")
        /// Author: HieuNV
        /// Created date: 07/10/2026
        /// </summary>
        public class Exception
        {
            /// <summary>
            /// "Đơn vị [{0}] không tồn tại hoặc không phải loại Trạm thu phí."
            /// </summary>
            public const string WorkUnitNotToll = Prefix.Exception + "workUnitNotToll";

            /// <summary>
            /// "Đơn vị [{0}] đã được gán cho trạm [{1}]."
            /// </summary>
            public const string WorkUnitAlreadyMapped = Prefix.Exception + "workUnitAlreadyMapped";
        }
    }
}
