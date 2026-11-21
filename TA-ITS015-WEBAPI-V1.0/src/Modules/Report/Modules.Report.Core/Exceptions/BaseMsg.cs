using Shared.DTO.Constants.Localization;

namespace Modules.Report.Core.Exceptions
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Localization key riêng của phân hệ quản lý báo cáo.
    /// Các key dùng chung đã có sẵn trong BaseLocaleManager, không định nghĩa lại.
    /// Created date: 27/07/2026
    /// </summary>
    public class BaseMsg : BaseLocaleManager
    {
        public class RptInfoConfig
        {
            protected class Prefix
            {
                public const string Message = "lz.message.rptInfoConfig.";
                public const string Validation = "lz.validation.rptInfoConfig.";
                public const string Exception = "lz.exception.rptInfoConfig.";
                public const string Entity = "lz.entity.rptInfoConfig.";
            }

            public class Entity
            {
                public const string ReportCode = Prefix.Entity + "reportCode";
                public const string GroupCode = Prefix.Entity + "groupCode";
                public const string Value = Prefix.Entity + "value";
                public const string ValueType = Prefix.Entity + "valueType";
                public const string IsSysConfig = Prefix.Entity + "isSysConfig";
            }

            public class Exception
            {
                /// <summary>Trùng bộ khóa ReportCode + GroupCode + Code</summary>
                public const string DuplicateKey = Prefix.Exception + "duplicateKey";
            }
        }
    }
}
