/*
 * Copyright © Công Ty Cổ Phần Đầu Tư Công Nghệ Thiên An
 * Website: http://tacorp.vn
 */

using Domain.Entities;

namespace Domain.Repositories
{
    /// <summary>
    /// Description: Interface thao tác cơ sở dữ liệu phân chia (SplitTable Repository Contract)
    /// Author: Antigravity
    /// Create Date: 20/07/2026
    /// </summary>
    public interface Icls_SplitTestRepository
    {
        /// <summary>
        /// Description: Thêm bản ghi mới vào bảng phân chia và tự động trả về Snowflake ID
        /// </summary>
        /// <param name="objRecord">Thực thể bản ghi</param>
        /// <returns>Snowflake ID tự sinh</returns>
        long fn_InsertSplitData(cls_SplitTestTable objRecord);

        /// <summary>
        /// Description: Lấy danh sách dữ liệu hợp nhất từ tất cả các bảng phân chia
        /// </summary>
        /// <returns>Danh sách thực thể cls_SplitTestTable</returns>
        List<cls_SplitTestTable> fn_GetAllSplitData();

        /// <summary>
        /// Description: Lấy dữ liệu phân chia theo khoảng thời gian chỉ định
        /// </summary>
        /// <param name="dtBegin">Thời gian bắt đầu</param>
        /// <param name="dtEnd">Thời gian kết thúc</param>
        /// <returns>Danh sách thực thể cls_SplitTestTable</returns>
        List<cls_SplitTestTable> fn_GetByTimeRange(DateTime dtBegin, DateTime dtEnd);

        /// <summary>
        /// Description: Lấy danh sách các tên bảng thực tế đã sinh ra trong DB
        /// </summary>
        /// <returns>Danh sách tên bảng</returns>
        List<string> fn_GetCreatedTables();
    }
}
