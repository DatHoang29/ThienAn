using FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Modules.TOLL.Extensions
{
    internal static class MvcBuilderExtensions
    {
        internal static IMvcBuilder AddTollValidation(this IMvcBuilder builder)
        {
            return builder.AddFluentValidation(fv =>
            {
                fv.RegisterValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            });
        }
    }
}
