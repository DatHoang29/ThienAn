using FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Modules.Report.Extensions
{
    internal static class MvcBuilderExtensions
    {
        internal static IMvcBuilder AddReportValidation(this IMvcBuilder builder)
        {
            return builder.AddFluentValidation(fv =>
            {
                fv.RegisterValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            });
        }
    }
}
