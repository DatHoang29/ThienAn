/*
 * Copyright © Công Ty Cổ Phần Đầu Tư Công Nghệ Thiên An
 * Website: http://tacorp.vn
 */

using Mapster;
using Lab.Domain.Entities;
using Lab.Application.DTOs;

namespace Lab.Application.Mappings
{
    /// <summary>
    /// Description: Lớp cấu hình quy tắc ánh xạ Mapster cho đối tượng Chia Bảng (cls_SplitTestTable -> cls_SplitTestDto)
    /// Author: Antigravity
    /// Create Date: 20/07/2026
    /// </summary>
    public class cls_SplitTestMapRegister : IRegister
    {
        /// <summary>
        /// Description: Phương thức đăng ký quy tắc Mapster cho module SplitTest
        /// Author: Antigravity
        /// Create Date: 20/07/2026
        /// </summary>
        /// <param name="objConfig">Đối tượng TypeAdapterConfig của Mapster</param>
        public void Register(TypeAdapterConfig objConfig)
        {
            objConfig.NewConfig<cls_SplitTestTable, cls_SplitTestDto>()
                .Map(dest => dest.vIdString, src => src.vId.ToString())
                .Map(dest => dest.vName, src => src.vName)
                .Map(dest => dest.vFormattedCreateTime, src => src.vCreateTime.ToString("yyyy-MM-dd HH:mm:ss"));
        }
    }
}
