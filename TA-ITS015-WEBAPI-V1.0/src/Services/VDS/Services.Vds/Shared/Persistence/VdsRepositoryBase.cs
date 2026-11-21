using Services.Shared.Persistence;
using Services.Vds.Configuration;

namespace Services.Vds.Shared.Persistence;

/// <summary>
/// Author: KienLT
/// Description: Base của repository đọc/ghi SQL SERVER.
///
/// Song song với <see cref="QuestRepositoryBase"/>, khác nhau đúng MỘT điểm: lấy connection theo
/// <c>Service:DataConnectionId</c> thay vì <c>Service:QuestConnectionId</c>. Nhờ
/// <see cref="DatabaseManager"/> quản nhiều kết nối theo <c>ConfigId</c> nên service dùng đồng thời hai
/// CSDL mà không phải tự tạo <c>SqlSugarClient</c> ở đâu cả.
///
/// SQL Server giữ: danh mục (<c>TmsEquipment</c>, <c>TmsZone*</c>, <c>SysOpConfig</c>) và các bảng có
/// vòng đời trạng thái (<c>TmsIncident</c>, <c>TmsZoneStatus</c>) — những bảng cần UPDATE từng dòng nên
/// không chuyển sang QuestDB được.
/// Created date: 24/08/2026
/// </summary>
public abstract class VdsRepositoryBase : SqlSugarAccess
{
    private readonly IVdsConfigManager _config;

    protected VdsRepositoryBase(DatabaseManager databaseManager, IVdsConfigManager config)
        : base(databaseManager, () => config.Current.DataConnectionId)
    {
        _config = config;
    }

    protected string TenantId => _config.Current.TenantId;

    protected string ServiceUserId => _config.Current.ServiceUserId;
}
