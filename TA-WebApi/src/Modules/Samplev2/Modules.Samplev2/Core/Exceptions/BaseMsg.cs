using Shared.DTO.Constants.Localization;

namespace Modules.Samplev2.Core.Exceptions
{
    public class BaseMsg : BaseLocaleManager
    {
        public static class SampCategory
        {
            /// <summary>
            /// Generate validation messages
            /// </summary>
            public static class Validation
            {
                public const string MustContainPrefix = "be.sampCategory.validation.mustContainPrefix";
            }

            /// <summary>
            /// Generate information messages
            /// </summary>
            public static class Message
            {
                public const string TestMessage = "be.sampCategory.message.testMessage";
            }

            /// <summary>
            /// Generate exception messages 
            /// </summary>
            public static class Exception
            {
                /// <summary>
                /// Cannot be deleted as it has associated product
                /// </summary>
                public const string CannotDeleteHasEnable = "be.sampCategory.exception.cannotDeleteHasEnable";
            }

            /// <summary>
            /// Generate entity messages
            /// </summary>
            public static class Entity
            {
                public const string Name = "entity.sampCategory.name";
            }

        }

    }

}
