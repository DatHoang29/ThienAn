using Microsoft.Extensions.DependencyInjection;

namespace Module.VideoWall.Core.Extensions
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Các hàm mở rộng cho Mvc Builder
    /// Created date: 14/08/2026
    /// </summary>
    internal static class MvcBuilderExtensions
    {
        /// <summary>
        /// Author: HieuNV
        /// Description: Thêm core validation
        /// Created date: 14/08/2026
        /// </summary>
        internal static IMvcBuilder AddVWCoreValidation(this IMvcBuilder builder)
        {
            return builder;
            //return builder.AddFluentValidation(fv =>
            //{
            //    fv.RegisterValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            //});
        }
    }
}