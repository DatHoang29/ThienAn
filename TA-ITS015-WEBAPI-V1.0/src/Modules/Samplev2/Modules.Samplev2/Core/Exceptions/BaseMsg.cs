using Shared.DTO.Constants.Localization;

namespace Modules.Samplev2.Core.Exceptions
{
    internal class BaseMsg:BaseLocaleManager
    {
        internal class SampCategory
        {

            protected class Prefix
            {
                internal const string Exception = "lz.exception.sampCategory.";
                internal const string Validation = "lz.validation.sampCategory.";
                internal const string Message = "lz.message.sampCategory.";
                internal const string Entity = "lz.entity.sampCategory.";
            }

            /// <summary>
            /// Generate validation messages
            /// </summary>
            internal class Validation
            {
                internal const string MustContainPrefix = Prefix.Validation + "mustContainPrefix";
            }

            /// <summary>
            /// Generate information messages
            /// </summary>
            internal class Message
            {
                internal const string TestMessage = Prefix.Validation + "testMessage";
            }

            /// <summary>
            /// Generate exception messages 
            /// </summary>
            internal class Exception
            {
                /// <summary>
                /// Cannot be deleted as it has associated product
                /// </summary>
                internal const string CannotDeleteHasEnable =  Prefix.Exception + "cannotDeleteHasEnable";
            }

            /// <summary>
            /// Generate entity messages
            /// </summary>
            internal class Entity
            {
                internal const string Name = Prefix.Entity + "name";
                internal const string Description = Prefix.Entity + "description";

            }

        }

    }

}
