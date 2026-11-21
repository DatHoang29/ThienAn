using Modules.TMS.Core.Entities;
using Services.Shared.Persistence;
using Services.Vds.Configuration;
using Services.Vds.Features.Statistic;
using Services.Vds.Pipeline.Persistence;
using Services.Vds.Shared.Persistence;
using SqlSugar;

namespace Services.Vds.Diagnostics;

/// <summary>Số bản ghi của các bảng mà service này GHI — để quan sát kết quả sau mỗi lượt test.</summary>
/// <param name="TrafficDataRecent">Dữ liệu thô trên QuestDB trong cửa sổ gần đây.</param>
/// <param name="StatisticRecent">Bản tin thống kê chu kỳ trên QuestDB trong cửa sổ gần đây.</param>
/// <param name="Incident">Sự cố trên SQL Server (tổng).</param>
/// <param name="ZoneStatus">Số vùng có dòng trạng thái (tổng).</param>
/// <param name="FilterHistory">Lịch sử khớp biển số (tổng).</param>
public readonly record struct VdsRowCounts(
    int TrafficDataRecent,
    int StatisticRecent,
    int Incident,
    int ZoneStatus,
    int FilterHistory);

/// <summary>
/// Author: KienLT
/// Description: Đọc số liệu quan sát cho harness — KHÔNG dùng trong đường dữ liệu.
///
/// <para>Chỉ đếm những bảng mà stack MỚI thực sự ghi. Bản cũ đếm <c>TmsTrafficData</c>,
/// <c>TmsTrafficAnalysis</c>, <c>TmsEventData</c>, <c>TmsTrafficCount</c>, <c>TmsTrafficSummary</c>,
/// <c>TmsTrafficJourney</c> — kiến trúc mới **không ghi bảng nào trong số đó**: dữ liệu thô và thống kê
/// sang QuestDB, số đếm tính lại từ bảng thô nên không có bảng chốt, hành trình chuyển sang WebAPI. Đếm
/// những bảng đó lại thì con số luôn đứng im và người test sẽ tưởng pipeline không chạy.</para>
///
/// <para>Hai bảng QuestDB đếm theo CỬA SỔ THỜI GIAN, không đếm tổng: QuestDB phân vùng theo ngày, và với
/// harness thì "có bản tin nào vừa vào không" mới là câu cần trả lời — bảng thật đang có 10 triệu dòng
/// nên con số tổng không nói lên điều gì. Hai lượt đọc đó đi qua cầu dao
/// <c>QuestAvailabilityGate</c> như mọi lượt gọi QuestDB khác.</para>
/// Created date: 26/08/2026
/// </summary>
public sealed class VdsDiagnosticsRepository : VdsRepositoryBase
{
    /// <summary>Cửa sổ quan sát của hai bảng QuestDB.</summary>
    private static readonly TimeSpan RecentWindow = TimeSpan.FromHours(24);

    private readonly QuestTrafficDataRepository _trafficData;
    private readonly QuestTrafficStatisticRepository _statistic;
    private readonly IVdsConfigManager _config;

    public VdsDiagnosticsRepository(
        DatabaseManager databaseManager,
        IVdsConfigManager config,
        QuestTrafficDataRepository trafficData,
        QuestTrafficStatisticRepository statistic)
        : base(databaseManager, config)
    {
        _config = config;
        _trafficData = trafficData;
        _statistic = statistic;
    }

    /// <summary>
    /// Đếm bản ghi các bảng chính. QuestDB chưa cấu hình hoặc đang không dùng được ⇒ hai số đầu trả
    /// <c>-1</c> thay vì ném: harness phải hiển thị được phần SQL Server dù QuestDB chết.
    /// </summary>
    public async Task<VdsRowCounts> CountRowsAsync(CancellationToken ct = default)
    {
        var to = DateTime.UtcNow;
        var from = to - RecentWindow;

        var traffic = -1;
        var statistic = -1;
        if (_config.Current.QuestEnabled)
        {
            try
            {
                traffic = await _trafficData.CountAsync(from, to, ct).ConfigureAwait(false);
                statistic = await _statistic.CountAsync(from, to, ct).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Giữ -1: chỗ gọi hiển thị "n/a". Lỗi chi tiết đã được cầu dao và health check ghi log.
            }
        }

        return new VdsRowCounts(
            traffic,
            statistic,
            await Client.Queryable<TmsIncident>().CountAsync(),
            await Client.Queryable<TmsZoneStatus>().CountAsync(),
            await Client.Queryable<TmsVehicleFilterHistory>().CountAsync());
    }

    /// <summary>Chuỗi ngắn mô tả đang nối vào SQL Server nào — để biết harness đang ghi vào đâu.</summary>
    public string DescribeConnection()
    {
        try
        {
            var b = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(
                Client.CurrentConnectionConfig.ConnectionString);
            return $"{b.DataSource}/{b.InitialCatalog}";
        }
        catch
        {
            return "(không đọc được chuỗi kết nối)";
        }
    }

    /// <summary>Dòng trạng thái hiện tại của một vùng — để đối chiếu với máy trạng thái trong RAM.</summary>
    public Task<TmsZoneStatus?> FindZoneStatusAsync(string zoneId, CancellationToken ct = default) =>
        Client.Queryable<TmsZoneStatus>()
            .Where(z => z.ZoneId == zoneId)
            .FirstAsync();
}
