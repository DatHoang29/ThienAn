namespace Shared.QuestHa.Options;

/// <summary>
/// Author: KienLT
/// Description: Cấu hình toàn bộ Shared.QuestHa (section <c>QuestHa</c>). Mọi ngưỡng thời gian cấu hình được;
/// giá trị mặc định là bản RÚT NGẮN cho lab (tham chiếu production dài hơn cho production — ghi chú từng dòng).
/// thêm WriteMode/ReadMode (mặc định Direct/Single = không đổi hành vi).
/// Created date: 09/10/2026
/// </summary>
public sealed class QuestHaOptions
{
    public const string SectionName = "QuestHa";

    /// <summary>Cách ghi (mặc định Direct = hành vi cũ, chưa dùng journal).</summary>
    public QuestWriteModeEnum WriteMode { get; set; } = QuestWriteModeEnum.Direct;

    /// <summary>Cách đọc (mặc định Single = hành vi cũ, chưa dùng router).</summary>
    public QuestReadModeEnum ReadMode { get; set; } = QuestReadModeEnum.Single;

    /// <summary>Journal (SQL Server, DB riêng). Bắt buộc cấu hình; rỗng ⇒ QuestJournalService báo lỗi khi khởi tạo.</summary>
    public string JournalConnectionString { get; set; } = "";

    /// <summary>Danh sách node QuestDB. Thứ tự ưu tiên đọc theo <see cref="QuestNodeOptions.Priority"/>.</summary>
    public List<QuestNodeOptions> Nodes { get; set; } = new();

    // nguồn ghi trực tiếp ở Shadow chỉ phục vụ đối soát, không được replicator giao lại.
    /// <summary>Node nguồn ngoài danh sách đích nhân bản, dùng bởi công cụ đối soát/vá ở Shadow.</summary>
    public QuestNodeOptions? SourceNode { get; set; }

    public JournalOptions Journal { get; set; } = new();

    // chính sách, không chạy ở reader hoặc Direct.
    public JournalMaintenanceOptions JournalMaintenance { get; set; } = new();

    // ngưỡng giám sát.
    public QuestHaHealthOptions Health { get; set; } = new();

    public ReplicatorOptions Replicator { get; set; } = new();

    public RouterOptions Router { get; set; } = new();

    public ReconcilerOptions Reconciler { get; set; } = new();

    public BackupOptions Backup { get; set; } = new();

    /// <summary>Backup xuất Parquet hằng ngày (mặc định tắt).</summary>
    public ExportOptions Export { get; set; } = new();

    /// <summary>Thời gian chờ một lệnh SQL/PG (giây).</summary>
    public int CommandTimeoutSeconds { get; set; } = 10;

    /// <summary>Bản sao nông để dựng snapshot tham số vận hành mới; không sửa bản đang được đọc.</summary>
    internal QuestHaOptions Clone() => (QuestHaOptions)MemberwiseClone();
}
