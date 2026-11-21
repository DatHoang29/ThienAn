using System.Reflection;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Module.VideoWall.Core.Entities;

namespace Module.CCTV.Extensions
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
            /// Description: Thêm validation
            /// Created date: 14/08/2026
            /// </summary>
            internal static IMvcBuilder AddVWValidation(this IMvcBuilder builder)
            {
            return builder.AddFluentValidation(fv =>
            {
                fv.RegisterValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            });
        }
    }
    }