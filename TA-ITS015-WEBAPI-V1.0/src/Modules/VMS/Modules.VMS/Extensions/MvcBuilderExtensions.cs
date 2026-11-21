using System.Reflection;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.VMS.Extensions
{
    internal static class MvcBuilderExtensions
    {
        internal static IMvcBuilder AddVMSValidation(this IMvcBuilder builder)
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