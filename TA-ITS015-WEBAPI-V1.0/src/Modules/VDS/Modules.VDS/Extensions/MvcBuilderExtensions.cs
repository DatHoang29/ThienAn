using FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Modules.VDS.Extensions
{
    internal static class MvcBuilderExtensions
    {
        internal static IMvcBuilder AddVDSValidation(this IMvcBuilder builder)
        {

            return builder.AddFluentValidation(fv =>
            {
                //fv.DisableDataAnnotationsValidation = true;
                //fv.AutomaticValidationEnabled = true;
                //fv.ImplicitlyValidateChildProperties = false;
                fv.RegisterValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            });
        }
    }
}