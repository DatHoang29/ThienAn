using Services.Vds.Shared.Domain;
using Services.Vds.Shared.Messaging;
using Services.Vds.Catalog;
using Services.Vds.Features.Counting;
using Services.Vds.Features.Events;
using Services.Vds.Features.Events.Automation;
using Services.Vds.Pipeline;
using Services.Vds.Pipeline.Quarantine;
using Services.Vds.Sources.Backfill;
using Services.Vds.Scheduling;
using Services.Vds.Features.Journey;
using Services.Vds.Features.Media;
using Services.Vds.Features.Statistic;
using Services.Vds.Features.Summary;
using Services.Vds.Features.VehicleFilter;
using Services.Vds.Features.ZoneStatus;
using Services.Vds.Pipeline.Mapping;

namespace Services.Vds.Configuration;

/// <summary>
/// Author: KienLT
/// Description: Cấu hình runtime của service VDS — bind section <c>"Service"</c> trong appsettings.json.
///
/// <b>Snapshot BẤT BIẾN VỀ MẶT SỬ DỤNG.</b> <see cref="VdsConfigManager"/> không sửa object này tại chỗ;
/// muốn đổi một tham số thì <see cref="Clone"/> ra bản sao, sửa trên bản sao, rồi thay tham chiếu.
/// Nhờ vậy luồng đang đọc <c>Current</c> không bao giờ thấy trạng thái nửa vời và không cần lock.
///
/// CỐ TÌNH GIỮ NHỎ: bản cũ (<c>ITS.VDS.Core</c>) có ~15 class options con vì đã chạy đủ tính năng.
/// Ở đây mỗi phase chỉ thêm đúng phần mình cần.
///
/// Phải có setter (không dùng <c>record</c> hay <c>init</c>) vì <c>IConfiguration.Get&lt;T&gt;()</c>
/// cần setter để bind.
/// Created date: 24/08/2026
/// </summary>
public sealed class ServiceConfig
{
    /// <summary>Tên section trong appsettings.json.</summary>
    public const string SectionName = "Service";

    public string Name { get; set; } = "VDS";

    /// <summary>
    /// <c>ConfigId</c> của kết nối SQL SERVER trong <c>Configuration/Database.json</c>.
    /// Dùng cho danh mục (TmsEquipment, TmsZone…) và các bảng có trạng thái (TmsIncident, TmsZoneStatus)
    /// — những bảng đó cần UPDATE nên không chuyển sang QuestDB được.
    /// </summary>
    public string DataConnectionId { get; set; } = "Default";

    /// <summary>
    /// <c>ConfigId</c> của kết nối QuestDB qua PgWire, dùng cho cả ĐỌC và GHI bằng SqlSugar.
    /// Rỗng = tắt toàn bộ QuestDB. Connection string phải có
    /// <c>ServerCompatibilityMode=NoTypeLoading</c> để Npgsql không tải catalog kiểu PostgreSQL mà
    /// QuestDB chỉ mô phỏng một phần.
    /// </summary>
    public string QuestConnectionId { get; set; } = "";

    /// <summary>Ghi vào cột <c>tenant_id</c> của mọi bản ghi do service này sinh.</summary>
    public string TenantId { get; set; } = "Default";

    /// <summary>Ghi vào cột <c>create_uid</c> — truy vết bản ghi do service nào tạo.</summary>
    public string ServiceUserId { get; set; } = "VDS-WORKER";

    /// <summary>
    /// Thư mục lưu state ra đĩa: override cấu hình, ngưỡng tốc độ last-known-good, checkpoint…
    /// Tương đối thì neo vào <c>AppContext.BaseDirectory</c> (xem <c>StatePath.Resolve</c>).
    /// </summary>
    public string StateDirectory { get; set; } = "data";

    /// <summary>
    /// Chu kỳ nạp lại cấu hình TỪ FILE (giây). <b>0 = tắt</b>, và đó là mặc định.
    ///
    /// Đường đổi cấu hình chính của VDS là đẩy tham số xuống (<c>VdsConfigManager.Apply</c>) — có hiệu
    /// lực ngay, không phải chờ chu kỳ. Bật cái này khi muốn sửa <c>appsettings.json</c> trên máy chủ mà
    /// không dừng service. Bật lên vẫn an toàn: mỗi lần reload đều phát lại override lên trên nên không
    /// ghi đè ngược tham số đã đẩy xuống.
    /// </summary>
    public int ConfigReloadSeconds { get; set; }

    /// <summary>
    /// Ngưỡng tốc độ theo hướng tuyến: <c>DirectionId → (min, max)</c> km/h.
    ///
    /// KHÔNG khai trong appsettings ở môi trường thật — nguồn chính là <c>VdsConfig</c> nhóm
    /// <c>vds_config</c> (người vận hành sửa trên web), do <c>OpConfigManager</c> nạp lên rồi ghi cache ra
    /// <c>{StateDirectory}/opconfig-speed.json</c>. Khai ở đây chỉ để test.
    ///
    /// Hướng không có ngưỡng thì <b>fail-closed</b>: không sinh sự kiện quá tốc độ cho hướng đó.
    /// </summary>
    public Dictionary<string, SpeedRange> SpeedRangeByDirection { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Cấu hình biên nhận raw frame và local queue tuần tự.</summary>
    public IngestionOptions Ingestion { get; set; } = new();

    /// <summary>Cấu hình bù dữ liệu lịch sử từ checkpoint.</summary>
    public BackfillOptions Backfill { get; set; } = new();

    /// <summary>Vùng chờ giữ bản tin không xử lý được, để lượt bù lịch sử đi tiếp thay vì đổ cả phiên.</summary>
    public QuarantineOptions Quarantine { get; set; } = new();

    /// <summary>Công tắc bật/tắt từng luồng publish NATS.</summary>
    public PublishOptions Publish { get; set; } = new();

    /// <summary>Ngưỡng và đơn vị để suy cỡ xe short/medium/long.</summary>
    public VehicleSizeOptions VehicleSize { get; set; } = new();

    /// <summary>Xử lý bản tin thống kê chu kỳ do thiết bị tự tổng hợp.</summary>
    public StatisticOptions Statistic { get; set; } = new();

    /// <summary>Phạm vi và bộ lọc của số đếm xe tính từ bảng dữ liệu thô.</summary>
    public CountingOptions Counting { get; set; } = new();

    /// <summary>Tham số của phần trạng thái lưu ra đĩa (chống trùng sự việc).</summary>
    public StateStoreOptions StateStore { get; set; } = new();

    /// <summary>Sinh sự kiện và nâng sự cố.</summary>
    public EventOptions Events { get; set; } = new();

    /// <summary>Danh mục thiết bị nạp vào RAM — camera VDS.</summary>
    public CatalogOptions Catalog { get; set; } = new();

    /// <summary>Nhờ WebAPI/TMS xử lý tiếp sự cố VDS vừa ghi (lập lịch kịch bản tự động).</summary>
    public TmsApiOptions TmsApi { get; set; } = new();

    /// <summary>Trạng thái giao thông của vùng (thông/chùng/nghẽn) ghi xuống bảng trạng thái vùng.</summary>
    public ZoneStatusOptions ZoneStatus { get; set; } = new();

    /// <summary>Cảnh báo biển số theo danh sách đen/trắng.</summary>
    public VehicleFilterOptions VehicleFilter { get; set; } = new();

    /// <summary>Tải và lưu ảnh/clip kèm bản tin.</summary>
    public MediaOptions Media { get; set; } = new();

    /// <summary>Ghép hành trình A→B để tính tốc độ trung bình và nâng sự cố quá tốc độ trung bình.</summary>
    public JourneyOptions Journey { get; set; } = new();

    /// <summary>Job tổng hợp số liệu lâu dài (giờ/ngày/tháng) ghi TmsTrafficSummary trên SQL Server.</summary>
    public SummaryOptions Summary { get; set; } = new();

    /// <summary>Lịch của các job định kỳ (ghi đè nhịp mặc định của từng tính năng).</summary>
    public JobsOptions Jobs { get; set; } = new();

    /// <summary>true = đã khai kết nối QuestDB cho SqlSugar/PgWire.</summary>
    public bool QuestEnabled => !string.IsNullOrWhiteSpace(QuestConnectionId);

    /// <summary>
    /// Sau một lỗi KẾT NỐI QuestDB thì bỏ mọi lượt gọi trong bao nhiêu giây rồi mới thăm dò lại (cầu dao
    /// <c>QuestAvailabilityGate</c>). &lt;= 0 ⇒ dùng mặc định 30 giây.
    ///
    /// Đừng đặt quá nhỏ: mỗi lượt thăm dò khi máy chủ không tới được giữ một luồng vài phút (đã đo 5 phút
    /// 16 giây). Cũng đừng quá lớn: đó là độ trễ trước khi service nhận ra QuestDB đã sống lại.
    /// </summary>
    public int QuestUnavailableCooldownSeconds { get; set; } = 30;

    /// <summary>Hạn nghỉ đã chuẩn hoá của cầu dao QuestDB.</summary>
    public int EffectiveQuestUnavailableCooldownSeconds =>
        QuestUnavailableCooldownSeconds > 0 ? QuestUnavailableCooldownSeconds : 30;

    // KienLT - 31/10/2026: tham số vận hành QuestDB HA nhận từ VdsConfig (mã vds_questha_*), lưu qua config-overrides.json để sống qua restart.
    /// <summary>
    /// Gói tham số vận hành QuestDB HA đã được QuestHaManager chấp nhận gần nhất (nguồn <c>VdsConfig</c> mã <c>vds_questha_*</c>,
    /// do <c>OpConfigManager</c> dựng). <c>null</c> = chưa có gói nào ⇒ section <c>QuestHa</c> của appsettings. Chỉ thay nguyên object.
    /// </summary>
    public global::Shared.QuestHa.Options.QuestHaRuntimeOptions? QuestHaRuntime { get; set; }

    /// <summary>
    /// Bản sao nông — đủ để đổi một tham số mà luồng đang đọc snapshot cũ không bị ảnh hưởng.
    /// <see cref="SpeedRangeByDirection"/> phải sao ra Dictionary MỚI, nếu chỉ gán tham chiếu thì
    /// sửa trên bản sao sẽ đụng luôn vào bản gốc — đúng thứ mà snapshot bất biến muốn tránh.
    /// </summary>
    public ServiceConfig Clone() => new()
    {
        Name = Name,
        DataConnectionId = DataConnectionId,
        QuestConnectionId = QuestConnectionId,
        QuestUnavailableCooldownSeconds = QuestUnavailableCooldownSeconds,
        TenantId = TenantId,
        ServiceUserId = ServiceUserId,
        StateDirectory = StateDirectory,
        ConfigReloadSeconds = ConfigReloadSeconds,
        SpeedRangeByDirection = new Dictionary<string, SpeedRange>(
            SpeedRangeByDirection, StringComparer.OrdinalIgnoreCase),
        Ingestion = Ingestion.Clone(),
        Backfill = Backfill.Clone(),
        Quarantine = Quarantine.Clone(),
        Publish = Publish.Clone(),
        VehicleSize = VehicleSize.Clone(),
        Statistic = Statistic.Clone(),
        Counting = Counting.Clone(),
        StateStore = StateStore.Clone(),
        Events = Events.Clone(),
        Catalog = Catalog.Clone(),
        TmsApi = TmsApi.Clone(),
        ZoneStatus = ZoneStatus.Clone(),
        VehicleFilter = VehicleFilter.Clone(),
        Media = Media.Clone(),
        Journey = Journey.Clone(),
        Summary = Summary.Clone(),
        Jobs = Jobs.Clone(),
        // KienLT - 31/10/2026: gói QuestHa bất biến theo quy ước (chỉ thay nguyên object) nên dùng chung tham chiếu.
        QuestHaRuntime = QuestHaRuntime,
    };
}
