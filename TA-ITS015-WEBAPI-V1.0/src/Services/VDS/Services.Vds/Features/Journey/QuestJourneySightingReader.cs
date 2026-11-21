using Services.Shared.Persistence;
using Services.Vds.Configuration;
using Shared.QuestHa;
using Services.Vds.Shared.Persistence;
using Services.Vds.Shared.Domain;

namespace Services.Vds.Features.Journey;

/// <summary>
/// Author: KienLT
/// Description: Một lượt xe đọc từ bảng thô, đủ dữ liệu để ghép hành trình A→B. Giữ TỐI THIỂU field để
/// <see cref="JourneyMatcher"/> là hàm thuần, test được mà không cần entity QuestDB.
/// Created date: 04/09/2026
/// </summary>
/// <param name="EquipmentId">Camera ghi nhận lượt xe (so với hai đầu đoạn).</param>
/// <param name="LicensePlate">Biển số THÔ (matcher tự chuẩn hoá).</param>
/// <param name="DetectTime">Thời điểm phát hiện (UTC).</param>
/// <param name="VehicleSize">Cỡ xe (copy sang hành trình để giữ giá trị lịch sử).</param>
public readonly record struct JourneySighting(
    string? EquipmentId, string? LicensePlate, DateTime DetectTime, string? VehicleSize);

/// <summary>
/// Author: KienLT
/// Description: Đọc lượt xe từ QuestDB <c>VdsTrafficData</c> để ghép hành trình A→B — thay cho nguồn cũ
/// <c>TmsTrafficAnalysis</c> (bảng đã bỏ trong stack mới).
///
/// <para>Chỉ lấy lượt CÓ BIỂN SỐ ở đúng hai camera đầu/cuối của đoạn trong cửa sổ thời gian: ghép hành
/// trình theo biển số nên lượt không biển vô dụng, và lọc hai camera ngay trong truy vấn để không kéo cả
/// tuyến về bộ nhớ.</para>
///
/// <para>Mọi truy vấn LUÔN có điều kiện trên <c>detectTime</c> (QuestDB phân vùng theo ngày; thiếu filter
/// thời gian là quét cả bảng và server tự huỷ query) và đi qua <c>GuardAsync</c> của
/// <see cref="QuestRepositoryBase"/>.</para>
/// Created date: 04/09/2026
/// </summary>
public sealed class QuestJourneySightingReader : QuestRepositoryBase
{
    /// <summary>Exact raw B identity for registration full; never derive it from the journey EventId.</summary>
    public async Task<Modules.VDS.Core.Dto.VdsRegistrationImageSourceOutput?> ReadImageSourceAsync(
        string equipmentId, DateTime detectTimeUtc, string licensePlate, CancellationToken ct)
    {
        var time = DateTime.SpecifyKind(detectTimeUtc, DateTimeKind.Utc);
        var rows = await ReadAsync(db => db.Queryable<VdsTrafficData>()
            .Where(x => x.EquipmentId == equipmentId && x.DetectTime == time && x.IsDelete == null)
            .ToListAsync(ct), ct);
        var matches = rows.Where(x => PlateRules.Normalize(x.LicensePlate) == PlateRules.Normalize(licensePlate)).Take(2).ToList();
        if (matches.Count != 1) return null;
        var row = matches[0];
        return new(row.VendorEventId, DateTime.SpecifyKind(row.DetectTime, DateTimeKind.Utc),
            row.EquipmentId!, row.CameraIp, row.EventType);
    }

    public QuestJourneySightingReader(
        DatabaseManager databaseManager, IVdsConfigManager config, QuestAvailabilityGate gate, QuestHaManager? questHa = null) // KienLT - 31/10/2026: đọc HA qua QuestHaManager
        : base(databaseManager, config, gate, questHa)
    {
    }

    /// <summary>
    /// Lượt xe có biển số của hai camera <paramref name="fromEquipmentId"/>/<paramref name="toEquipmentId"/>
    /// trong <c>[fromUtc, toUtc)</c>, sắp theo thời gian tăng dần để matcher ghép tuần tự.
    /// </summary>
    public async Task<List<JourneySighting>> ReadAsync(
        string fromEquipmentId,
        string toEquipmentId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (fromUtc >= toUtc) return new List<JourneySighting>();

        var rows = await ReadAsync(db => db.Queryable<VdsTrafficData>()
            .Where(x => x.DetectTime >= fromUtc && x.DetectTime < toUtc)
            .Where(x => x.IsDelete == null)
            .Where(x => x.EquipmentId == fromEquipmentId || x.EquipmentId == toEquipmentId)
            .Where(x => x.LicensePlate != null && x.LicensePlate != "")
            .Select(x => new { x.EquipmentId, x.LicensePlate, x.DetectTime, x.VehicleSize })
            .OrderBy(x => x.DetectTime)
            .ToListAsync(ct), ct);

        return rows
            .Select(r => new JourneySighting(r.EquipmentId, r.LicensePlate, r.DetectTime, r.VehicleSize))
            .ToList();
    }

    /// <summary>
    /// Lượt xe của MỘT biển số tại hai camera đầu/cuối trong <c>[fromUtc, toUtc)</c> — dùng khi ghép theo bản
    /// tin (xe vừa tới camera cuối). Lọc theo biển THÔ đúng như thiết bị gửi: mọi camera cùng hãng nên cùng
    /// định dạng biển; ca lệch định dạng do lượt quét đối soát (<see cref="JourneyJob"/>, chuẩn hoá trong
    /// bộ nhớ) bắt lại. Kết quả chỉ vài dòng nên mỗi lượt xe tốn một truy vấn nhỏ trên đúng phân vùng ngày.
    /// </summary>
    public async Task<List<JourneySighting>> ReadPlateAsync(
        string fromEquipmentId,
        string toEquipmentId,
        string licensePlate,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (fromUtc >= toUtc || string.IsNullOrWhiteSpace(licensePlate)) return new List<JourneySighting>();

        var rows = await ReadAsync(db => db.Queryable<VdsTrafficData>()
            .Where(x => x.DetectTime >= fromUtc && x.DetectTime < toUtc)
            .Where(x => x.IsDelete == null)
            .Where(x => x.EquipmentId == fromEquipmentId || x.EquipmentId == toEquipmentId)
            .Where(x => x.LicensePlate == licensePlate)
            .Select(x => new { x.EquipmentId, x.LicensePlate, x.DetectTime, x.VehicleSize })
            .OrderBy(x => x.DetectTime)
            .ToListAsync(ct), ct);

        return rows
            .Select(r => new JourneySighting(r.EquipmentId, r.LicensePlate, r.DetectTime, r.VehicleSize))
            .ToList();
    }
}
