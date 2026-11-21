using Services.Shared.Persistence;
using Services.Vds.Shared.Persistence;
using Services.Vds.Configuration;
using Shared.Utility.Utilities;

namespace Services.Vds.Features.ZoneStatus;

/// <summary>
/// Author: KienLT
/// Description: Bảng trạng thái vùng (<c>TmsZoneStatus</c>) trên SQL Server — <b>thuộc bên TMS, VDS dùng
/// NGUYÊN TRẠNG</b>: không đổi lược đồ, không thêm ràng buộc, không xoá dòng, và chỉ ghi ba cột nêu ở
/// <see cref="UpsertStatusAsync"/>.
///
/// Đó là lý do repository này tách riêng khỏi các bảng VDS tự quản: bảng KHÔNG có UNIQUE trên vùng nên
/// không dùng được lối "ghi trước, bắt lỗi trùng" như bảng sự cố.
/// Created date: 26/08/2026
/// </summary>
public sealed class ZoneStatusRepository : VdsRepositoryBase
{
    public ZoneStatusRepository(DatabaseManager databaseManager, IVdsConfigManager config)
        : base(databaseManager, config)
    {
    }

    /// <summary>
    /// Cập nhật trạng thái một vùng. Chỉ ghi tốc độ trung bình, tình trạng và mốc cập nhật.
    ///
    /// <para>
    /// Nhóm cột <c>DetectWay/DetectStop/DetectSlow/DetectOthers</c> thuộc trục khác của hệ cũ nên GIỮ NGUYÊN
    /// giá trị đang có — ghi đè chúng là xoá dữ liệu của tính năng khác.
    /// </para>
    ///
    /// <para>
    /// <b>UPDATE TRƯỚC, chỉ INSERT khi vùng chưa có dòng nào.</b> Và KHÔNG lọc theo tenant ở điều kiện
    /// update: mục đích là cập nhật đúng dòng FE đang đọc, không phải tạo thêm một dòng song song theo
    /// tenant (cùng cách danh mục camera đang đọc). Vùng có nhiều dòng thì cập nhật hết — kết quả vẫn nhất
    /// quán.
    /// </para>
    /// </summary>
    /// <returns>Số dòng đã cập nhật, hoặc 1 nếu vừa thêm dòng mới cho vùng chưa có.</returns>
    public async Task<int> UpsertStatusAsync(
        string zoneId,
        string? averageSpeed,
        string condition,
        DateTime updateTime,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var updated = await Client.Updateable<TmsZoneStatus>()
            .SetColumns(x => x.AverageSpeed == averageSpeed)
            .SetColumns(x => x.Condition == condition)
            .SetColumns(x => x.UpdateTime == updateTime)
            .Where(x => x.IsDelete == null && x.ZoneId == zoneId)
            .ExecuteCommandAsync();

        if (updated > 0) return updated;

        // Vùng chưa có dòng trạng thái: FE join vùng ↔ trạng thái nên thiếu dòng là vùng bị loại khỏi kết
        // quả. Thêm một dòng là thao tác DỮ LIỆU thông thường, không phải đổi cấu trúc bảng.
        var entity = new TmsZoneStatus
        {
            ID = Guid.NewGuid().ToString(),
            ZoneId = zoneId,
            AverageSpeed = averageSpeed,
            Condition = condition,
            UpdateTime = updateTime,
            TenantId = TenantId,
            CreateTime = BaseUtil.TimeServer.GetTimeServer(),
            CreateUId = ServiceUserId,
        };

        await Client.Insertable(entity).ExecuteCommandAsync();
        return 1;
    }
}
