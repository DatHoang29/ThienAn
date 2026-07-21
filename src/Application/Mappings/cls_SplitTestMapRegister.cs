/*
 * Copyright © Công Ty Cổ Phần Đầu Tư Công Nghệ Thiên An
 * Website: http://tacorp.vn
 */

namespace Application.Mappings
{
    /// <summary>
    /// Description: Khai báo các quy tắc ánh xạ Mapster cho thực thể kiểm thử chia bảng
    /// Author: Antigravity
    /// Create Date: 20/07/2026
    /// </summary>
    public class cls_SplitTestMapRegister : IRegister
    {
        /// <summary>
        /// Description: Định nghĩa quy tắc mapping giữa cls_SplitTestTable và cls_SplitTestDto
        /// </summary>
        /// <param name="config">Đối tượng cấu hình TypeAdapterConfig của Mapster</param>
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<cls_SplitTestTable, cls_SplitTestDto>()
                .Map(dest => dest.vIdString, src => src.vId.ToString())
                .Map(dest => dest.vName, src => src.vName)
                .Map(dest => dest.vFormattedCreateTime, src => src.vCreateTime.ToString("dd/MM/yyyy HH:mm:ss"));
        }
    }
}
