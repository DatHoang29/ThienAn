using Microsoft.Extensions.DependencyInjection;

namespace Modules.TMS.Core.Extensions
{
    internal static class MvcBuilderExtensions
    {
        internal static IMvcBuilder AddTMSCoreValidation(this IMvcBuilder builder)
        {
            return builder;
            //return builder.AddFluentValidation(fv =>
            //{
            //    fv.RegisterValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            //});
        }
    }
}