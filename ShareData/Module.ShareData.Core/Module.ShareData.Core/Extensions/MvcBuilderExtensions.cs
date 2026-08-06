using Microsoft.Extensions.DependencyInjection;

namespace Module.ShareData.Core.Extensions
{
    internal static class MvcBuilderExtensions
    {
        internal static IMvcBuilder AddShareDataCoreValidation(this IMvcBuilder builder)
        {
            return builder;
        }
    }
}
