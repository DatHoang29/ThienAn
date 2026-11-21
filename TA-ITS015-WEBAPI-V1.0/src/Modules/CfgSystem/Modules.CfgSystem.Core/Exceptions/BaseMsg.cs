using Shared.DTO.Constants.Localization;

namespace Modules.CfgSystem.Core.Exceptions
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 19/08/2024
    /// </summary>
    public class BaseMsg : BaseLocaleManager
    {
        /// <summary>
        /// Author: ThangNN
        /// Description: CfgSystem Msg
        /// </summary>
        public class CfgSystem
        {
            public class Prefix
            {
                public const string Message = "lz.message.cfgSystem.";
                public const string Validation = "lz.validation.cfgSystem.";
                public const string Exception = "lz.exception.cfgSystem.";
                public const string Entity = "lz.entity.cfgSystem.";
            }

            public class Entity
            {
            }
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: SysConfigType Msg
        /// </summary>
        public class SysConfigType
        {
            public class Prefix
            {
                public const string Message = "lz.message.sysConfigType.";
                public const string Validation = "lz.validation.sysConfigType.";
                public const string Exception = "lz.exception.sysConfigType.";
                public const string Entity = "lz.entity.sysConfigType.";
            }

            public class Entity
            {
            }
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: SysConfigData Msg
        /// </summary>
        public class SysConfigData
        {
            public class Prefix
            {
                public const string Message = "lz.message.sysConfigData.";
                public const string Validation = "lz.validation.sysConfigData.";
                public const string Exception = "lz.exception.sysConfigData.";
                public const string Entity = "lz.entity.sysConfigData.";
            }

            public class Entity
            {
                public const string ConfigTypeId = Prefix.Entity + "configTypeId";
                public const string Value = Prefix.Entity + "value";
                public const string TagType = Prefix.Entity + "tagType";
                public const string StyleSetting = Prefix.Entity + "styleSetting";
                public const string ClassSetting = Prefix.Entity + "classSetting";
            }

            public class Message
            {
                public const string ParentIsRequired = Prefix.Message + "parentIsRequired";
            }
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: SysOpConfig Msg
        /// </summary>
        public class SysOpConfig
        {
            public class Prefix
            {
                public const string Message = "lz.message.sysOpConfig.";
                public const string Validation = "lz.validation.sysOpConfig.";
                public const string Exception = "lz.exception.sysOpConfig.";
                public const string Entity = "lz.entity.sysOpConfig.";
            }

            public class Entity
            {
                public const string GroupCode = Prefix.Entity + "groupCode";
                public const string Value = Prefix.Entity + "value";
                public const string IsSysConfig = Prefix.Entity + "isSysConfig";
            }

            public class Message
            {
                public const string ParentIsRequired = Prefix.Message + "sysConfigDeleteWarning";
            }
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: SysTerminolog Msg
        /// </summary>
        public class SysTerminolog
        {
            public class Prefix
            {
                public const string Message = "lz.message.sysTerminolog.";
                public const string Validation = "lz.validation.sysTerminolog.";
                public const string Exception = "lz.exception.sysTerminolog.";
                public const string Entity = "lz.entity.sysTerminolog.";
            }

            public class Entity
            {
                public const string GroupCode = Prefix.Entity + "groupCode";
                public const string Value = Prefix.Entity + "value";
            }
        }
    }
}
