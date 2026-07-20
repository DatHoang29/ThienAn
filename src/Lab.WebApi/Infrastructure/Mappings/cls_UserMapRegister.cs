/*
 * Copyright © Công Ty Cổ Phần Đầu Tư Công Nghệ Thiên An
 * Website: http://tacorp.vn
 */

using Mapster;
using Lab.WebApi.Entities;
using Lab.WebApi.DTOs;

namespace Lab.WebApi.Infrastructure.Mappings
{
    /// <summary>
    /// Description: Lớp cấu hình quy tắc ánh xạ Mapster chuyên biệt cho đối tượng Người dùng (cls_User -> cls_UserDto)
    /// Author: Antigravity
    /// Create Date: 20/07/2026
    /// </summary>
    public class cls_UserMapRegister : IRegister
    {
        /// <summary>
        /// Description: Phương thức đăng ký quy tắc Mapster cho module User
        /// Author: Antigravity
        /// Create Date: 20/07/2026
        /// </summary>
        /// <param name="objConfig">Đối tượng TypeAdapterConfig của Mapster</param>
        public void Register(TypeAdapterConfig objConfig)
        {
            // Cấu hình quy tắc Map từ Entity cls_User sang DTO cls_UserDto
            objConfig.NewConfig<cls_User, cls_UserDto>()
                .Map(dest => dest.vId, src => src.intId)
                .Map(dest => dest.vUserName, src => src.strUserName)
                .Map(dest => dest.vEmail, src => src.strEmail)
                .Map(dest => dest.vFormattedCreateDate, src => src.dtCreateDate.HasValue ? src.dtCreateDate.Value.ToString("dd/MM/yyyy HH:mm:ss") : "Chưa xác định");
        }
    }
}
