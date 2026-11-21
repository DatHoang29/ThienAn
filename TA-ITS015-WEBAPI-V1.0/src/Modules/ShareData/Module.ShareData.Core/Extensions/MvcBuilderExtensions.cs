using Microsoft.Extensions.DependencyInjection;

namespace Module.ShareData.Core.Extensions
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
        internal static IMvcBuilder AddShareDataCoreValidation(this IMvcBuilder builder)
        {
            return builder;
        }
    }
}
