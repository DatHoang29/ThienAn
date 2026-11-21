namespace ITS.FA.Core.Configuration;

/// <summary>
/// Cấu hình dịch vụ FA (section "Service" trong appsettings.json). Không có giá trị nghiệp vụ nào
/// gán cứng trong code: thanh ghi, địa chỉ tủ, loại sự kiện, loại camera... đều khai ở đây.
/// Author: ChuongLP
/// Created date: 29/10/2026
/// </summary>
public sealed class FaOptions
{
    public const string SectionName = "Service";

    /// <summary>ConfigId trong Database.json trỏ tới SQL Server chứa bảng Fa* và Tms*.</summary>
    public string DataConnectionId { get; set; } = "Default";

    public string TenantId { get; set; } = "Default";

    /// <summary>Ghi vào CreateUId/UpdateUId để truy vết bản ghi do dịch vụ nào tạo. Cũ: EventManager.ModifiedIdentity = "FA Service".</summary>
    public string ServiceUserId { get; set; } = "FA Service";

    public FaModbusOptions Modbus { get; set; } = new();

    public FaIncidentOptions Incident { get; set; } = new();

    public FaPublishOptions Publish { get; set; } = new();
}

/// <summary>Bật/tắt từng loại bản tin NATS. Kết nối NATS khai ở Configuration/DataTransporter.json.</summary>
public sealed class FaPublishOptions
{
    /// <summary>Phát sự cố mới lên ta.its.data.incident.</summary>
    public bool Incident { get; set; } = true;

    /// <summary>Phát trạng thái điểm báo cháy và cửa lên ta.its.data.fa.</summary>
    public bool State { get; set; } = true;
}

/// <summary>
/// Kết nối Modbus TCP và bản đồ thanh ghi tổng của tủ báo cháy.
/// Địa chỉ viết theo quy ước cũ (1-based, code trừ 1 khi gửi lệnh).
/// </summary>
public sealed class FaModbusOptions
{
    public string Ip { get; set; } = string.Empty;

    public int Port { get; set; } = 502;

    public byte UnitId { get; set; } = 1;

    /// <summary>Thanh ghi input: số điểm đang báo cháy. Cũ: FireCountAddress = 1441.</summary>
    public ushort FireCountAddress { get; set; }

    /// <summary>Thanh ghi input: số điểm đang lỗi. Cũ: FaultCountAddress = 1443 (code cũ không dùng).</summary>
    public ushort FaultCountAddress { get; set; }

    /// <summary>Discrete input: báo cháy chưa rõ nguồn. Cũ: UnknownFireAlarm = 1281.</summary>
    public ushort UnknownFireAlarmAddress { get; set; }

    /// <summary>Chu kỳ quét. Cũ: Thread.Sleep(5000).</summary>
    public int ScanIntervalMs { get; set; } = 5000;

    /// <summary>Số giây chờ trước khi chạy lại khi mất kết nối. Cũ: đếm ngược 5 giây ở Program.Main.</summary>
    public int RestartDelaySeconds { get; set; } = 5;

    public int ConnectTimeoutMs { get; set; } = 3000;

    /// <summary>Hạn đọc/ghi mỗi lệnh Modbus. Không đặt thì tủ treo kết nối sẽ làm vòng quét treo theo.</summary>
    public int ReadTimeoutMs { get; set; } = 3000;
}

/// <summary>Cách tạo sự cố khi có báo cháy. Các giá trị cũ gán cứng (23, "3", "FA", "1", 4/5) nay khai ở đây.</summary>
public sealed class FaIncidentOptions
{
    /// <summary>Bật/tắt tạo sự cố. Tắt thì chỉ cập nhật trạng thái điểm.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// TmsEventType.ID của sự cố báo cháy. Loại này CHƯA tạo trong DB — để trống thì sự cố vẫn được tạo
    /// nhưng EventTypeId = null (và có cảnh báo trong log); khai ID khi đã tạo loại sự kiện.
    /// Cũ: INCIDENT_TYPE_ID = 23, EVENT_TYPE = "3".
    /// </summary>
    public string? EventTypeId { get; set; }

    /// <summary>Nhóm sự kiện (TmsIncident.EventGroup). Để trống nếu loại sự kiện chưa có.</summary>
    public string? EventGroup { get; set; }

    /// <summary>
    /// Nguồn sự cố và người tạo: mã trong SysConfigData nhóm tms_source_detect ("fa" = Fire alarm, như "vds").
    /// Cũ: SOURCE = "FA", CREATED_BY = "FA".
    /// </summary>
    public string Source { get; set; } = "fa";

    /// <summary>
    /// Mã và tên loại dùng trong mã/tên sự cố khi chưa khai hoặc không tìm thấy <see cref="EventTypeId"/>.
    /// Mã/tên sự cố đặt theo IncidentNamingHelper như VDS (cũ: "INC_FA_km_met_..." và "FA_Km...").
    /// </summary>
    public string FallbackEventCode { get; set; } = "fire";

    public string FallbackEventName { get; set; } = "Báo cháy";

    /// <summary>
    /// Trạng thái sự cố được coi là ĐÃ ĐÓNG khi kiểm tra trùng vị trí (cũ: STATUS NOT IN (4, 5) và > 0).
    /// Giá trị theo BaseEnums.TmsIncidentStateEnum: 0 Skipped, 4 Finished, 5 Canceled.
    /// </summary>
    public List<string> ClosedStates { get; set; } = ["0", "4", "5"];

    /// <summary>TmsEquipment.EquipmentTypeId của camera dùng để chọn camera theo vùng. Để trống = không gắn camera.</summary>
    public string? CameraEquipmentTypeId { get; set; }

    /// <summary>Số lần thử ghi sự cố. Cũ: RetryHelper.RetryOnException(10, ...).</summary>
    public int SaveRetryAttempts { get; set; } = 10;

    /// <summary>Số giây chờ giữa các lần thử. Cũ: TimeSpan.FromSeconds(2).</summary>
    public int SaveRetryDelaySeconds { get; set; } = 2;
}
