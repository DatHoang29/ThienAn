namespace Shared.Utility.Endpoints.System
{
    /// <summary>
    /// System module endpoint class
    /// </summary>
    public class SystemEndpoint
    {
        /// <summary>
        /// System Auth endpoint
        /// </summary>
        public class Auth
        {
            public const string Base = "/api/system/sysauth";
        }

        /// <summary>
        /// System User endpoint
        /// </summary>
        public class User
        {
            public const string Base = "/api/system/sysuser";
        }

        /// <summary>
        /// System Role endpoint
        /// </summary>
        public class Role
        {
            public const string Base = "/api/system/sysrole";
        };

        /// <summary>
        /// System Menu endpoint
        /// </summary>
        public class Menu
        {
            public const string Base = "/api/system/sysmenu";
        };

        /// <summary>
        /// System Config endpoint
        /// </summary>
        public class Config
        {
            public const string Base = "/api/system/sysconfig";
        };

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Base api url SysOrg
        /// </summary>
        public class Org
        {
            public const string Base = "/api/system/sysorg";
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Base api url SysConfigType
        /// </summary>
        public class ConfigType
        {
            public const string Base = "/api/cfgsystem/sysconfigtype";
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Base api url SysConfigData
        /// </summary>
        public class ConfigData
        {
            public const string Base = "/api/cfgsystem/sysconfigdata";
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Base api url SysOpConfig
        /// </summary>
        public class OpConfig
        {
            public const string Base = "/api/cfgsystem/sysopconfig";
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Base api url SysTerminology
        /// </summary>
        public class Terminology
        {
            public const string Base = "/api/cfgsystem/systerminology";
        }
    }
}
