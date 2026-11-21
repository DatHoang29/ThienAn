namespace Shared.QuestHa.Options;

/// <summary>
/// Author: KienLT
/// Description: Backup xuất Parquet hằng ngày (section <c>QuestHa:Export</c>). Hạ tầng (đổi phải restart): <see cref="Enabled"/>,
/// <see cref="ExportRoot"/>, <see cref="HttpTimeout"/>, <see cref="CoverageWait"/>, <see cref="PollInterval"/>, <see cref="MinimumFreeBytes"/>.
/// Tham số vận hành còn lại đổi lúc chạy qua VdsConfig (<c>vds_questha_export_*</c>); giá trị ở đây là mặc định khi DB thiếu dòng.
/// Created date: 02/11/2026
/// </summary>
public sealed class ExportOptions
{
    /// <summary>Bật lịch xuất trong host Writer và cho export Verified được tính như "có backup" khi dọn journal/Health. Mặc định tắt.</summary>
    public bool Enabled { get; set; }

    /// <summary>Thư mục gốc chứa bản xuất <c>{ExportRoot}/{bảng}/{yyyy}/{MM}/{yyyy-MM-dd}/</c>; tuyệt đối, local hoặc UNC.</summary>
    public string ExportRoot { get; set; } = "";

    /// <summary>Thời gian chờ tối đa một lệnh HTTP <c>/exp</c> (một giờ dữ liệu).</summary>
    public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>Ngân sách chờ node chọn phủ head journal (cursor ≥ head, WAL áp tới mốc) trước khi xuất.</summary>
    public TimeSpan CoverageWait { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>Nhịp worker lịch kiểm việc đến hạn.</summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>Dung lượng trống tối thiểu phải còn sau khi xuất (ngoài điều kiện trống ≥ ước lượng × 2).</summary>
    public long MinimumFreeBytes { get; set; } = 1L * 1024 * 1024 * 1024;

    /// <summary>Giờ chạy hằng ngày <c>HH:mm</c> theo giờ server; xuất trọn ngày hôm trước [00:00, 24:00) giờ server.</summary>
    public string Schedule { get; set; } = "02:00";

    /// <summary>Giữ thư mục ngày bao nhiêu ngày (bản Verified mới nhất mỗi bảng luôn giữ).</summary>
    public int RetentionDays { get; set; } = 90;

    /// <summary>Mỗi lượt kiểm lại bao nhiêu ngày trước ngày vừa xuất (1 ⇒ ngày D-2) để bắt dữ liệu đến muộn.</summary>
    public int RecheckDays { get; set; } = 1;

    /// <summary>Số lần thử tối đa trong một ngày.</summary>
    public int MaxAttempts { get; set; } = 6;

    /// <summary>Chờ giữa hai lần thử.</summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>Ngày mới nhất đã xuất Verified được xuất cách đây lâu hơn ⇒ Health Warn.</summary>
    public TimeSpan WarnAfter { get; set; } = TimeSpan.FromHours(26);

    /// <summary>Ngày mới nhất đã xuất Verified được xuất cách đây lâu hơn ⇒ Health Critical.</summary>
    public TimeSpan CriticalAfter { get; set; } = TimeSpan.FromHours(50);

    /// <summary>true ⇒ lịch chỉ xem trước (node, số dòng, dung lượng ước) và ghi timeline, không ghi file/catalog, không xoá.</summary>
    public bool DryRun { get; set; } = true;

    /// <summary>Kiểm cấu hình hạ tầng khi bật; sai ⇒ <see cref="ArgumentException"/>.</summary>
    public void ValidateInfrastructure()
    {
        if (!Enabled) return;
        if (string.IsNullOrWhiteSpace(ExportRoot) || !Path.IsPathFullyQualified(ExportRoot))
            throw new ArgumentException("QuestHa:Export:ExportRoot phải là đường dẫn tuyệt đối khi Export:Enabled=true.");
        if (HttpTimeout <= TimeSpan.Zero || HttpTimeout > TimeSpan.FromHours(6) || CoverageWait <= TimeSpan.Zero ||
            CoverageWait > TimeSpan.FromHours(1) || PollInterval < TimeSpan.FromSeconds(5) || PollInterval > TimeSpan.FromHours(1) ||
            MinimumFreeBytes < 0)
            throw new ArgumentException("QuestHa:Export có HttpTimeout/CoverageWait/PollInterval/MinimumFreeBytes không hợp lệ.");
    }

    /// <summary>Bản sao nông để dựng snapshot tham số vận hành mới; không sửa bản đang được đọc.</summary>
    internal ExportOptions Clone() => (ExportOptions)MemberwiseClone();
}
