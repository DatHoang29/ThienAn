namespace Services.Vds.Catalog;

/// <summary>
/// Author: KienLT
/// Description: Danh mục thiết bị nạp vào RAM (section <c>"Service:Catalog"</c>).
///
/// Chỉ khai phần ĐANG dùng. Bản cũ có thêm <c>CctvModuleId</c> (chọn camera xem được hình cho sự cố)
/// — thêm khi tới phase Events.
/// Created date: 24/08/2026
/// </summary>
public sealed class CatalogOptions
{
    /// <summary>
    /// <c>TmsEquipmentType.ID</c> của camera VDS. Đây là bộ lọc chính khi nạp danh mục — không có nó thì
    /// sẽ nạp cả camera CCTV, biển VMS… vào cache camera VDS.
    /// </summary>
    public string VdsEquipmentTypeId { get; set; } = "";

    /// <summary>
    /// <c>TmsModules.ID</c> của module CCTV — nhóm thiết bị stream được.
    ///
    /// Camera VDS phát hiện sự cố nhưng KHÔNG stream được, nên sự cố phải trỏ sang camera CCTV cùng vùng
    /// để FE mở được hình. Rỗng = bỏ qua map này; sự cố sẽ để trống thiết bị xem được.
    /// </summary>
    public string CctvModuleId { get; set; } = "";

    // Danh sách camera nạp vào service KHÔNG còn khai trong appsettings: nó lấy từ danh mục SysConfig
    // 'vds_list_equipment' (mỗi con lưu một TmsEquipment.ID) — xem CameraManager.LoadAsync.

    /// <summary>
    /// Chu kỳ nạp lại danh mục (giây). <b>0 = tắt</b>, và đó là mặc định.
    ///
    /// Bật khi người vận hành thêm/sửa camera, vùng hoặc đoạn tuyến trên web mà không muốn khởi động lại
    /// service. Nạp lại an toàn: kết quả rỗng hoặc lỗi truy vấn thì GIỮ danh mục đang dùng, không xoá trắng.
    /// </summary>
    public int ReloadSeconds { get; set; }

    public int EffectiveReloadSeconds => Math.Clamp(ReloadSeconds, 30, 86_400);

    public CatalogOptions Clone() => new()
    {
        VdsEquipmentTypeId = VdsEquipmentTypeId,
        CctvModuleId = CctvModuleId,
        ReloadSeconds = ReloadSeconds,
    };
}
