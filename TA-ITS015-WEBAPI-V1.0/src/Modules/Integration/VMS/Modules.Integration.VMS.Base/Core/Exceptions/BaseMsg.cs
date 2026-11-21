using Shared.DTO.Constants.Localization;

namespace Modules.Integration.VMS.Base.Core.Exceptions
{
    public class BaseMsg:BaseLocaleManager
    {
        public class IntgVms
        {
            protected class Prefix
            {

                internal const string Entity = "lz.entity.intgVms.";
                internal const string Validation = "lz.validation.intgVms.";
                internal const string Exception = "lz.exception.intgVms.";
                internal const string Message = "lz.message.intgVms.";
            }
            public class Exception
            {

                public const string InvalidParam  = Prefix.Exception + "invalidParam";
                public const string CannotConnect = Prefix.Exception + "cannotConnect";
                public const string VendorNotSupported = Prefix.Exception + "vendorNotSupported";
                public const string VendorDllNotFound = Prefix.Exception + "vendorDllNotFound";
                public const string VendorDllNotRegistered = Prefix.Exception + "vendorDllNotRegistered";
                public const string VendorDllAlreadyRegistered = Prefix.Exception + "vendorDllAlreadyRegistered";
               
                public const string VendorDllRegisteredError = Prefix.Exception + "registerVendorFail";

                //No valid connection type found in the DLL.
                public const string VendorDllNoConnectionType = Prefix.Exception + "vendorDllNoConnectionType";

                public const string InvalidStatMapping = Prefix.Exception + "invalidStatMapping";

            }

            public class Message
            {
                /// <summary>
                /// Module '{0}' registered successfully.
                /// </summary>
                public const string VendorDllRegistered = Prefix.Message + "vendorDllRegistered";

                public const string VendorDllNotRegistered = Prefix.Message + "vendorDllNotRegistered";
            }

        }
    }

}
