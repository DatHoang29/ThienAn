using Microsoft.Extensions.DependencyInjection;

namespace Modules.Samplev2.Extensions
{
    internal static class MvcBuilderExtensions
    {
        internal static IMvcBuilder AddSampleValidation(this IMvcBuilder builder)
        {
            return builder;
        }
    }
}