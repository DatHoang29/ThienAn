using System.Globalization;
using Modules.VDS.Core.Constants;
using Newtonsoft.Json.Linq;
using BaseMsg = Modules.VDS.Core.Exceptions.BaseMsg;
using Modules.VDS.Core.Dto;

namespace Modules.VDS.Core.Helper;

/// <summary>
/// Author: KienLT
/// Description: Luật THUẦN (không DB) tự sinh đoạn A→B từ km + tuyến + hướng của camera VDS.
///
/// <para><b>Cùng tuyến:</b> ghép mọi cặp camera CÙNG HƯỚNG; B nằm phía trước A theo chiều km của
/// (tuyến, hướng). Khoảng cách = |pA − pB|.</para>
///
/// <para><b>Khác tuyến:</b> CHỈ theo lượt rẽ khai ở nhóm <see cref="VdsConfigConst.Group.Segment"/> (<c>vds_turn_…</c>). A là camera cuối
/// trước nút trên (tuyến vào, hướng vào), B nằm sau nút trên (tuyến ra, hướng ra).
/// Khoảng cách = |pA − gốc vào| + |pB − gốc ra|; gốc lấy từ dòng nút <c>vds_junction_…</c>
/// để một con số không bị khai ở hai nơi. Chỉ tính qua MỘT nút.</para>
///
/// <para><b>Chiều km</b> (<c>vds_km_dir_…</c>): dòng <c>vds_km_dir_{tuyến}_{hướng}</c> thắng
/// dòng <c>vds_km_dir_default_{hướng}</c>; không có cả hai thì hướng 1 = desc, hướng 2 = asc.</para>
/// Created date: 29/09/2026
/// </summary>
public class VdsSegmentTopologyHelper
{
    /// <summary>Camera đầu vào của bộ sinh đoạn — chỉ các cột cần cho hình học tuyến.</summary>
    public sealed record TopoCamera(string Id, string Code, string? Route, int? Direction, int? Km, int? Met)
    {
        /// <summary>Vị trí quy về mét (km × 1000 + m); null khi thiếu km.</summary>
        public int? Position => Km is null ? null : Km.Value * 1000 + (Met ?? 0);
    }

    /// <summary>Một dòng VdsConfig của các nhóm hình học tuyến.</summary>
    public sealed record TopoConfigRow(string? GroupCode, string? Code, string? Value);

    /// <summary>
    /// Nút giao hai tuyến, đọc từ dòng <c>vds_junction_{Code}</c> = <c>{"a","aPos","b","bPos"}</c> bằng
    /// <see cref="ReadJunctions"/>. Bộ sinh đoạn (<see cref="Plan"/>) và API sơ đồ tuyến dùng CHUNG kiểu này để hai
    /// bên hiểu cấu hình nút như nhau.
    /// </summary>
    /// <param name="Code">Mã nút đã bỏ tiền tố (<c>1</c> của <c>vds_junction_1</c>) — đúng giá trị <c>junction</c> của lượt rẽ.</param>
    /// <param name="A">Tuyến a (mã <c>TmsEquipment.Location</c>, ví dụ <c>main_route</c>).</param>
    /// <param name="APos">Điểm neo trên tuyến a, mét (<c>"7+051"</c> ⇒ 7051).</param>
    /// <param name="B">Tuyến b (ví dụ <c>second_route</c>).</param>
    /// <param name="BPos">Điểm neo trên tuyến b, mét.</param>
    public sealed record TopoJunction(string Code, string A, int APos, string B, int BPos)
    {
        /// <summary>Điểm neo của <paramref name="route"/> tại nút; null nếu nút không nằm trên tuyến đó.</summary>
        public int? AnchorOf(string route) => route == A ? APos : route == B ? BPos : null;

        /// <summary>Nút nối đúng hai tuyến <paramref name="route1"/> và <paramref name="route2"/> (không kể thứ tự).</summary>
        public bool Connects(string route1, string route2)
            => (route1 == A && route2 == B) || (route1 == B && route2 == A);
    }

    /// <summary>Một đoạn A→B tính ra từ hình học tuyến.</summary>
    /// <param name="Code">Mã tất định ghi vào <c>TmsTrafficSegment.Code</c>.</param>
    /// <param name="Kind"><c>S</c> = cùng tuyến, <c>T</c> = rẽ qua nút.</param>
    /// <param name="Turn">Mã lượt rẽ (chỉ có khi <paramref name="Kind"/> = T).</param>
    /// <param name="Direction">Hướng ghi vào đoạn: cùng tuyến là hướng của lane, rẽ là HƯỚNG RA.</param>
    /// <param name="Formula">Diễn giải phép tính để người vận hành đối chiếu.</param>
    public sealed record PlannedSegment(
        string Code, string Kind, string? Turn,
        string FromId, string FromCode, string ToId, string ToCode,
        int Direction, int DistanceMeter, string Formula);

    /// <summary>
    /// Kết quả sinh đoạn.
    /// <para><b>Errors</b> = CẤU HÌNH sai (JSON hỏng, thiếu/sai kiểu trường, nút không tồn tại, trùng mã…): kết
    /// quả có thể THIẾU đoạn nên không được dùng để ghi DB hay thay cache đang chạy. <b>Warnings</b> = dữ liệu chưa
    /// đủ (camera thiếu km, lane chưa có camera…): kết quả vẫn dùng được.</para>
    /// <para>Cả hai là mã + tham số (<see cref="VdsSegmentTopologyMessageOutput"/>): WebAPI dịch theo ngôn ngữ request, worker ghi log
    /// tiếng Việt qua <see cref="FormatVi"/>.</para>
    /// </summary>
    public sealed record TopoPlan(List<PlannedSegment> Segments, List<VdsSegmentTopologyMessageOutput> Warnings, List<VdsSegmentTopologyMessageOutput> Errors,
        int PlausibleMaxSpeedKmh = VdsSegmentTopologyHelper.PlausibleMaxSpeedKmh)
    {
        /// <summary>Có lỗi cấu hình ⇒ không được áp kết quả.</summary>
        public bool HasErrors => Errors.Count > 0;

        /// <summary>Thời gian đi tối thiểu của một đoạn theo tốc độ trần của lượt tính này.</summary>
        public int MinTravelSeconds(int distanceMeter) => VdsSegmentTopologyHelper.MinTravelSeconds(distanceMeter, PlausibleMaxSpeedKmh);
    }

    /// <summary>Tiền tố mã đoạn cùng tuyến do máy sinh.</summary>
    public const string StraightPrefix = "S:";

    /// <summary>Tiền tố mã đoạn rẽ qua nút do máy sinh.</summary>
    public const string TurnPrefix = "T:";

    /// <summary>Mã chiều km mặc định: <c>default_{hướng}</c>.</summary>
    public const string DefaultRoute = "default";

    /// <summary>Tốc độ trần hợp lý MẶC ĐỊNH (km/h) để suy ra thời gian đi tối thiểu.</summary>
    public const int PlausibleMaxSpeedKmh = 200;

    /// <summary>Khoảng cho phép khi khai <c>vds_segment_plausible_max_speed</c> (km/h).</summary>
    public const int PlausibleMaxSpeedMin = 1, PlausibleMaxSpeedMax = 10000;

    /// <summary>Trên mức này là giá trị để TEST: vẫn áp nhưng cảnh báo, vì lọc ghép nhầm biển gần như tắt.</summary>
    public const int PlausibleMaxSpeedRealistic = 300;

    private const int CodeMaxLength = 64;

    /// <summary>Đoạn do máy sinh (sync được phép sửa/tắt).</summary>
    public static bool IsGenerated(string? code)
        => code is not null && (code.StartsWith(StraightPrefix, StringComparison.Ordinal)
                                || code.StartsWith(TurnPrefix, StringComparison.Ordinal));

    /// <summary>
    /// Đoạn bị KHÓA tay: có mã nhưng không phải mã máy sinh. Mã rỗng là dữ liệu cũ khai tay chưa đánh dấu —
    /// sync được tiếp quản.
    /// </summary>
    public static bool IsLocked(string? code) => !string.IsNullOrWhiteSpace(code) && !IsGenerated(code);

    /// <summary>Thời gian đi tối thiểu = khoảng cách / tốc độ trần, làm tròn lên, ít nhất 1 giây.</summary>
    public static int MinTravelSeconds(int distanceMeter, int plausibleMaxSpeedKmh = PlausibleMaxSpeedKmh)
        => Math.Max(VdsJourneyTravelConst.CodeMinSeconds,
            (int)Math.Ceiling(distanceMeter / (Math.Max(1, plausibleMaxSpeedKmh) / 3.6)));

    /// <summary>Vị trí từ số mét (<c>7051</c>) hoặc dạng km+m (<c>7+051</c>, <c>7+51</c> = 7.051 m).</summary>
    public static int? ParsePosition(JToken? token)
    {
        if (token is null || token.Type == JTokenType.Null) return null;
        if (token.Type == JTokenType.Integer) return token.Value<int>();
        var raw = token.ToString().Trim();
        var plus = raw.IndexOf('+');
        if (plus < 0)
            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var m) ? m : null;
        return int.TryParse(raw[..plus], NumberStyles.Integer, CultureInfo.InvariantCulture, out var km)
               && int.TryParse(raw[(plus + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var met)
               && met is >= 0 and < 1000
            ? km * 1000 + met
            : null;
    }

    /// <summary>
    /// Sinh toàn bộ đoạn từ danh sách camera và cấu hình hình học. KHÔNG NÉM với cấu hình sai: mọi lỗi đọc
    /// cấu hình đi vào <see cref="TopoPlan.Errors"/>, bên gọi quyết định không áp kết quả.
    /// </summary>
    public static TopoPlan Plan(IEnumerable<TopoCamera> cameras, IEnumerable<TopoConfigRow> config)
    {
        var warnings = new List<VdsSegmentTopologyMessageOutput>();
        var errors = new List<VdsSegmentTopologyMessageOutput>();
        void Error(string key, params object[] arguments) => errors.Add(new VdsSegmentTopologyMessageOutput(key, arguments));
        void Warning(string key, params object[] arguments) => warnings.Add(new VdsSegmentTopologyMessageOutput(key, arguments));
        // Chỉ nhóm vds_segment; loại dòng theo tiền tố Code, phần sau tiền tố là khoá (default_1, 1, 1_1…).
        var rows = config.Where(r => r.GroupCode == VdsConfigConst.Group.Segment).ToList();
        var turnRows = Keyed(rows, VdsConfigConst.Pattern.TurnPrefix);
        foreach (var r in rows.Where(r => !HasPrefix(r.Code, VdsConfigConst.Pattern.KmDirPrefix)
                     && !HasPrefix(r.Code, VdsConfigConst.Pattern.JunctionPrefix) && !HasPrefix(r.Code, VdsConfigConst.Pattern.TurnPrefix)
                     && r.Code?.Trim() != VdsConfigConst.Code.SegmentPlausibleMaxSpeed))
            Error(BaseMsg.Vds.Message.TopologyConfigCodeInvalid, r.Code ?? "", VdsConfigConst.Pattern.KmDirPrefix,
                VdsConfigConst.Pattern.JunctionPrefix, VdsConfigConst.Pattern.TurnPrefix);
        var plausibleSpeed = ReadPlausibleSpeed(rows, warnings, errors);
        var orientation = VdsRouteOrientationHelper.Create(rows, errors);

        // Camera dùng được: đủ km, tuyến, hướng. Còn lại liệt kê để người vận hành bổ sung.
        var usable = new List<TopoCamera>();
        foreach (var c in cameras)
        {
            if (c.Position is null || string.IsNullOrWhiteSpace(c.Route) || c.Direction is null)
                Warning(BaseMsg.Vds.Message.TopologyCameraIncomplete, c.Code);
            else usable.Add(c with { Route = c.Route.Trim() });
        }

        int Sign(string route, int dir) => orientation(route, dir);
        var lanes = usable.GroupBy(c => (c.Route!, c.Direction!.Value))
            .ToDictionary(g => g.Key,
                g => g.OrderBy(c => Sign(g.Key.Item1, g.Key.Item2) * c.Position!.Value).ThenBy(c => c.Code).ToList());

        var result = new Dictionary<(string, string), PlannedSegment>();
        void Add(PlannedSegment s)
        {
            var key = (s.FromId, s.ToId);
            if (result.TryGetValue(key, out var old))
            {
                Error(BaseMsg.Vds.Message.TopologyPairMultiplePaths, s.FromCode, s.ToCode, old.Code, s.Code);
                return;
            }
            result[key] = s;
        }

        // Mọi cặp có thứ tự: giữ đoạn liền kề và bổ sung đo trực tiếp hai đầu đoạn dài.
        foreach (var ((route, dir), list) in lanes)
        {
            for (var i = 0; i + 1 < list.Count; i++)
            for (var j = i + 1; j < list.Count; j++)
            {
                var a = list[i];
                var b = list[j];
                var d = Math.Abs(a.Position!.Value - b.Position!.Value);
                if (d == 0)
                {
                    Warning(BaseMsg.Vds.Message.TopologyCameraSameKm, a.Code, b.Code, route, dir);
                    continue;
                }
                Add(new PlannedSegment(MakeCode(StraightPrefix, a.Code + ">" + b.Code, a.Id, b.Id), "S", null,
                    a.Id, a.Code, b.Id, b.Code, dir, d, $"|{a.Position} − {b.Position}| = {d}"));
            }
        }

        // ===== Khác tuyến: chỉ theo lượt rẽ đã khai =====
        // Cùng hàm đọc nút với API sơ đồ tuyến; gọi đúng chỗ cũ nên thứ tự Errors không đổi.
        var junctions = ReadJunctions(rows, errors);
        var turnCodes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in turnRows)
        {
            var code = row.Code?.Trim() ?? "";
            var rowCode = VdsConfigConst.Pattern.TurnPrefix + code;
            // try THEO TỪNG DÒNG: một dòng sai không được làm mất cả lượt tính.
            try
            {
                if (code.Length == 0) { Error(BaseMsg.Vds.Message.TopologyTurnMissingCode); continue; }
                if (!turnCodes.Add(code)) { Error(BaseMsg.Vds.Message.TopologyTurnDuplicate, rowCode); continue; }
                if (!TryParseObject(row.Value, out var turn))
                {
                    Error(BaseMsg.Vds.Message.TopologyTurnInvalidJson, rowCode);
                    continue;
                }

                var jCode = GetString(turn, "junction");
                var inRoute = GetString(turn, "in");
                var outRoute = GetString(turn, "out");
                var inDir = GetInt(turn, "inDir");
                var outDir = GetInt(turn, "outDir");
                if (jCode is null || inRoute is null || outRoute is null || inDir is null || outDir is null)
                {
                    Error(BaseMsg.Vds.Message.TopologyTurnInvalidFields, rowCode);
                    continue;
                }
                if (!junctions.TryGetValue(jCode, out var j))
                {
                    Error(BaseMsg.Vds.Message.TopologyTurnJunctionNotFound, rowCode, VdsConfigConst.Pattern.JunctionPrefix + jCode);
                    continue;
                }
                var inAnchor = j.AnchorOf(inRoute);
                var outAnchor = j.AnchorOf(outRoute);
                if (inAnchor is null || outAnchor is null || inRoute == outRoute)
                {
                    Error(BaseMsg.Vds.Message.TopologyTurnRoutesInvalid, rowCode, jCode);
                    continue;
                }

                var sIn = Sign(inRoute, inDir.Value);
                var sOut = Sign(outRoute, outDir.Value);
                var inLane = lanes.GetValueOrDefault((inRoute, inDir.Value)) ?? new List<TopoCamera>();
                var outLane = lanes.GetValueOrDefault((outRoute, outDir.Value)) ?? new List<TopoCamera>();

                // Toạ độ theo chiều xe chạy: t = dấu × vị trí. "Trước nút" là t ≤ t_nút, "sau nút" là t ≥ t_nút.
                var starts = inLane.Where(c => sIn * c.Position!.Value <= sIn * inAnchor.Value).ToList();
                var ends = outLane.Where(c => sOut * c.Position!.Value >= sOut * outAnchor.Value).ToList();
                if (starts.Count == 0)
                {
                    Warning(inLane.Count > 0 ? BaseMsg.Vds.Message.TopologyTurnInboundMismatch : BaseMsg.Vds.Message.TopologyTurnNoCamera,
                        rowCode, inRoute, inDir.Value);
                    continue;
                }
                if (ends.Count == 0)
                {
                    Warning(outLane.Count > 0 ? BaseMsg.Vds.Message.TopologyTurnOutboundMismatch : BaseMsg.Vds.Message.TopologyTurnNoCamera,
                        rowCode, outRoute, outDir.Value);
                    continue;
                }

                foreach (var a in starts)
                foreach (var b in ends)
                {
                    var dIn = Math.Abs(a.Position!.Value - inAnchor.Value);
                    var dOut = Math.Abs(b.Position!.Value - outAnchor.Value);
                    if (dIn + dOut == 0) continue;
                    Add(new PlannedSegment(MakeCode(TurnPrefix, code + ":" + a.Code + ">" + b.Code, a.Id, b.Id), "T", code,
                        a.Id, a.Code, b.Id, b.Code, outDir.Value, dIn + dOut,
                        $"|{a.Position} − {inAnchor}| + |{b.Position} − {outAnchor}| = {dIn} + {dOut} = {dIn + dOut}"));
                }
            }
            catch (Exception ex)
            {
                Error(BaseMsg.Vds.Message.TopologyTurnUnreadable, rowCode, ex.GetType().Name);
            }
        }

        // Mã đoạn là ĐỊNH DANH (SegmentId của worker, ID bản ghi khi Sync) nên phải duy nhất.
        foreach (var dup in result.Values.GroupBy(s => s.Code, StringComparer.Ordinal).Where(g => g.Count() > 1))
            Error(BaseMsg.Vds.Message.TopologySegmentCodeDuplicate, dup.Key, dup.Count());

        return new TopoPlan(result.Values.OrderBy(s => s.Kind).ThenBy(s => s.Code, StringComparer.Ordinal).ToList(),
            warnings, errors, plausibleSpeed);
    }

    /// <summary>Tốc độ trần hợp lý từ <c>vds_segment_plausible_max_speed</c>; thiếu ⇒ mặc định, sai ⇒ Errors.</summary>
    private static int ReadPlausibleSpeed(List<TopoConfigRow> rows, List<VdsSegmentTopologyMessageOutput> warnings,
        List<VdsSegmentTopologyMessageOutput> errors)
    {
        var row = rows.LastOrDefault(r => r.Code?.Trim() == VdsConfigConst.Code.SegmentPlausibleMaxSpeed);
        if (row is null) return PlausibleMaxSpeedKmh;
        if (!int.TryParse(row.Value?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)
            || v is < PlausibleMaxSpeedMin or > PlausibleMaxSpeedMax)
        {
            errors.Add(new VdsSegmentTopologyMessageOutput(BaseMsg.Vds.Message.TopologyPlausibleSpeedInvalid,
                VdsConfigConst.Code.SegmentPlausibleMaxSpeed, row.Value ?? "", PlausibleMaxSpeedMin, PlausibleMaxSpeedMax));
            return PlausibleMaxSpeedKmh;
        }
        if (v > PlausibleMaxSpeedRealistic)
            warnings.Add(new VdsSegmentTopologyMessageOutput(BaseMsg.Vds.Message.TopologyPlausibleSpeedUnrealistic,
                VdsConfigConst.Code.SegmentPlausibleMaxSpeed, v, PlausibleMaxSpeedRealistic));
        return v;
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Định dạng thông báo của bộ sinh đoạn bằng mẫu tiếng Việt (<see cref="VdsSegmentTopologyViTextConst"/>) — cho worker
    /// ghi log; WebAPI dịch theo ngôn ngữ request thay vì gọi hàm này.
    /// Created date: 22/10/2026
    /// </summary>
    public static string FormatVi(VdsSegmentTopologyMessageOutput message) => VdsSegmentTopologyViTextConst.Format(message);

    /// <summary>
    /// Author: KienLT
    /// Description: Đọc các nút giao <c>vds_junction_…</c> của nhóm <see cref="VdsConfigConst.Group.Segment"/> (dòng nhóm
    /// khác và dòng không mang tiền tố nút bị bỏ qua). Hàm DUY NHẤT parse JSON nút: <see cref="Plan"/> và API sơ đồ tuyến
    /// cùng gọi. KHÔNG NÉM với cấu hình sai: dòng hỏng (JSON sai, thiếu trường, hai tuyến trùng nhau, trùng mã) ghi vào
    /// <paramref name="errors"/> và không có trong kết quả.
    /// Created date: 20/10/2026
    /// </summary>
    /// <returns>Nút hợp lệ theo mã đã bỏ tiền tố (so khớp ordinal).</returns>
    public static Dictionary<string, TopoJunction> ReadJunctions(IEnumerable<TopoConfigRow> config, List<VdsSegmentTopologyMessageOutput> errors)
    {
        var rows = Keyed(config.Where(r => r.GroupCode == VdsConfigConst.Group.Segment).ToList(),
            VdsConfigConst.Pattern.JunctionPrefix);
        var map = new Dictionary<string, TopoJunction>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            var code = row.Code?.Trim() ?? "";
            var rowCode = VdsConfigConst.Pattern.JunctionPrefix + code;
            try
            {
                if (!TryParseObject(row.Value, out var o))
                {
                    errors.Add(new VdsSegmentTopologyMessageOutput(BaseMsg.Vds.Message.TopologyJunctionInvalidJson, rowCode));
                    continue;
                }
                var a = GetString(o, "a");
                var b = GetString(o, "b");
                var aPos = ParsePosition(o["aPos"]);
                var bPos = ParsePosition(o["bPos"]);
                if (code.Length == 0 || a is null || b is null || aPos is null || bPos is null || a == b)
                {
                    errors.Add(new VdsSegmentTopologyMessageOutput(BaseMsg.Vds.Message.TopologyJunctionInvalidFields, rowCode));
                    continue;
                }
                if (map.ContainsKey(code))
                {
                    errors.Add(new VdsSegmentTopologyMessageOutput(BaseMsg.Vds.Message.TopologyJunctionDuplicate, rowCode));
                    continue;
                }
                map[code] = new TopoJunction(code, a, aPos.Value, b, bPos.Value);
            }
            catch (Exception ex)
            {
                errors.Add(new VdsSegmentTopologyMessageOutput(BaseMsg.Vds.Message.TopologyJunctionUnreadable, rowCode, ex.GetType().Name));
            }
        }
        return map;
    }

    /// <summary>
    /// Mã đoạn tất định, tối đa 64 ký tự. Quá dài thì giữ phần đầu để còn đọc được và thêm hash của cặp ID
    /// camera, để hai cặp khác nhau không bao giờ bị cắt về cùng một mã.
    /// </summary>
    public static string MakeCode(string prefix, string body, string fromId, string toId)
    {
        var code = prefix + body;
        if (code.Length <= CodeMaxLength) return code;
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(fromId + "|" + toId + "|" + code))).ToLowerInvariant()[..12];
        return code[..(CodeMaxLength - hash.Length - 1)] + "#" + hash;
    }

    private static bool HasPrefix(string? code, string prefix)
        => code is not null && code.Trim().StartsWith(prefix, StringComparison.Ordinal) && code.Trim().Length > prefix.Length;

    /// <summary>Dòng có tiền tố <paramref name="prefix"/>, Code đã bỏ tiền tố (còn lại khoá: default_1, 1, 1_1…).</summary>
    private static List<TopoConfigRow> Keyed(List<TopoConfigRow> rows, string prefix)
        => rows.Where(r => HasPrefix(r.Code, prefix))
            .Select(r => r with { Code = r.Code!.Trim()[prefix.Length..] }).ToList();

    private static bool TryParseObject(string? raw, out JObject obj)
    {
        obj = null!;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        try
        {
            if (JToken.Parse(raw) is not JObject parsed) return false;
            obj = parsed;
            return true;
        }
        catch (Newtonsoft.Json.JsonException) { return false; }
    }

    /// <summary>Chuỗi không rỗng, hoặc null nếu thiếu / không phải chuỗi.</summary>
    private static string? GetString(JObject o, string key)
        => o[key] is { Type: JTokenType.String } t && !string.IsNullOrWhiteSpace((string?)t) ? ((string)t!).Trim() : null;

    /// <summary>Số nguyên (nhận cả "2"), hoặc null nếu thiếu / sai kiểu — KHÔNG ném.</summary>
    private static int? GetInt(JObject o, string key) => o[key] switch
    {
        { Type: JTokenType.Integer } t => (long)t is >= int.MinValue and <= int.MaxValue ? (int)(long)t : null,
        { Type: JTokenType.String } t => int.TryParse((string?)t, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null,
        _ => null,
    };
}
