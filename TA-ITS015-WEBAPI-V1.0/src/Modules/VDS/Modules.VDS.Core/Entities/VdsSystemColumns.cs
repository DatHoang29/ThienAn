using Shared.Core.Domain;

namespace Modules.VDS.Core.Entities;

/// <summary>
/// Author: KienLT
/// Description: Giữ các cột HỆ THỐNG của <see cref="EntityTenant"/> khỏi tay client khi input là entity.
///
/// <para>Từ 23/09/2026 các input thêm/sửa của VDS kế thừa thẳng entity và command handler <c>Adapt</c> nguyên
/// — đúng mẫu <c>SysOpConfig</c> của CfgSystem. Hệ quả là body request cũng mang theo <c>ID</c>,
/// <c>TenantId</c>, <c>IsDelete</c>, <c>RowStatus</c> và bốn cột audit, và Mapster ghi đè tất cả (mặc định
/// KHÔNG bỏ qua null). Hai chỗ đó phải chặn, nhưng chặn ở đây chứ không phải bằng cách cắt field khỏi DTO:</para>
/// <list type="bullet">
/// <item><b>Thêm:</b> xoá sạch để tầng persistence tự sinh <c>ID</c>, gắn tenant và audit. Nếu không, client
/// tự chỉ định được <c>ID</c> và <c>TenantId</c> — tức là ghi được sang tenant khác.</item>
/// <item><b>Sửa:</b> khôi phục từ bản ghi đang có trong DB. Nếu không, một body thiếu các cột này sẽ xoá
/// <c>TenantId</c>/<c>CreateTime</c>/<c>CreateUId</c> của bản ghi, và một body cố ý gửi
/// <c>isDelete: true</c> sẽ xoá mềm bản ghi qua đường sửa.</item>
/// </list>
///
/// <para>Đây KHÔNG phải danh sách trắng field nghiệp vụ: mọi cột nghiệp vụ vẫn đi qua <c>Adapt</c> nguyên
/// vẹn. Chỗ này chỉ nói "cột hệ thống do hệ thống quyết".</para>
/// Created date: 23/09/2026
/// </summary>
public static class VdsSystemColumns
{
    /// <summary>Xoá cột hệ thống trước khi INSERT, để tầng persistence tự điền.</summary>
    public static T Reset<T>(T row) where T : EntityTenant
    {
        row.ID = "";
        row.TenantId = default!;
        row.CreateTime = default;
        row.CreateUId = default!;
        row.UpdateTime = null;
        row.UpdateUId = default!;
        row.RowStatus = default!;
        row.IsDelete = null;
        return row;
    }

    /// <summary>Khôi phục cột hệ thống từ bản ghi đang lưu, sau khi đã map dữ liệu người dùng vào.</summary>
    /// <param name="row">Entity sắp UPDATE (đã bị <c>Adapt</c> ghi đè).</param>
    /// <param name="stored">Ảnh chụp các cột hệ thống đọc từ DB trước khi map.</param>
    public static T KeepFrom<T>(T row, VdsStoredColumns stored) where T : EntityTenant
    {
        row.ID = stored.ID;
        row.TenantId = stored.TenantId;
        row.CreateTime = stored.CreateTime;
        row.CreateUId = stored.CreateUId;
        row.RowStatus = stored.RowStatus;
        row.IsDelete = stored.IsDelete;
        return row;
    }

    /// <summary>Chụp cột hệ thống của bản ghi đọc từ DB, trước khi <c>Adapt</c> ghi đè.</summary>
    public static VdsStoredColumns Snapshot(EntityTenant row) => new()
    {
        ID = row.ID,
        TenantId = row.TenantId,
        CreateTime = row.CreateTime,
        CreateUId = row.CreateUId,
        RowStatus = row.RowStatus,
        IsDelete = row.IsDelete,
    };
}
