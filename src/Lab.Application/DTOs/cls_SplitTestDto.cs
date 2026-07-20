/*
 * Copyright © Công Ty Cổ Phần Đầu Tư Công Nghệ Thiên An
 * Website: http://tacorp.vn
 */

namespace Lab.Application.DTOs
{
    /// <summary>
    /// Description: DTO chứa dữ liệu phản hồi của đối tượng chia bảng SplitTestTable
    /// Author: Antigravity
    /// Create Date: 20/07/2026
    /// </summary>
    public class cls_SplitTestDto
    {
        /// <summary>
        /// Description: Định danh Snowflake ID
        /// </summary>
        public string vIdString { get; set; } = null!;

        /// <summary>
        /// Description: Tên dữ liệu mẫu
        /// </summary>
        public string? vName { get; set; }

        /// <summary>
        /// Description: Chuỗi ngày tạo định dạng hiển thị
        /// </summary>
        public string? vFormattedCreateTime { get; set; }
    }
}
