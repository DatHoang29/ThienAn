using System.Reflection;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Modules.CCTV.Core.Entities; // ← dùng entity làm anchor

namespace Module.CCTV.Extensions
{
    public static class MvcBuilderExtensions
    {
        public static IMvcBuilder AddCctvValidation(this IMvcBuilder builder)
        {
            return builder.AddFluentValidation(fv =>
            {
                // Dùng typeof(CctvDashboard) thay vì GetExecutingAssembly()
                // → đảm bảo đúng assembly chứa entity CCTV
                fv.RegisterValidatorsFromAssembly(typeof(CctvDashboard).Assembly);

            });
        }
    }
}