using Microsoft.Extensions.DependencyInjection;

namespace Modules.WP.Extensions
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Extension đăng ký FluentValidation cho MvcBuilder của module WP (WebPortal)
    /// Created date: 03/07/2026
    /// </summary>
    internal static class MvcBuilderExtensions
    {
        /// <summary>
        /// Author: HieuNV
        /// Description: Thêm validation
        /// Created date: 14/08/2026
        /// </summary>
        internal static IMvcBuilder AddWPValidation(this IMvcBuilder builder)
        {
            return builder;
        }
    }
}