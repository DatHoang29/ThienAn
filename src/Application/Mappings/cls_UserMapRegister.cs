/*
 * Copyright © Công Ty Cổ Phần Đầu Tư Công Nghệ Thiên An
 * Website: http://tacorp.vn
 */

namespace Application.Mappings
{
    /// <summary>
    /// Description: Khai báo các quy tắc ánh xạ Mapster cho thực thể Người dùng
    /// Author: Antigravity
    /// Create Date: 20/07/2026
    /// </summary>
    public class cls_UserMapRegister : IRegister
    {
        /// <summary>
        /// Description: Định nghĩa quy tắc mapping giữa cls_User và cls_UserDto
        /// </summary>
        /// <param name="config">Đối tượng cấu hình TypeAdapterConfig của Mapster</param>
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<cls_User, cls_UserDto>()
                .Map(dest => dest.vId, src => src.intId)
                .Map(dest => dest.vUserName, src => src.strUserName)
                .Map(dest => dest.vEmail, src => src.strEmail)
                .Map(dest => dest.vFormattedCreateDate, src => src.dtCreateDate.HasValue ? src.dtCreateDate.Value.ToString("dd/MM/yyyy HH:mm:ss") : string.Empty);
        }
    }
}
