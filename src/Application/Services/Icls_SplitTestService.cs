/*
 * Copyright © Công Ty Cổ Phần Đầu Tư Công Nghệ Thiên An
 * Website: http://tacorp.vn
 */

using Application.DTOs;
using Domain.Entities;

namespace Application.Services
{
    /// <summary>
    /// Description: Service Interface xử lý các Use Case nghiệp vụ chia bảng (SplitTable Service Contract)
    /// Author: Antigravity
    /// Create Date: 20/07/2026
    /// </summary>
    public interface Icls_SplitTestService
    {
        /// <summary>
        /// Description: Thêm bản ghi mới vào bảng chia theo ngày
        /// </summary>
        /// <param name="strName">Tên dữ liệu mẫu</param>
        /// <returns>Đối tượng phản hồi DTO và tên bảng đã tạo</returns>
        (cls_SplitTestDto objDto, string strTableName) fn_InsertSplitData(string strName);

        /// <summary>
        /// Description: Lấy toàn bộ danh sách dữ liệu hợp nhất từ các bảng phân chia
        /// </summary>
        /// <returns>Danh sách DTO</returns>
        List<cls_SplitTestDto> fn_GetAllSplitData();

        /// <summary>
        /// Description: Lấy danh sách dữ liệu phân chia theo khoảng thời gian
        /// </summary>
        /// <param name="dtBegin">Thời gian bắt đầu</param>
        /// <param name="dtEnd">Thời gian kết thúc</param>
        /// <returns>Danh sách DTO</returns>
        List<cls_SplitTestDto> fn_GetByTimeRange(DateTime dtBegin, DateTime dtEnd);

        /// <summary>
        /// Description: Lấy danh sách tên các bảng phân chia đã sinh ra trong DB
        /// </summary>
        /// <returns>Danh sách tên bảng</returns>
        List<string> fn_GetCreatedTables();
    }
}
