using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Services.Vds.Shared.Domain;

namespace Services.Vds.Catalog;

/// <summary>Kết cục một lượt đồng bộ ngưỡng làn — dùng để log đúng mức, không coi mọi thứ là lỗi.</summary>
public enum AidLaneSyncOutcome
{
    /// <summary>Không có tập làn xác định để ghi. KHÔNG phải lỗi: camera chưa xác định làn thì bỏ qua là đúng.</summary>
    Skipped,

    /// <summary>Mọi làn đích đã mang đúng ngưỡng ⇒ không gửi PUT.</summary>
    AlreadyMatches,

    /// <summary>Đã ghi và đã đối chiếu lại thành công.</summary>
    Updated,
}

/// <param name="Outcome">Kết cục.</param>
/// <param name="Detail">Vì sao — nguồn xác định làn, hoặc lý do bỏ qua.</param>
/// <param name="Lanes">Tập làn đích của lượt này.</param>
public sealed record AidLaneSyncResult(
    AidLaneSyncOutcome Outcome, string Detail, IReadOnlyList<string> Lanes)
{
    internal static AidLaneSyncResult Skip(string detail) =>
        new(AidLaneSyncOutcome.Skipped, detail, Array.Empty<string>());
}

/// <summary>
/// Author: KienLT
/// Description: Đồng bộ ngưỡng vận tốc tối thiểu/tối đa lên từng làn của một camera AID
/// (<c>GET/PUT /api/v1/cameras/by-ip/{ip}/lanes</c>), theo đúng NGUỒN XÁC ĐỊNH LÀN mà máy chủ khai.
///
/// <para><b>Vì sao phải đọc cả envelope, không chỉ mảng <c>lanes</c> (sửa 25/09/2026).</b> Bản cũ lấy tập
/// làn đích = đúng những làn ĐÃ có cấu hình. Hai hệ quả, cả hai đều quan sát được trên máy thật:
/// <list type="bullet">
/// <item>Làn đã có dữ liệu nhưng chưa từng cấu hình thì vĩnh viễn không được đặt ngưỡng — camera
/// <c>lane_count_source=observed</c> với <c>observed_lanes</c> 1–4 mà <c>lanes</c> chỉ 1–3.</item>
/// <item>Camera <c>lane_count_source=processor</c> còn sót cấu hình của làn ngoài <c>1..lane_count</c>: API
/// v1.8 mục 4.7 trả <b>422 và KHÔNG ghi gì cả</b> — cả lượt đồng bộ mất trắng vì một làn lạc.</item>
/// </list></para>
///
/// <para><b>Tập làn đích theo nguồn</b> (<see cref="ResolveTargetLanes"/>):
/// <list type="bullet">
/// <item><c>processor</c> — đúng khoá <c>"1"</c>…<c>"lane_count"</c>. Đây là số làn hộp xử lý AI đang chia
/// và cũng là số mà <c>lane_no.</c> trên sự kiện được đánh theo. KHÔNG lấy làn lịch sử ngoài khoảng:
/// <c>observed_lanes</c> là thống kê 7 ngày nên còn giữ làn của topology cũ.</item>
/// <item><c>observed</c> — hợp của làn đã cấu hình và làn đã quan sát, để bổ sung ngưỡng còn thiếu.</item>
/// <item>nguồn khác (<c>unknown</c>, <c>twin</c>, hoặc giá trị mới chưa hiểu rõ) — chỉ những làn ĐÃ cấu
/// hình. Không tự tạo làn theo một nguồn mà ta chưa biết ngữ nghĩa.</item>
/// </list></para>
///
/// <para><b>Giữ nguyên phần máy chủ sở hữu.</b> Làn đã có được <c>DeepClone</c> rồi chỉ ghi đè min/max, nên
/// <c>emergency</c> (mục 4.7 dựa vào cờ này để phát Occupy Emergency Lane) và <c>lane_name</c> không bị mất.
/// <c>updated_at</c> bị bỏ khỏi payload vì đó là trường audit của máy chủ. Làn mới thêm chỉ mang
/// <c>lane</c> + min/max: không bịa cờ mà ta không biết.</para>
///
/// <para><b>Đối chiếu lại theo TẬP LÀN ĐÃ YÊU CẦU</b>, không đòi hai lần GET giống nhau từng phần tử: cấu
/// hình có thể được người khác sửa đồng thời, coi đó là lỗi thì lượt nào cũng báo lỗi.</para>
///
/// <para><b>PUT chỉ cập nhật làn có tên trong body</b> (mục 4.7, khác ngữ nghĩa PUT thông thường) nên làn
/// ngoài tập đích được để nguyên — đúng điều ta muốn với làn lạc của camera processor.</para>
///
/// <para><b>Không xử lý DELETE/409 và không gửi min/max <c>null</c>.</b> Luồng này chỉ ĐẶT ngưỡng; bỏ giới
/// hạn (mục 4.8) là tính năng riêng, thêm vào đây chỉ làm rộng bề mặt mà không ai gọi.</para>
/// Created date: 26/08/2026 (đọc nguồn xác định làn 25/09/2026)
/// </summary>
public static class AidLaneSpeedSync
{
    private const string SourceProcessor = "processor";
    private const string SourceObserved = "observed";

    /// <summary>
    /// Số lượt thử. 422 nghĩa là topology đã khác lúc GET, nên nạp lại rồi thử MỘT lần nữa; vẫn 422 thì
    /// ném để lượt refresh sau xử lý — quay vòng ở đây chỉ nhân số lần ghi vào nhật ký của máy chủ.
    /// </summary>
    private const int MaxAttempts = 2;

    public static async Task<AidLaneSyncResult> SyncAsync(HttpClient http, Uri uri, SpeedRange range,
        Action<HttpRequestMessage> authenticate, CancellationToken ct)
    {
        if (!range.IsValid) throw new ArgumentException("Invalid speed range.", nameof(range));

        for (var attempt = 1; ; attempt++)
        {
            var topology = await ReadAsync(http, uri, authenticate, ct).ConfigureAwait(false);
            var target = ResolveTargetLanes(topology, out var skipReason);
            if (target.Count == 0) return AidLaneSyncResult.Skip(skipReason);

            var detail = $"lane_count_source={topology.Source}, lane_count={topology.LaneCount}";
            if (target.All(lane => Matches(topology.Lane(lane), range)))
                return new AidLaneSyncResult(AidLaneSyncOutcome.AlreadyMatches, detail, target);

            using var put = new HttpRequestMessage(HttpMethod.Put, uri)
            {
                Content = new StringContent(
                    BuildPayload(topology, target, range).ToJsonString(), Encoding.UTF8, "application/json"),
            };
            authenticate(put);
            using var response = await http.SendAsync(put, ct).ConfigureAwait(false);

            // 422 = làn nằm ngoài khoảng của hộp xử lý AI. Nội dung lỗi của máy chủ nêu ĐÚNG làn nào sai và
            // khoảng hợp lệ, nên phải đọc ra: không đọc thì chỉ biết "422" mà không biết sửa gì.
            if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
            {
                var body = await ReadErrorAsync(response, ct).ConfigureAwait(false);
                if (attempt >= MaxAttempts)
                {
                    throw new InvalidDataException(
                        $"AID từ chối (422) ngưỡng cho làn [{string.Join(",", target)}] ({detail}) sau " +
                        $"{MaxAttempts} lượt; máy chủ trả: {body}");
                }

                continue; // Nạp lại topology rồi tính lại tập làn đích.
            }

            response.EnsureSuccessStatusCode();

            var after = await ReadAsync(http, uri, authenticate, ct).ConfigureAwait(false);
            var pending = target.Where(lane => !Matches(after.Lane(lane), range)).ToArray();
            if (pending.Length > 0)
            {
                throw new InvalidDataException(
                    $"Đối chiếu lại thất bại: làn [{string.Join(",", pending)}] chưa mang ngưỡng {range} " +
                    $"({detail}).");
            }

            return new AidLaneSyncResult(AidLaneSyncOutcome.Updated, detail, target);
        }
    }

    /// <summary>Ảnh chụp cấu hình làn của một camera, đọc từ <c>GET .../lanes</c>.</summary>
    internal sealed record AidLaneTopology(
        string Source, int LaneCount, IReadOnlyList<string> Observed,
        IReadOnlyList<string> Configured, JsonArray Lanes)
    {
        /// <summary>Đối tượng làn theo khoá, hoặc null khi làn đó chưa có cấu hình.</summary>
        internal JsonObject? Lane(string key) => Lanes
            .OfType<JsonObject>()
            .FirstOrDefault(lane => string.Equals(
                lane["lane"]?.ToString()?.Trim(), key, StringComparison.Ordinal));
    }

    /// <summary>
    /// Tập làn cần mang ngưỡng. Rỗng ⇒ bỏ qua camera kèm lý do trong <paramref name="skipReason"/>; đó là
    /// trạng thái BÌNH THƯỜNG chứ không phải lỗi.
    /// </summary>
    internal static IReadOnlyList<string> ResolveTargetLanes(
        AidLaneTopology topology, out string skipReason)
    {
        skipReason = string.Empty;

        switch (topology.Source)
        {
            case SourceProcessor:
                if (topology.LaneCount < 1)
                {
                    skipReason = "lane_count_source=processor nhưng lane_count chưa xác định ⇒ không tự tạo làn";
                    return Array.Empty<string>();
                }

                // Khoá viết như sự kiện mang: "2", không phải "02" (mục 4.7).
                return Enumerable.Range(1, topology.LaneCount)
                    .Select(n => n.ToString(CultureInfo.InvariantCulture))
                    .ToArray();

            case SourceObserved:
            {
                var lanes = topology.Configured.Concat(topology.Observed)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(key => key, StringComparer.Ordinal)
                    .ToArray();
                if (lanes.Length == 0)
                    skipReason = "lane_count_source=observed nhưng chưa có làn nào được cấu hình hay quan sát";
                return lanes;
            }

            default:
                if (topology.Configured.Count == 0)
                {
                    skipReason = $"lane_count_source={topology.Source} và chưa có làn nào được cấu hình " +
                        "⇒ không tự tạo làn";
                }

                return topology.Configured;
        }
    }

    /// <summary>
    /// Body của PUT: chỉ gồm làn đích. Làn đã có thì clone để giữ <c>emergency</c>/<c>lane_name</c>; làn mới
    /// chỉ mang khoá và hai ngưỡng.
    /// </summary>
    internal static JsonObject BuildPayload(
        AidLaneTopology topology, IReadOnlyList<string> target, SpeedRange range)
    {
        var desired = new JsonArray();
        foreach (var key in target)
        {
            var lane = topology.Lane(key) is JsonObject existing
                ? (JsonObject)existing.DeepClone()
                : new JsonObject { ["lane"] = key };

            lane.Remove("updated_at"); // Trường audit của máy chủ.
            lane["min_speed_kmh"] = range.Min;
            lane["max_speed_kmh"] = range.Max;
            desired.Add(lane);
        }

        return new JsonObject { ["lanes"] = desired };
    }

    private static async Task<AidLaneTopology> ReadAsync(
        HttpClient http, Uri uri, Action<HttpRequestMessage> authenticate, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        authenticate(request);
        using var response = await http.SendAsync(request, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = JsonNode.Parse(await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
        var lanes = json?["lanes"] as JsonArray
            ?? throw new InvalidDataException("AID response has no lanes array.");

        // Phần tử sai kiểu hoặc thiếu khoá là hợp đồng bị phá — khác hẳn với "mảng rỗng", vốn chỉ nghĩa là
        // chưa cấu hình làn nào (mục 4.6).
        if (lanes.Any(x => x is not JsonObject || string.IsNullOrWhiteSpace(x!["lane"]?.ToString())))
            throw new InvalidDataException("AID lanes[] có phần tử thiếu khoá 'lane'.");

        var configured = lanes.Select(x => x!["lane"]!.ToString().Trim()).ToArray();
        if (configured.Distinct(StringComparer.Ordinal).Count() != configured.Length)
            throw new InvalidDataException("Duplicate AID lane identities.");

        var observed = (json?["observed_lanes"] as JsonArray)?
            .Select(x => x?.ToString()?.Trim())
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Select(key => key!)
            .ToArray() ?? Array.Empty<string>();

        return new AidLaneTopology(
            Source: (json?["lane_count_source"]?.ToString() ?? "unknown").Trim().ToLowerInvariant(),
            LaneCount: json?["lane_count"]?.GetValue<int>() ?? 0,
            Observed: observed,
            Configured: configured,
            Lanes: lanes);
    }

    private static bool Matches(JsonObject? lane, SpeedRange range) =>
        lane is not null
        && lane["min_speed_kmh"]?.GetValue<double>() == range.Min
        && lane["max_speed_kmh"]?.GetValue<double>() == range.Max;

    /// <summary>Nội dung lỗi của máy chủ, cắt bớt để một body dài không tràn dòng log.</summary>
    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var body = (await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false)).Trim();
            if (body.Length == 0) return "(body rỗng)";
            return body.Length <= 500 ? body : body[..500] + "…";
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return "(không đọc được body)";
        }
    }
}
