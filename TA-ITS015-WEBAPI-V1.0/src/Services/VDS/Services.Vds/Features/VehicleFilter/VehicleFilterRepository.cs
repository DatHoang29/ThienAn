using Services.Shared.Persistence;
using Services.Vds.Shared.Persistence;
using Services.Vds.Configuration;
using Shared.Utility.Utilities;

namespace Services.Vds.Features.VehicleFilter;

/// <summary>
/// Author: KienLT
/// Description: Hai bảng của tính năng cảnh báo biển số, trên SQL Server:
/// <list type="bullet">
/// <item><c>TmsVehicleFilter</c> — danh sách đen/trắng do người vận hành khai trên web, service chỉ ĐỌC.</item>
/// <item><c>TmsVehicleFilterHistory</c> — lượt xe khớp danh sách, service GHI.</item>
/// </list>
///
/// <para>
/// <b>Vì sao đọc/ghi DB trực tiếp thay vì gọi API của WebAPI</b>: hai handler tương ứng nằm trong module
/// TMS của WebAPI mà service này cố tình không tham chiếu (chỉ mang project entity), còn gọi qua HTTP thì
/// WebAPI chặn xác thực toàn bộ đường dẫn API mà service nền không có token người dùng. Truy vấn ở đây giữ
/// ĐÚNG ngữ nghĩa của handler tương ứng nên dữ liệu hai bên nhìn thấy là một.
/// </para>
///
/// <para>
/// <b>KHÁC BIỆT CÓ CHỦ Ý với handler thêm lịch sử của web</b>: handler đó chặn trùng theo biển số (mỗi biển
/// chỉ một dòng lịch sử). Với bảng LỊCH SỬ thì đó là hành vi sai — mỗi lượt xe đi qua phải là một dòng — nên
/// repository này chèn mọi lượt khớp. Chống dội thông báo do đó KHÔNG nằm ở DB mà ở cửa sổ trong RAM.
/// </para>
/// Created date: 26/08/2026
/// </summary>
public sealed class VehicleFilterRepository : VdsRepositoryBase
{
    public VehicleFilterRepository(DatabaseManager databaseManager, IVdsConfigManager config)
        : base(databaseManager, config)
    {
    }

    /// <summary>
    /// Toàn bộ danh sách chưa xoá mềm.
    ///
    /// Bộ lọc theo trạng thái bật/tắt và khoảng hiệu lực KHÔNG làm ở đây mà làm lúc đối chiếu: danh sách nằm
    /// trong RAM cả ngày nên hiệu lực phải xét theo THỜI ĐIỂM XE ĐI QUA, không theo thời điểm nạp.
    ///
    /// KHÔNG lọc theo tenant: bảng do web quản, phải nhìn đúng dòng người vận hành vừa khai — cùng quy ước
    /// với danh mục camera và bảng trạng thái vùng.
    /// </summary>
    public Task<List<TmsVehicleFilter>> QueryFiltersAsync(CancellationToken ct = default)
        => Client.Queryable<TmsVehicleFilter>()
            .Where(x => x.IsDelete == null)
            .OrderBy(x => x.CreateTime)
            .ToListAsync();

    /// <summary>
    /// Ghi một lượt xe khớp danh sách. Trả về id dòng vừa ghi (giao diện dùng để mở đúng bản ghi từ thông
    /// báo). KHÔNG luỹ đẳng theo biển số — xem ghi chú của class.
    /// </summary>
    public async Task<string> InsertHistoryAsync(
        TmsVehicleFilterHistory entity, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrEmpty(entity.ID)) entity.ID = Guid.NewGuid().ToString();
        entity.TenantId = TenantId;
        entity.CreateTime = BaseUtil.TimeServer.GetTimeServer();
        entity.CreateUId = ServiceUserId;

        // KHÔNG gán IsDelete: cột đó là mốc thời gian xoá mềm, null nghĩa là chưa xoá.

        await Client.Insertable(entity).ExecuteCommandAsync();
        return entity.ID;
    }

    /// <summary>
    /// Chuyến gần nhất của một biển kể từ <paramref name="sinceUtc"/> — nạp lại trạng thái chuyến sau khi service
    /// khởi động lại, để xe đang đi dở không bị mở chuyến mới (thông báo lặp). Không có ⇒ null.
    ///
    /// So biển theo dạng chuẩn hoá (bỏ khoảng trắng, '.', '-', in hoa) — cùng luật <c>PlateRules.Normalize</c>,
    /// vì lịch sử lưu biển NGUYÊN VĂN của lượt xe.
    /// </summary>
    public async Task<PlateTripState?> QueryLastTripAsync(
        string normalizedPlate, DateTime sinceUtc, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var row = await Client.Queryable<TmsVehicleFilterHistory>()
            .Where(x => x.IsDelete == null && x.TripId != null && x.DetectTime >= sinceUtc)
            .Where(x => x.LicensePlate!.Replace("-", "").Replace(".", "").Replace(" ", "").ToUpper() == normalizedPlate)
            .OrderBy(x => x.DetectTime, OrderByType.Desc)
            .Select(x => new { x.TripId, x.DetectTime, x.EquipmentId })
            .FirstAsync();

        return row?.TripId is null || row.DetectTime is null
            ? null
            : new PlateTripState(row.TripId, row.DetectTime.Value, row.EquipmentId);
    }
}
