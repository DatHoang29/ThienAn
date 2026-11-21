namespace Services.Vds.Features.Media;

/// <summary>
/// Author: KienLT
/// Description: Tải và lưu ảnh/clip kèm bản tin (section <c>"Service:Media"</c>).
///
/// <para>
/// <b>KHÔNG dùng bảng media</b>: đường dẫn file suy ra tất định từ (loại bản tin, thời điểm phát hiện, loại
/// media), nên bên đọc chỉ cần dòng dữ liệu thô là dựng lại được đường dẫn. Đánh đổi: đổi quy ước đặt tên là
/// làm mất dấu toàn bộ ảnh cũ, nên ba suffix <c>v</c>/<c>p</c>/<c>f</c> phải giữ nguyên.
/// </para>
///
/// Tên thuộc tính ở đây BỎ tiền tố <c>Media</c> của bản cũ (<c>MediaQueueCapacity</c> →
/// <c>QueueCapacity</c>): section đã tên là <c>Media</c>, lặp lại chỉ làm khoá cấu hình dài ra.
/// Created date: 26/08/2026
/// </summary>
public sealed class MediaOptions
{
    private HashSet<string>? _kindSet;

    /// <summary>
    /// Tải và lưu media.
    ///
    /// <b>Mặc định TẮT</b>: cần khai <see cref="StorageDirectory"/> trỏ đúng thư mục web đang phục vụ ảnh,
    /// nếu không thì ảnh nằm cạnh file thực thi và giao diện không mở được. Bật cùng worker cũ thì mỗi ảnh bị
    /// tải hai lần (tên file tất định nên nội dung không hỏng, chỉ tốn băng thông và lượt ghi đĩa).
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Loại media được tải. RỖNG = tải mọi loại.
    ///
    /// Mặc định giữ ba loại nhẹ nhất; <c>clip</c> là VIDEO nên bật thì phải tính lại dung lượng đĩa.
    /// </summary>
    public List<string> Kinds { get; set; } = new() { "vehicle", "plate", "full" };

    /// <summary>
    /// Thư mục GỐC lưu media. Tuyệt đối thì dùng nguyên; tương đối thì neo vào thư mục chứa file thực thi
    /// (KHÔNG neo vào working directory — chạy như Windows Service thì đó là thư mục hệ thống).
    ///
    /// Bố cục bên dưới: <c>{data|event}/{yyyyMM}/{ddHH}/{yyyyMMddHHmmssfff}_{suffix}{ext}</c>.
    ///
    /// <b>Ở môi trường thật phải trỏ vào thư mục ảnh của WebAPI</b>, vì giao diện lấy ảnh qua web chứ không
    /// đọc đĩa của service.
    /// </summary>
    public string StorageDirectory { get; set; } = "media";

    /// <summary>Số việc tải chờ tối đa. Đầy thì BỎ việc mới, không chặn luồng xử lý bản tin.</summary>
    public int QueueCapacity { get; set; } = 2_000;

    /// <summary>Số worker tải song song — cũng là trần số kết nối tải cùng lúc.</summary>
    public int WorkerCount { get; set; } = 4;

    /// <summary>Timeout tải một file (giây).</summary>
    public int DownloadTimeoutSeconds { get; set; } = 15;

    /// <summary>Kích thước tối đa một file được lưu (byte); lớn hơn thì bỏ.</summary>
    public long MaxBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>Khi dừng service, chờ tải hết việc còn lại trong bao nhiêu giây trước khi huỷ.</summary>
    public int ShutdownDrainSeconds { get; set; } = 15;

    public int EffectiveQueueCapacity => Math.Max(16, QueueCapacity);
    public int EffectiveWorkerCount => Math.Clamp(WorkerCount, 1, 32);
    public int EffectiveDownloadTimeoutSeconds => Math.Clamp(DownloadTimeoutSeconds, 1, 300);
    public long EffectiveMaxBytes => MaxBytes > 0 ? MaxBytes : 10 * 1024 * 1024;
    public int EffectiveShutdownDrainSeconds => Math.Clamp(ShutdownDrainSeconds, 0, 300);

    /// <summary>Loại media này có được tải không (rỗng = tải hết). Không phân biệt hoa thường.</summary>
    public bool AcceptsKind(string? kind)
    {
        if (Kinds.Count == 0) return true;

        _kindSet ??= new HashSet<string>(Kinds, StringComparer.OrdinalIgnoreCase);
        return _kindSet.Contains(kind ?? "");
    }

    /// <summary>Thư mục gốc dạng tuyệt đối.</summary>
    public string ResolveRoot()
    {
        var directory = string.IsNullOrWhiteSpace(StorageDirectory) ? "media" : StorageDirectory.Trim();

        return Path.IsPathRooted(directory)
            ? directory
            : Path.Combine(AppContext.BaseDirectory, directory);
    }

    public MediaOptions Clone() => new()
    {
        Enabled = Enabled,
        Kinds = new List<string>(Kinds),
        StorageDirectory = StorageDirectory,
        QueueCapacity = QueueCapacity,
        WorkerCount = WorkerCount,
        DownloadTimeoutSeconds = DownloadTimeoutSeconds,
        MaxBytes = MaxBytes,
        ShutdownDrainSeconds = ShutdownDrainSeconds,
    };
}
