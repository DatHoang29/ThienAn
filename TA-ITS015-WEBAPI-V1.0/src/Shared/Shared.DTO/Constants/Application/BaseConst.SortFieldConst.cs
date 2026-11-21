namespace Shared.DTO.Constants.Application;

public static partial class BaseConst
{
    /// <summary>
    /// Tên trường dùng làm mặc định khi sắp xếp danh sách phân trang (tham số defaultSortField/defaultSortFields của OrderBuilder).
    /// </summary>
    public static class SortFieldConst
    {
        /// <summary>
        /// Sắp xếp theo thời gian tạo (EntityBase.CreateTime).
        /// </summary>
        public const string CreateTime = "CreateTime";

        /// <summary>
        /// Sắp xếp theo thời gian cập nhật gần nhất (EntityBase.UpdateTime).
        /// Lưu ý: UpdateTime chỉ được gán khi cập nhật, bản ghi mới tạo chưa sửa lần nào sẽ có giá trị NULL.
        /// </summary>
        public const string UpdateTime = "UpdateTime";

        /// <summary>
        /// Sắp xếp theo thời gian thay đổi gần nhất: lấy UpdateTime, nếu NULL thì lấy CreateTime.
        /// Truyền vào overload OrderBuilder nhận nhiều trường, tương đương COALESCE(UpdateTime, CreateTime).
        /// </summary>
        public static readonly System.Collections.Generic.IReadOnlyList<string> UpdateOrCreateTime = new[] { UpdateTime, CreateTime };
    }
}
