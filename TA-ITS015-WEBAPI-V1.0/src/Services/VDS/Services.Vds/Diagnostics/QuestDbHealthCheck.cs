using Microsoft.Extensions.Hosting;
using Services.Vds.Configuration;
using Services.Vds.Pipeline.Persistence;

namespace Services.Vds.Diagnostics;

/// <summary>
/// Author: KienLT
/// Description: Kiểm tra kết nối QuestDB MỘT LẦN lúc khởi động rồi ghi kết quả ra log.
///
/// <para><b>Không được để lượt kiểm tra này chặn khởi động.</b> Hosted service chạy TUẦN TỰ, nên nếu
/// <see cref="StartAsync"/> chờ kết quả thì cả host đứng theo: <c>ApplicationStarted</c> không bắn và
/// mọi service đăng ký sau không chạy. Vì thế phép thử được đẩy sang luồng nền, <c>StartAsync</c> trả
/// về ngay.</para>
///
/// <para><b>Số đo thực tế (26/08/2026, QuestDB không tới được):</b> một lượt <c>CountAsync</c> chặn
/// <b>5 phút 16 giây</b> rồi mới báo lỗi, dù connection string khai <c>Timeout=30</c> và lời gọi có
/// <see cref="CancellationToken"/> hết hạn sau 10 giây. Lý do đọc từ stack: SqlSugar
/// <c>QuestDBProvider.CheckConnection()</c> gọi <c>NpgsqlConnection.Open()</c> bản ĐỒNG BỘ — không
/// nhận token, và hạn kết nối của Npgsql không chặn được lượt mở đó. Kết luận cho mọi chỗ khác:
/// truyền token cho repository QuestDB KHÔNG đủ để dừng một lượt gọi đang treo; muốn có hạn thì phải
/// canh bằng đồng hồ (<see cref="Task.WhenAny(Task, Task)"/>) và chấp nhận luồng kia còn treo.</para>
///
/// Nguyên tắc quan trọng: KHÔNG ném ngoại lệ ra ngoài. QuestDB chết thì service vẫn phải khởi động
/// được — mọi tính năng chạy trên SQL Server không có lý do gì phải dừng theo.
/// Created date: 24/08/2026
/// </summary>
public sealed class QuestDbHealthCheck : IHostedService
{
    /// <summary>
    /// Sau bao nhiêu giây thì báo "chưa phản hồi". Chỉ để LOG cho người vận hành biết máy chủ đang im,
    /// không huỷ được lượt gọi (xem ghi chú ở phần mô tả class).
    /// </summary>
    private const int TimeoutSeconds = 10;

    private readonly IVdsConfigManager _config;
    private readonly QuestTrafficDataRepository _repo;
    private readonly ILogger<QuestDbHealthCheck> _logger;

    public QuestDbHealthCheck(
        IVdsConfigManager config,
        QuestTrafficDataRepository repo,
        ILogger<QuestDbHealthCheck> logger)
    {
        _config = config;
        _repo = repo;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken ct)
    {
        // Cố tình KHÔNG await: xem ghi chú ở phần mô tả class. Task chạy nền trên thread pool nên nó
        // cũng không giữ tiến trình lại lúc thoát.
        _ = Task.Run(ProbeAsync, CancellationToken.None);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    private async Task ProbeAsync()
    {
        // Đọc snapshot MỘT LẦN: hai lời gọi Current có thể rơi vào hai snapshot khác nhau nếu vừa
        // có thay đổi, khi đó log sẽ nói về cấu hình này mà lại kết nối bằng cấu hình khác.
        var config = _config.Current;

        if (!config.QuestEnabled)
        {
            _logger.LogInformation(
                "[QuestDb] Chưa khai Service:QuestConnectionId — bỏ qua kiểm tra kết nối.");
            return;
        }

        // Log TRƯỚC khi thử: không có dòng này thì một lượt treo trông giống hệt lỗi ở service khác.
        _logger.LogInformation("[QuestDb] Đang thử kết nối {ConfigId}...", config.QuestConnectionId);

        var to = DateTime.UtcNow;
        var from = to.AddHours(-1);

        // Bọc thêm một Task.Run: SqlSugar mở kết nối ĐỒNG BỘ ngay trong thân method async, nên gọi
        // trực tiếp sẽ chặn luôn ProbeAsync và không quan sát được hạn báo chậm bên dưới.
        var work = Task.Run(() => _repo.CountAsync(from, to), CancellationToken.None);

        if (await Task.WhenAny(work, Task.Delay(TimeSpan.FromSeconds(TimeoutSeconds)))
                .ConfigureAwait(false) != work)
        {
            _logger.LogWarning(
                "[QuestDb] Chưa phản hồi sau {Timeout}s ({ConfigId}) — gần như luôn là không tới được " +
                "máy chủ (sai IP/cổng, firewall, hoặc QuestDB chưa chạy). Vẫn chờ kết quả thật; khởi " +
                "động KHÔNG bị chặn.",
                TimeoutSeconds, config.QuestConnectionId);
        }

        try
        {
            var count = await work.ConfigureAwait(false);
            _logger.LogInformation(
                "[QuestDb] Kết nối OK ({ConfigId}). VdsTrafficData 1 giờ gần nhất: {Count} dòng.",
                config.QuestConnectionId, count);
        }
        catch (Exception ex)
        {
            // SqlSugar QuestDBProvider nuốt lỗi của lượt Open rồi để Npgsql báo lại là
            // "Connection is not open", nên ngoại lệ ở đây KHÔNG nói đúng nguyên nhân gốc.
            _logger.LogError(ex,
                "[QuestDb] KẾT NỐI THẤT BẠI ({ConfigId}) — chưa dùng được QuestDB. Ngoại lệ bên dưới " +
                "có thể chỉ là 'Connection is not open'; nguyên nhân thật thường là không tới được máy " +
                "chủ. Kiểm tra Configuration/Database.json: ConfigId có đúng, DbType phải là 'QuestDB', " +
                "và connection string cần 'Command Timeout'.",
                config.QuestConnectionId);
        }
    }
}
