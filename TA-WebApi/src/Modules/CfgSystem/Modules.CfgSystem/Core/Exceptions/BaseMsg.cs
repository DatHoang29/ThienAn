using Shared.DTO.Constants.Localization;

namespace Modules.CfgSystem.Core.Exceptions
{
    /// <summary>
    /// Author: Ngọc Thắng
    /// Created date: 19/08/2024
    /// </summary>
    internal class BaseMsg : BaseLocaleManager
    {
        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: CfgSystem Msg
        /// </summary>
        internal class CfgSystem
        {
            internal class Prefix
            {
                internal const string Message = "lz.message.cfgSystem.";
                internal const string Validation = "lz.validation.cfgSystem.";
                internal const string Exception = "lz.exception.cfgSystem.";
                internal const string Entity = "lz.entity.cfgSystem.";
            }

            internal class Entity
            {
            }
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: SysConfigType Msg
        /// </summary>
        internal class SysConfigType
        {
            internal class Prefix
            {
                internal const string Message = "lz.message.sysConfigType.";
                internal const string Validation = "lz.validation.sysConfigType.";
                internal const string Exception = "lz.exception.sysConfigType.";
                internal const string Entity = "lz.entity.sysConfigType.";
            }

            internal class Entity
            {
            }
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: SysConfigData Msg
        /// </summary>
        internal class SysConfigData
        {
            internal class Prefix
            {
                internal const string Message = "lz.message.sysConfigData.";
                internal const string Validation = "lz.validation.sysConfigData.";
                internal const string Exception = "lz.exception.sysConfigData.";
                internal const string Entity = "lz.entity.sysConfigData.";
            }

            internal class Entity
            {
                internal const string ConfigTypeId = Prefix.Entity + "configTypeId";
                internal const string Value = Prefix.Entity + "value";
                internal const string TagType = Prefix.Entity + "tagType";
                internal const string StyleSetting = Prefix.Entity + "styleSetting";
                internal const string ClassSetting = Prefix.Entity + "classSetting";
            }

            internal class Message
            {
                internal const string ParentIsRequired = Prefix.Message + "parentIsRequired";
            }
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: SysOpConfig Msg
        /// </summary>
        internal class SysOpConfig
        {
            internal class Prefix
            {
                internal const string Message = "lz.message.sysOpConfig.";
                internal const string Validation = "lz.validation.sysOpConfig.";
                internal const string Exception = "lz.exception.sysOpConfig.";
                internal const string Entity = "lz.entity.sysOpConfig.";
            }

            internal class Entity
            {
                internal const string GroupCode = Prefix.Entity + "groupCode";
                internal const string Value = Prefix.Entity + "value";
                internal const string IsSysConfig = Prefix.Entity + "isSysConfig";
            }

            internal class Message
            {
                internal const string ParentIsRequired = Prefix.Message + "sysConfigDeleteWarning";
            }
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: SysTerminolog Msg
        /// </summary>
        internal class SysTerminolog
        {
            internal class Prefix
            {
                internal const string Message = "lz.message.sysTerminolog.";
                internal const string Validation = "lz.validation.sysTerminolog.";
                internal const string Exception = "lz.exception.sysTerminolog.";
                internal const string Entity = "lz.entity.sysTerminolog.";
            }

            internal class Entity
            {
                internal const string GroupCode = Prefix.Entity + "groupCode";
                internal const string Value = Prefix.Entity + "value";
            }
        }
    }
}
