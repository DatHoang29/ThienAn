using Modules.VDS.Core.Helper;
using System.Globalization;
using Modules.TMS.Core.Entities;
using Modules.VDS.Controllers.Segment.Dto;
using BaseMsg = Modules.VDS.Core.Exceptions.BaseMsg;
using Modules.VDS.Core.Dto;

namespace Modules.VDS.Infrastructure.Services.Segment.Rules;

/// <summary>
/// Author: KienLT
/// Description: Đồng bộ kết quả tự sinh đoạn (<see cref="VdsBaseHelper.SegmentTopologyHp"/>, nằm ở Modules.VDS.Core để
/// worker dùng chung) vào bảng <c>TmsTrafficSegment</c>. Lớp này lo phần so khớp với DB và (từ 20/10/2026) dựng sơ đồ
/// tuyến cho API <c>Topology</c> (<see cref="BuildTopology"/>, <see cref="BuildPath"/>) — đều là hàm thuần, không DB.
/// Created date: 29/09/2026
/// </summary>
public static class VdsSegmentTopologyRules
{
    /// <summary>Tính đoạn — uỷ quyền cho bản dùng chung ở Core.</summary>
    public static VdsBaseHelper.SegmentTopologyHp.TopoPlan Plan(IEnumerable<VdsBaseHelper.SegmentTopologyHp.TopoCamera> cameras, IEnumerable<VdsBaseHelper.SegmentTopologyHp.TopoConfigRow> config)
        => VdsBaseHelper.SegmentTopologyHp.Plan(cameras, config);

    private static bool IsLocked(string? code) => VdsBaseHelper.SegmentTopologyHp.IsLocked(code);



    /// <summary>Một camera VDS đọc từ DB, trước khi lọc.</summary>
    public sealed record CameraRow(string Id, string Code, string? Ip, string? Location, int? Direction, int? Km, int? Met);

    /// <summary>
    /// Chọn camera đầu vào theo ĐÚNG luật nạp của worker (<c>Services.Vds/Catalog/CameraManager</c>):
    /// <list type="bullet">
    /// <item>Danh sách <c>vds_list_equipment</c> RỖNG ⇒ 0 camera (worker coi rỗng là dừng hẳn, không phải "lấy hết").</item>
    /// <item>Camera thiếu IP bị loại — worker không nhận diện được bản tin của nó.</item>
    /// <item>IP trùng ⇒ giữ camera có ID lớn nhất theo thứ tự ordinal (worker nạp theo cùng thứ tự đó).</item>
    /// </list>
    /// Lệch luật này thì Preview/Sync và đoạn worker tự sinh ra hai kết quả khác nhau (ví dụ camera giữa tuyến
    /// thiếu IP: Preview có A→B, B→C còn worker có A→C).
    /// </summary>
    /// <param name="formatMessage">Dịch mã cảnh báo theo ngôn ngữ request; null ⇒ giữ nguyên mã.</param>
    public static List<VdsBaseHelper.SegmentTopologyHp.TopoCamera> SelectCameras(IReadOnlyList<CameraRow> rows, IReadOnlyCollection<string?> listed,
        List<string> warnings, Func<VdsSegmentTopologyMessageOutput, string>? formatMessage = null)
    {
        string Format(string key, params object[] arguments)
            => formatMessage?.Invoke(new(key, arguments)) ?? key;
        var ids = listed.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (ids.Count == 0)
        {
            warnings.Add(Format(BaseMsg.Vds.Message.TopologyNoEquipment));
            return new List<VdsBaseHelper.SegmentTopologyHp.TopoCamera>();
        }

        var picked = rows.Where(r => ids.Contains(r.Id)).ToList();
        foreach (var r in picked.Where(r => string.IsNullOrWhiteSpace(r.Ip)))
            warnings.Add(Format(BaseMsg.Vds.Message.TopologyCameraMissingIp, r.Code));

        var result = new List<VdsBaseHelper.SegmentTopologyHp.TopoCamera>();
        foreach (var g in picked.Where(r => !string.IsNullOrWhiteSpace(r.Ip))
                     .GroupBy(r => r.Ip!.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            var keep = g.OrderBy(r => r.Id, StringComparer.Ordinal).Last();
            if (g.Count() > 1)
                warnings.Add(Format(BaseMsg.Vds.Message.TopologyDuplicateIp, g.Key, g.Count(), string.Join(", ", g.Select(r => r.Code)), keep.Code));
            result.Add(new VdsBaseHelper.SegmentTopologyHp.TopoCamera(keep.Id, keep.Code, keep.Location, keep.Direction, keep.Km, keep.Met));
        }
        return result;
    }

    /// <summary>Hành động đồng bộ cho một đoạn.</summary>
    public static class SyncAction
    {
        public const string Insert = "insert";
        public const string Update = "update";
        public const string Unchanged = "unchanged";
        public const string Disable = "disable";
        public const string Locked = "locked";
    }

    /// <summary>
    /// So kết quả sinh với <c>TmsTrafficSegment</c> đang có (chưa xoá) theo cặp (From, To).
    ///
    /// <para>Đoạn KHÓA tay (<see cref="IsLocked"/>) không bị động vào. Đoạn máy sinh hoặc đoạn cũ mã rỗng thì
    /// được cập nhật khoảng cách/hướng/biên dưới và bật lại; nếu không còn trong kết quả thì tắt (Status 0),
    /// KHÔNG xoá, vì <c>ID</c> nằm trong <c>EventId</c> của sự cố. Tên và biên trên đang có được giữ.</para>
    /// </summary>
    /// <param name="maxTravelDefault">Biên trên mặc định cho đoạn chưa khai.</param>
    public static List<SegmentSyncItem> Diff(IReadOnlyList<VdsBaseHelper.SegmentTopologyHp.PlannedSegment> plan,
        IReadOnlyList<TmsTrafficSegment> existing, int maxTravelDefault,
        int plausibleMaxSpeedKmh = VdsBaseHelper.SegmentTopologyHp.PlausibleMaxSpeedKmh)
    {
        var items = new List<SegmentSyncItem>();
        var byPair = existing
            .Where(x => x.FromEquipmentId != null && x.ToEquipmentId != null)
            .GroupBy(x => (x.FromEquipmentId!, x.ToEquipmentId!))
            .ToDictionary(g => g.Key, g => g.First());
        var matched = new HashSet<string>(StringComparer.Ordinal);
        var nextOrder = existing.Select(x => x.OrderNo ?? 0).DefaultIfEmpty(0).Max();

        foreach (var p in plan)
        {
            var min = VdsBaseHelper.SegmentTopologyHp.MinTravelSeconds(p.DistanceMeter, plausibleMaxSpeedKmh);
            if (!byPair.TryGetValue((p.FromId, p.ToId), out var row))
            {
                var max = Math.Max(min, maxTravelDefault);
                items.Add(new SegmentSyncItem(SyncAction.Insert, p, null, new TmsTrafficSegment
                {
                    // ID = mã tất định = SegmentId worker dùng khi đoạn còn là đoạn tự sinh.
                    ID = p.Code,
                    Code = p.Code,
                    Name = DefaultName(p),
                    FromEquipmentId = p.FromId,
                    ToEquipmentId = p.ToId,
                    Direction = p.Direction.ToString(CultureInfo.InvariantCulture),
                    DistanceMeter = p.DistanceMeter,
                    MinTravelSeconds = min,
                    MaxTravelSeconds = max,
                    Status = TmsTrafficSegment.StatusEnabled,
                    OrderNo = ++nextOrder,
                }));
                continue;
            }

            matched.Add(row.ID);
            if (IsLocked(row.Code))
            {
                items.Add(new SegmentSyncItem(SyncAction.Locked, p, row, row));
                continue;
            }

            var target = new TmsTrafficSegment
            {
                ID = row.ID,
                Code = p.Code,
                Name = string.IsNullOrWhiteSpace(row.Name) ? DefaultName(p) : row.Name,
                FromEquipmentId = row.FromEquipmentId,
                ToEquipmentId = row.ToEquipmentId,
                Direction = p.Direction.ToString(CultureInfo.InvariantCulture),
                DistanceMeter = p.DistanceMeter,
                MinTravelSeconds = min,
                MaxTravelSeconds = Math.Max(min, row.MaxTravelSeconds ?? maxTravelDefault),
                Status = TmsTrafficSegment.StatusEnabled,
                OrderNo = row.OrderNo ?? ++nextOrder,
                Remark = row.Remark,
            };
            var same = row.Code == target.Code && row.Name == target.Name && row.Direction == target.Direction
                       && row.DistanceMeter == target.DistanceMeter && row.MinTravelSeconds == target.MinTravelSeconds
                       && row.MaxTravelSeconds == target.MaxTravelSeconds && row.Status == target.Status
                       && row.OrderNo == target.OrderNo;
            items.Add(new SegmentSyncItem(same ? SyncAction.Unchanged : SyncAction.Update, p, row, target));
        }

        foreach (var row in existing.Where(x => !matched.Contains(x.ID)))
        {
            if (IsLocked(row.Code))
            {
                items.Add(new SegmentSyncItem(SyncAction.Locked, null, row, row));
                continue;
            }
            if (row.Status == TmsTrafficSegment.StatusDisabled) continue;
            var off = Clone(row);
            off.Status = TmsTrafficSegment.StatusDisabled;
            items.Add(new SegmentSyncItem(SyncAction.Disable, null, row, off));
        }

        return items;
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Loại đường đi của một đoạn trên sơ đồ (<see cref="SegmentTopologySegmentOutput.Kind"/>).
    /// Created date: 20/10/2026
    /// </summary>
    public static class PathKind
    {
        /// <summary>Cùng tuyến: <c>Path = [A, B]</c>.</summary>
        public const string Straight = "S";

        /// <summary>Qua MỘT nút giao: <c>Path = [A, (tuyến A, neo A), (tuyến B, neo B), B]</c>.</summary>
        public const string Turn = "T";

        /// <summary>Không dựng được (thiếu camera / thiếu vị trí / không có nút nối): <c>Path</c> rỗng + một dòng cảnh báo.</summary>
        public const string Unresolved = "X";
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Trục vẽ của tuyến (<see cref="SegmentTopologyRouteOutput.Axis"/>).
    /// Created date: 20/10/2026
    /// </summary>
    public static class RouteAxis
    {
        /// <summary>Trục ngang: tuyến a của nút giao, hoặc tuyến không thuộc nút nào.</summary>
        public const string Horizontal = "h";

        /// <summary>Trục dọc: tuyến b của nút giao.</summary>
        public const string Vertical = "v";
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Một mục danh mục mã → tên (<c>SysConfigData</c> loại <c>tms_zone_location</c>: <c>Code</c> → <c>Name</c>).
    /// Created date: 20/10/2026
    /// </summary>
    public sealed record NameRow(string? Code, string? Name);

    /// <summary>
    /// Author: KienLT
    /// Description: Đường đi dựng được cho một đoạn.
    /// Created date: 20/10/2026
    /// </summary>
    /// <param name="Kind">Một giá trị của <see cref="PathKind"/>.</param>
    /// <param name="JunctionCode">Mã nút khi <paramref name="Kind"/> = T.</param>
    /// <param name="Path">Các điểm theo chiều A→B; rỗng khi X.</param>
    /// <param name="Problem">Lý do không dựng được (chỉ khi X), chưa kèm mã đoạn.</param>
    public sealed record PathResult(string Kind, string? JunctionCode, List<SegmentTopologyPointOutput> Path, VdsSegmentTopologyMessageOutput? Problem);

    /// <summary>
    /// Author: KienLT
    /// Description: Dựng đường đi của một đoạn A→B (PLAN-20261020-SO-DO-DUONG-DI-TOC-DO-TB mục 8.3):
    /// <list type="bullet">
    /// <item>Camera đầu/cuối tra theo <paramref name="fromId"/>/<paramref name="toId"/> trong <paramref name="cameras"/>
    /// (danh sách camera của bộ sinh đoạn); không thấy, hoặc thấy mà thiếu km/tuyến ⇒ X.</item>
    /// <item>Cùng tuyến ⇒ S, <c>[A, B]</c>.</item>
    /// <item>Khác tuyến và có ĐÚNG MỘT nút nối hai tuyến ⇒ T, <c>[A, (tuyến A, neo A), (tuyến B, neo B), B]</c>. Không có
    /// nút nào, hoặc nhiều nút cùng nối hai tuyến (không biết đoạn đi qua nút nào) ⇒ X — không vẽ đoán.</item>
    /// </list>
    /// So tuyến và mã nút theo ordinal như <see cref="VdsBaseHelper.SegmentTopologyHp.Plan"/>.
    /// Created date: 20/10/2026
    /// </summary>
    /// <param name="cameras">Camera theo ID (nên dùng so khớp không phân biệt hoa thường như worker).</param>
    public static PathResult BuildPath(string? fromId, string? toId,
        IReadOnlyDictionary<string, VdsBaseHelper.SegmentTopologyHp.TopoCamera> cameras,
        IEnumerable<VdsBaseHelper.SegmentTopologyHp.TopoJunction> junctions)
    {
        static PathResult Fail(string key, params object[] arguments)
            => new(PathKind.Unresolved, null, new List<SegmentTopologyPointOutput>(), new(key, arguments));
        VdsBaseHelper.SegmentTopologyHp.TopoCamera? Find(string? id)
            => !string.IsNullOrWhiteSpace(id) && cameras.TryGetValue(id.Trim(), out var c) ? c : null;
        static string? RouteOf(VdsBaseHelper.SegmentTopologyHp.TopoCamera c)
            => string.IsNullOrWhiteSpace(c.Route) ? null : c.Route.Trim();

        var a = Find(fromId);
        var b = Find(toId);
        if (a is null || b is null)
        {
            object CameraId(string? id) => string.IsNullOrWhiteSpace(id)
                ? new VdsSegmentTopologyMessageOutput(BaseMsg.Vds.Message.TopologyEmptyId) : id.Trim();
            if (a is null && b is null)
                return Fail(BaseMsg.Vds.Message.TopologyBothCamerasMissing, CameraId(fromId), CameraId(toId));
            return a is null
                ? Fail(BaseMsg.Vds.Message.TopologyFromCameraMissing, CameraId(fromId))
                : Fail(BaseMsg.Vds.Message.TopologyToCameraMissing, CameraId(toId));
        }

        var routeA = RouteOf(a);
        var routeB = RouteOf(b);
        var unplaced = new[] { (Camera: a, Route: routeA), (Camera: b, Route: routeB) }
            .Where(x => x.Camera.Position is null || x.Route is null).Select(x => x.Camera.Code).Distinct().ToList();
        if (unplaced.Count > 0) return Fail(BaseMsg.Vds.Message.TopologyCameraUnplaced, string.Join(", ", unplaced));

        SegmentTopologyPointOutput Point(string route, int pos) => new() { Route = route, Pos = pos };
        var start = Point(routeA!, a.Position!.Value);
        var end = Point(routeB!, b.Position!.Value);
        if (routeA == routeB) return new PathResult(PathKind.Straight, null, new List<SegmentTopologyPointOutput> { start, end }, null);

        var links = junctions.Where(j => j.Connects(routeA!, routeB!)).ToList();
        if (links.Count == 0) return Fail(BaseMsg.Vds.Message.TopologyNoJunction, routeA!, routeB!);
        if (links.Count > 1)
            return Fail(BaseMsg.Vds.Message.TopologyAmbiguousJunction, links.Count, routeA!, routeB!, string.Join(", ", links.Select(j => j.Code)));

        var j = links[0];
        return new PathResult(PathKind.Turn, j.Code, new List<SegmentTopologyPointOutput>
        {
            start, Point(routeA!, j.AnchorOf(routeA!)!.Value), Point(routeB!, j.AnchorOf(routeB!)!.Value), end,
        }, null);
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Dựng sơ đồ tuyến cho API <c>Topology</c> (mục 8.2/8.3 của PLAN-20261020-SO-DO-DUONG-DI-TOC-DO-TB).
    /// <list type="bullet">
    /// <item><b>Nút giao</b>: <see cref="VdsBaseHelper.SegmentTopologyHp.ReadJunctions"/> — cùng hàm của bộ sinh đoạn; nút
    /// hỏng chỉ thành cảnh báo.</item>
    /// <item><b>Camera</b>: đúng danh sách bộ sinh đoạn đã chọn (<see cref="SelectCameras"/>); camera thiếu km/tuyến không vẽ
    /// được ⇒ cảnh báo, nhưng vẫn được nhận ra khi dựng đoạn (để báo "thiếu km/tuyến" thay vì "thiếu camera").</item>
    /// <item><b>Tuyến</b>: mọi tuyến có camera vẽ được hoặc là tuyến của nút. Trục: tuyến a của nút ⇒ <c>h</c>, tuyến b ⇒
    /// <c>v</c>; không thuộc nút nào ⇒ <c>h</c> + cảnh báo; vừa a vừa b (hai nút khác nhau) ⇒ <c>h</c> + cảnh báo.
    /// Min/Max = vị trí nhỏ/lớn nhất trong camera + điểm neo nút của tuyến. Tên tra danh mục (bỏ khoảng trắng, không phân
    /// biệt hoa thường, mục đầu có tên); thiếu ⇒ bằng mã.</item>
    /// <item><b>Đoạn</b>: mọi dòng của <paramref name="segments"/>, giữ thứ tự đầu vào; đường đi theo <see cref="BuildPath"/>,
    /// đoạn X thêm một dòng cảnh báo nêu mã đoạn (mã rỗng ⇒ ID).</item>
    /// <item><b>Đoạn worker tự sinh</b>: hành trình của đoạn tự sinh chưa Sync mang
    /// <c>SegmentId</c> = mã tất định (<c>S:VDS-01&gt;VDS-03</c>, <c>T:1_2:VDS-01&gt;VDS-08</c>) không có trong DB. Khi có
    /// <paramref name="generated"/>, mọi đoạn của nó có <c>Code</c> CHƯA trùng <c>ID</c> một đoạn DB (so ordinal — so theo
    /// mã, không theo cặp camera, vì hành trình cũ vẫn trỏ mã tự sinh dù sau đó đã khai tay cùng cặp) được nối sau đoạn
    /// DB, dựng giống hệt worker (<c>AutoSegmentManager</c>): <c>Id = Code = mã</c>, tên <c>"{A} → {B}"</c> /
    /// <c>"{A} → {B} ({lượt rẽ})"</c>, <c>Status = null</c>; đường đi cũng qua <see cref="BuildPath"/>. Kết quả sinh có lỗi
    /// cấu hình ⇒ không bổ sung đoạn nào (có thể thiếu đoạn, worker cũng không áp), thêm một cảnh báo kèm lỗi đầu tiên.</item>
    /// </list>
    /// Created date: 20/10/2026
    /// </summary>
    /// <param name="cameras">Camera đã qua <see cref="SelectCameras"/>.</param>
    /// <param name="cameraNames">ID camera → <c>TmsEquipment.Name</c>; thiếu/rỗng ⇒ tên = mã.</param>
    /// <param name="config">Dòng VdsConfig nhóm hình học tuyến (cùng truy vấn của bộ sinh đoạn).</param>
    /// <param name="segments"><c>TmsTrafficSegment</c> chưa xoá (kể cả đang tắt), đã sắp theo thứ tự hiển thị.</param>
    /// <param name="routeCatalog">Danh mục tuyến <c>tms_zone_location</c>, đã sắp theo <c>OrderNo</c>.</param>
    /// <param name="warnings">Cảnh báo có sẵn (ví dụ của <see cref="SelectCameras"/>), đứng đầu danh sách kết quả.</param>
    /// <param name="generated">Kết quả <see cref="Plan"/> trên CÙNG <paramref name="cameras"/>/<paramref name="config"/>;
    /// null ⇒ chỉ có đoạn DB.</param>
    /// <param name="formatMessage">Dịch mã cảnh báo (<see cref="VdsSegmentTopologyMessageOutput"/>) theo ngôn ngữ request; null ⇒ giữ nguyên mã.</param>
    public static SegmentTopologyOutput BuildTopology(
        IReadOnlyList<VdsBaseHelper.SegmentTopologyHp.TopoCamera> cameras,
        IReadOnlyDictionary<string, string?> cameraNames,
        IEnumerable<VdsBaseHelper.SegmentTopologyHp.TopoConfigRow> config,
        IEnumerable<TmsTrafficSegment> segments,
        IEnumerable<NameRow> routeCatalog,
        IEnumerable<string>? warnings = null,
        VdsBaseHelper.SegmentTopologyHp.TopoPlan? generated = null,
        Func<VdsSegmentTopologyMessageOutput, string>? formatMessage = null)
    {
        string Text(VdsSegmentTopologyMessageOutput message) => formatMessage?.Invoke(message) ?? message.Key;
        string Format(string key, params object[] arguments) => Text(new(key, arguments));
        var output = new SegmentTopologyOutput();
        if (warnings is not null) output.Warnings.AddRange(warnings);

        // ===== Nút giao =====
        var junctionErrors = new List<VdsSegmentTopologyMessageOutput>();
        var junctions = VdsBaseHelper.SegmentTopologyHp.ReadJunctions(config, junctionErrors).Values
            .OrderBy(j => j.Code, StringComparer.Ordinal).ToList();
        output.Warnings.AddRange(junctionErrors.Select(Text));
        output.Junctions = junctions.Select(j => new SegmentTopologyJunctionOutput
        {
            Code = j.Code, RouteA = j.A, PosA = j.APos, RouteB = j.B, PosB = j.BPos,
        }).ToList();

        // ===== Camera =====
        var byId = new Dictionary<string, VdsBaseHelper.SegmentTopologyHp.TopoCamera>(StringComparer.OrdinalIgnoreCase);
        var placed = new List<SegmentTopologyCameraOutput>();
        foreach (var c in cameras)
        {
            var camera = c with { Route = string.IsNullOrWhiteSpace(c.Route) ? null : c.Route.Trim() };
            if (string.IsNullOrWhiteSpace(camera.Id) || !byId.TryAdd(camera.Id.Trim(), camera)) continue;
            if (camera.Position is null || camera.Route is null)
            {
                output.Warnings.Add(Format(BaseMsg.Vds.Message.TopologyCameraNotDrawable, camera.Code));
                continue;
            }
            placed.Add(new SegmentTopologyCameraOutput
            {
                Id = camera.Id,
                Code = camera.Code,
                Name = cameraNames.TryGetValue(camera.Id.Trim(), out var name) && !string.IsNullOrWhiteSpace(name) ? name.Trim() : camera.Code,
                Route = camera.Route,
                Pos = camera.Position.Value,
                Direction = camera.Direction,
            });
        }
        output.Cameras = placed.OrderBy(x => x.Route, StringComparer.Ordinal).ThenBy(x => x.Pos)
            .ThenBy(x => x.Code, StringComparer.Ordinal).ToList();

        // ===== Tuyến =====
        var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in routeCatalog)
            if (!string.IsNullOrWhiteSpace(row.Code) && !string.IsNullOrWhiteSpace(row.Name))
                names.TryAdd(row.Code.Trim(), row.Name.Trim());

        var routes = new List<SegmentTopologyRouteOutput>();
        foreach (var code in output.Cameras.Select(x => x.Route).Concat(junctions.SelectMany(j => new[] { j.A, j.B }))
                     .Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal))
        {
            var isA = junctions.Any(j => j.A == code);
            var isB = junctions.Any(j => j.B == code);
            if (!isA && !isB) output.Warnings.Add(Format(BaseMsg.Vds.Message.TopologyRouteWithoutJunction, code));
            else if (isA && isB) output.Warnings.Add(Format(BaseMsg.Vds.Message.TopologyRouteAxisConflict, code));
            var positions = output.Cameras.Where(x => x.Route == code).Select(x => x.Pos)
                .Concat(junctions.Where(j => j.A == code).Select(j => j.APos))
                .Concat(junctions.Where(j => j.B == code).Select(j => j.BPos)).ToList();
            routes.Add(new SegmentTopologyRouteOutput
            {
                Code = code,
                Name = names.TryGetValue(code, out var routeName) ? routeName : code,
                Axis = isB && !isA ? RouteAxis.Vertical : RouteAxis.Horizontal,
                MinPos = positions.Min(),
                MaxPos = positions.Max(),
            });
        }
        output.Routes = routes.OrderBy(x => x.Axis == RouteAxis.Horizontal ? 0 : 1)
            .ThenBy(x => x.Code, StringComparer.Ordinal).ToList();

        // ===== Đoạn =====
        void AddSegment(string id, string? code, string? name, string? fromId, string? toId, string? direction,
            int? distanceMeter, string? status)
        {
            var path = BuildPath(fromId, toId, byId, junctions);
            if (path.Problem is not null)
                output.Warnings.Add(Format(BaseMsg.Vds.Message.TopologySegmentUnresolved, string.IsNullOrWhiteSpace(code) ? id : code, path.Problem));
            output.Segments.Add(new SegmentTopologySegmentOutput
            {
                Id = id,
                Code = code,
                Name = name,
                FromEquipmentId = fromId,
                ToEquipmentId = toId,
                Direction = direction,
                DistanceMeter = distanceMeter,
                Status = status,
                Kind = path.Kind,
                JunctionCode = path.JunctionCode,
                Path = path.Path,
            });
        }

        foreach (var s in segments)
            AddSegment(s.ID ?? "", s.Code, s.Name, s.FromEquipmentId, s.ToEquipmentId, s.Direction, s.DistanceMeter, s.Status);

        // Bổ sung đoạn worker tự sinh chưa có trong DB.
        if (generated is { HasErrors: true })
            output.Warnings.Add(Format(BaseMsg.Vds.Message.TopologyGeneratedSegmentsSkipped, generated.Errors.Count, generated.Errors[0]));
        else if (generated is not null)
        {
            var known = output.Segments.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
            foreach (var p in generated.Segments)
            {
                if (!known.Add(p.Code)) continue; // đã có đoạn DB cùng ID ⇒ bản DB thắng, không nhân đôi
                AddSegment(p.Code, p.Code, DefaultName(p), p.FromId, p.ToId,
                    p.Direction.ToString(CultureInfo.InvariantCulture), p.DistanceMeter, status: null);
            }
        }

        return output;
    }

    /// <summary>Tên mặc định của đoạn tự sinh — CÙNG mẫu với worker (<c>AutoSegmentManager</c>); dùng cho Sync và sơ đồ tuyến.</summary>
    private static string DefaultName(VdsBaseHelper.SegmentTopologyHp.PlannedSegment p)
        => p.Turn is null ? $"{p.FromCode} → {p.ToCode}" : $"{p.FromCode} → {p.ToCode} ({p.Turn})";

    private static TmsTrafficSegment Clone(TmsTrafficSegment r) => new()
    {
        ID = r.ID, Code = r.Code, Name = r.Name, FromEquipmentId = r.FromEquipmentId, ToEquipmentId = r.ToEquipmentId,
        Direction = r.Direction, DistanceMeter = r.DistanceMeter, MinTravelSeconds = r.MinTravelSeconds,
        MaxTravelSeconds = r.MaxTravelSeconds, Status = r.Status, OrderNo = r.OrderNo, Remark = r.Remark,
    };
}
