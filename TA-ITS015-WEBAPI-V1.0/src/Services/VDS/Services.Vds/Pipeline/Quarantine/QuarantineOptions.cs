namespace Services.Vds.Pipeline.Quarantine;

/// <summary>
/// Author: KienLT
/// Description: Tham số của vùng chờ (quarantine) giữ những bản tin KHÔNG xử lý được.
///
/// <para><b>Vì sao cần vùng chờ.</b> Trước 23/09/2026 một bản tin bị loại làm lượt bù lịch sử ném
/// <c>InvalidDataException</c>, lỗi thoát ra tới <c>SseEventSource</c> nên cả đường live cũng dừng. Đo được
/// trên máy thật: một bản tin của camera chưa khai danh mục giữ worker đứng 2 ngày, lặp 296 lượt bù.
/// Vùng chờ cho lượt bù đi tiếp mà KHÔNG im lặng bỏ dữ liệu — bản tin nằm lại có đủ lý do để đối soát và
/// replay.</para>
///
/// <para>Phải có setter (không dùng <c>record</c>) vì <c>IConfiguration.Get&lt;T&gt;()</c> cần setter để bind.</para>
/// Created date: 23/09/2026
/// </summary>
public sealed class QuarantineOptions
{
    /// <summary>
    /// Bật vùng chờ. <b>Mặc định BẬT</b>: tắt đi thì bản tin không xử lý được lại không có nơi lưu, và lượt
    /// bù buộc phải chọn giữa bỏ im lặng hoặc dừng — đúng hai lựa chọn tồi mà vùng chờ sinh ra để tránh.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Thư mục con trong <c>StateDirectory</c> chứa <c>index.json</c> và thư mục <c>payload</c>.
    /// </summary>
    public string Directory { get; set; } = "quarantine";

    /// <summary>
    /// Trần số bản tin giữ trong vùng chờ. Vượt trần thì bỏ mục CŨ NHẤT và ghi log mức Error — bộ đếm tổng
    /// vẫn giữ nguyên để đối soát, vì mất dấu vết im lặng chính là thứ vùng chờ phải ngăn.
    /// </summary>
    public int MaxEntries { get; set; } = 10_000;

    /// <summary>Số ngày giữ một mục kể từ lần gặp đầu tiên. Quá hạn thì dọn cùng lượt ghi kế tiếp.</summary>
    public int RetentionDays { get; set; } = 14;

    /// <summary>
    /// Số lượt replay tối đa cho một mục <c>Unmapped</c>. Hết số lượt thì chuyển sang <c>dropped</c> và thôi
    /// thử lại — replay mãi một bản tin mà danh mục vẫn thiếu chỉ tốn truy vấn và làm log ồn.
    ///
    /// <para>Đếm theo LƯỢT REPLAY (mỗi lượt cách nhau một chu kỳ nạp lại danh mục), không phải theo lần thử
    /// liên tiếp: nguyên nhân của <c>Unmapped</c> chỉ mất đi khi danh mục đổi, nên thử lại dồn dập trong một
    /// giây không mang thêm thông tin nào.</para>
    /// </summary>
    public int MaxReplayAttempts { get; set; } = 3;

    /// <summary>Trần đã chuẩn hoá; luôn ≥ 1 mục.</summary>
    public int EffectiveMaxEntries => Math.Max(1, MaxEntries);

    /// <summary>Hạn giữ đã chuẩn hoá, tính bằng ngày; luôn ≥ 1.</summary>
    public int EffectiveRetentionDays => Math.Max(1, RetentionDays);

    /// <summary>Số lượt replay đã chuẩn hoá; 0 = không replay, chỉ giữ để đối soát.</summary>
    public int EffectiveMaxReplayAttempts => Math.Max(0, MaxReplayAttempts);

    /// <summary>
    /// Ghi LỊCH SỬ chỉ-thêm ra <c>{Directory}/history/quarantine-{yyyyMMdd}.jsonl</c>, một dòng JSON cho mỗi
    /// biến cố.
    ///
    /// <para><b>Vì sao cần dù đã có <c>index.json</c>.</b> Index là trạng thái HIỆN HÀNH: mục replay xong,
    /// quá hạn giữ, hoặc bị trần đẩy ra đều biến mất khỏi đó. Lịch sử là thứ trả lời được "hôm qua bỏ bao
    /// nhiêu bản tin, của camera nào, vì sao, về sau có bù được không".</para>
    /// </summary>
    public bool HistoryEnabled { get; set; } = true;

    /// <summary>
    /// Số ngày giữ file lịch sử. <b>0 = KHÔNG tự xoá</b> (mặc định) — cùng quy ước với log frame thô: đây là
    /// bản ghi để đối soát, việc dọn thuộc về vận hành.
    /// </summary>
    public int HistoryRetentionDays { get; set; }

    /// <summary>Tên thư mục đã chuẩn hoá; rỗng ⇒ về mặc định để không ghi thẳng vào gốc state.</summary>
    public string EffectiveDirectory =>
        string.IsNullOrWhiteSpace(Directory) ? "quarantine" : Directory.Trim();

    /// <summary>Bản sao nông theo đúng khuôn snapshot bất biến của <c>ServiceConfig</c>.</summary>
    public QuarantineOptions Clone() => new()
    {
        Enabled = Enabled,
        Directory = Directory,
        MaxEntries = MaxEntries,
        RetentionDays = RetentionDays,
        MaxReplayAttempts = MaxReplayAttempts,
        HistoryEnabled = HistoryEnabled,
        HistoryRetentionDays = HistoryRetentionDays,
    };
}
