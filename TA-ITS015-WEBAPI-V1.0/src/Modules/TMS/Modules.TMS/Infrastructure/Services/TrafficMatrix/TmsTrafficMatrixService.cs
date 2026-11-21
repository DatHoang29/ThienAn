using Modules.CfgSystem.Core.Entities;
using Modules.TMS.Controllers.TrafficMatrix.Dto;
using Modules.TMS.Controllers.Zone.Dto;
using Modules.TMS.Core.Constants;
using Shared.Utility.Counting;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Extensions;
using Shared.DTO.Dtos.Vds;
using Shared.Utility.Utilities;
using static Shared.DTO.Enums.BaseEnums;
using Modules.TMS.Core.Dto.TrafficMatrix;

namespace Modules.TMS.Infrastructure.Services.TrafficMatrix
{
    /// <summary>
    /// Author: KienLT
    /// Description: Dựng bảng ma trận vận tốc / lưu lượng theo giờ từ bảng raw <c>TmsTrafficData</c>.
    ///
    /// <para><b>Vì sao gộp ở SQL.</b> Một ngày dữ liệu VDS là hàng trăm nghìn dòng; các chỗ tổng hợp theo
    /// giờ hiện có trong hệ (<c>TrafficDataDayService</c>, <c>TrafficStatisticSectionReportService</c>)
    /// đều <c>ToListAsync()</c> rồi LINQ trong RAM. Ở đây gộp bằng <c>GROUP BY</c> nên chỉ mang về tối đa
    /// <c>số_zone × số_hướng × 24</c> dòng.</para>
    ///
    /// <para><b>Vì sao lọc EventType.</b> Bản tin sự kiện (Speeding / Congestion / ...) sinh THÊM trên
    /// cùng một lượt xe. Không lọc thì lưu lượng bị đếm trùng và vận tốc bị kéo lệch về phía xe vi phạm.
    /// Tiêu chí lọc lấy từ <see cref="TrafficCountFilter"/> — dùng chung với bộ đếm của VDS để hai bên
    /// không thể lệch số.</para>
    ///
    /// <para><b>Vì sao trung bình có trọng số.</b> Vận tốc của đoạn/của ngày là trung bình của TỪNG LƯỢT
    /// XE, không phải trung bình của các trung bình — một giờ có 5 xe không được nặng bằng giờ có 500 xe.</para>
    ///
    /// Created date: 11/08/2026
    /// </summary>
    public class TmsTrafficMatrixService : IScoped
    {
        /// <summary>Giá trị <c>Pid</c> của zone gốc.</summary>
        private const string RootPid = "0";

        /// <summary>
        /// Chặn số bậc khi leo cây zone: dữ liệu lỗi (<c>Pid</c> trỏ vòng) không được làm treo request.
        /// </summary>
        private const int MaxZoneDepth = 32;

        private readonly BaseRepository<TmsTrafficData> _trafficDataRepo;
        private readonly BaseRepository<TmsZone> _zoneRepo;
        private readonly BaseRepository<SysConfigData> _configDataRepo;
        private readonly BaseRepository<SysOpConfig> _opConfigRepo;
        private readonly IMessageBus _messageBus;

        public TmsTrafficMatrixService(
            BaseRepository<TmsTrafficData> trafficDataRepo,
            BaseRepository<TmsZone> zoneRepo,
            BaseRepository<SysConfigData> configDataRepo,
            BaseRepository<SysOpConfig> opConfigRepo,
            IMessageBus messageBus)
        {
            _trafficDataRepo = trafficDataRepo;
            _zoneRepo = zoneRepo;
            _configDataRepo = configDataRepo;
            _opConfigRepo = opConfigRepo;
            _messageBus = messageBus;
        }

        /// <summary>
        /// Dựng ma trận cho một ngày. Luôn trả về đủ hàng (mọi đoạn tuyến) × đủ 24 cột, kể cả khi không
        /// có xe — bảng thiếu ô làm người xem tưởng mất dữ liệu.
        /// </summary>
        public async Task<TrafficMatrixOutput> BuildAsync(TrafficMatrixInput input)
        {
            // FE gửi UTC+0 ⇒ quy về giờ server trước khi cắt mốc ngày, kẻo lệch một ngày.
            var valueDate = (BaseUtil.TimeServer.GetTimeServer(input.ValueDate)
                             ?? BaseUtil.TimeServer.GetTimeServer()).Date;
            var dayEnd = valueDate.AddDays(1);

            var filter = new TrafficCountFilter { CountAllEventTypes = input.CountAllEventTypes };
            var eventTypes = filter.EffectiveEventTypes;

            var zoneList = await LoadZoneListAsync();
            var parentMap = await LoadZoneParentMapAsync();
            var configDataList = await LoadConfigDataListAsync();
            var opConfig = await LoadOpConfigMapAsync();

            // Thứ tự ưu tiên: FE chỉ định > cấu hình hoạt động > không gom (zone có dữ liệu làm hàng).
            var zoneType = string.IsNullOrWhiteSpace(input.ZoneType)
                ? ResolveOpConfig(opConfig, TrafficMatrixConst.OpConfig.RowZoneType)
                : input.ZoneType!.Trim();

            // Phạm vi nhánh zone được xem (thường là một tuyến). FE chỉ định thì ghi đè cấu hình.
            var rootZoneId = string.IsNullOrWhiteSpace(input.ZoneId)
                ? ResolveOpConfig(opConfig, TrafficMatrixConst.OpConfig.RootZoneId)
                : input.ZoneId!.Trim();

            var zoneById = BuildIndex(zoneList);
            var rowZones = ResolveRowZones(zoneList, parentMap, zoneType, rootZoneId);
            var leafToRow = BuildLeafToRowMap(parentMap, rowZones);

            var rawRows = await QueryHourlyAsync(valueDate, dayEnd, eventTypes, input);
            var buckets = Rollup(rawRows, leafToRow);

            // Hàng chỉ có ở dữ liệu (zone chưa khai đúng loại) vẫn phải hiện, kẻo mất xe khỏi báo cáo.
            AppendZonesPresentInData(rowZones, buckets, zoneById, parentMap, rootZoneId);

            var directions = ResolveDirections(configDataList, buckets, input.Direction);
            var bands = new MatrixBands(
                ResolveBands(configDataList, TrafficMatrixConst.ConfigType.SpeedBand),
                ResolveBands(configDataList, TrafficMatrixConst.ConfigType.VolumeBand));

            return new TrafficMatrixOutput
            {
                ValueDate = valueDate,
                Hours = Enumerable.Range(0, TrafficMatrixConst.HoursPerDay).ToList(),
                ZoneType = zoneType,
                RootZoneId = rootZoneId,
                CountedEventTypes = eventTypes,
                SpeedBands = bands.Speed,
                VolumeBands = bands.Volume,
                Groups = directions
                    .Select(direction => BuildGroup(direction, rowZones, buckets, bands))
                    .ToList()
            };
        }

        #region Truy vấn

        /// <summary>
        /// Gộp theo (ZoneId, Direction, giờ) — NGUỒN SỐ nay là module VDS (QuestDB), gọi IN-PROCESS qua
        /// <c>IMessageBus.InvokeAsync</c> với <see cref="VdsTrafficMatrixRequest"/> (handler
        /// <c>Modules.VDS.Controllers.TrafficMatrix.Queries.TrafficMatrixQueryHandler</c>), KHÔNG còn đọc thẳng
        /// <c>TmsTrafficData</c> trên SQL Server.
        ///
        /// <para><b>13/10/2026:</b> bỏ Refit <c>IVdsTrafficMatrixApi</c> (HTTP nội bộ cùng process). Handler VDS tự
        /// chạy validator + quy UTC như endpoint HTTP cũ. Gọi qua <c>InvokeVdsAsync</c>: giữ giới hạn 10s của Refit,
        /// lỗi nội bộ phía VDS ghi log rồi trả thông báo chung <c>BaseMsg.VdsQuery.Exception.MatrixUnavailable</c>.</para>
        ///
        /// <para><b>Cutover 03/09/2026 (Phase 4→5).</b> Dữ liệu thô VDS đã chuyển sang QuestDB; đọc thẳng
        /// SQL Server là đọc bảng không còn được ghi. VDS trả ma trận THÔ theo <c>zone_id</c>; phần gom cây
        /// zone + thang màu vẫn ở service này (quyết định 3b). Giữ nguyên chữ ký + kiểu trả để
        /// <c>Rollup</c> và phần còn lại không đổi — rollback chỉ là revert đúng method này.</para>
        ///
        /// <para><b>vehicle_size, KHÔNG phải vehicle_type.</b> <c>input.VehicleType</c> của FE mang nghĩa
        /// CỠ xe (short/medium/long) nên map sang <c>VehicleSize</c> của request VDS.</para>
        ///
        /// <para>⚠️ CHƯA đối chiếu số trên dữ liệu QuestDB thật (chưa có dữ liệu tại thời điểm cutover).
        /// Khi có dữ liệu: so từng ô (zone×hour×direction) với bản đọc SQL cũ trước khi tin số.</para>
        /// </summary>
        private async Task<List<TrafficMatrixRawRow>> QueryHourlyAsync(
            DateTime dayStart, DateTime dayEnd, List<string> eventTypes, TrafficMatrixInput input)
        {
            var request = new VdsTrafficMatrixRequest
            {
                FromDate = DateTime.SpecifyKind(dayStart, DateTimeKind.Local).ToUniversalTime(),
                ToDate = DateTime.SpecifyKind(dayEnd, DateTimeKind.Local).ToUniversalTime(),
                VehicleSize = string.IsNullOrWhiteSpace(input.VehicleType) ? null : input.VehicleType!.Trim(),
                Direction = NormalizeDirection(input.Direction),
                CountAllEventTypes = input.CountAllEventTypes,
            };

            var rows = await _messageBus.InvokeVdsAsync<List<VdsTrafficMatrixRow>>(
                request, TimeSpan.FromSeconds(10), BaseMsg.VdsQuery.Exception.MatrixUnavailable);
            if (rows.Count == 0) return new List<TrafficMatrixRawRow>();

            return rows.Select(r => new TrafficMatrixRawRow
            {
                ZoneId = r.ZoneId,
                Direction = r.Direction,
                Hour = r.Hour,
                VehicleCount = r.VehicleCount,
                SpeedSum = r.SpeedSum is double s ? (float)s : (float?)null,
                SpeedSampleCount = r.SpeedSampleCount,
            }).ToList();
        }

        private async Task<List<ZoneOutput>> LoadZoneListAsync()
        {
            // ZoneInput đi qua cache của ZoneQueryHandler nên không thêm truy vấn DB.
            var list = await _messageBus.InvokeAsync<List<ZoneOutput>>(new ZoneInput());
            return list ?? new List<ZoneOutput>();
        }

        /// <summary>
        /// Bản đồ <c>ID → Pid</c> của MỌI zone, KỂ CẢ zone đã xoá mềm.
        /// <para>
        /// Vì sao cần: <c>TmsTrafficData.ZoneId</c> vẫn trỏ tới zone đã bị xoá (dữ liệu cũ không được
        /// dọn theo). Nếu chỉ leo cây trên danh sách zone đang hoạt động thì những zone đó không gom
        /// lên được đoạn tuyến, và lượt xe của chúng biến thành một hàng lạ mang tên là GUID.
        /// Ở đây chỉ lấy hai cột nên truy vấn rất nhẹ.
        /// </para>
        /// </summary>
        private async Task<Dictionary<string, string?>> LoadZoneParentMapAsync()
        {
            var rows = await _zoneRepo.AsQueryable()
                .ClearFilter()
                .Select(z => new { z.ID, z.Pid })
                .ToListAsync();

            return rows
                .Where(z => !string.IsNullOrWhiteSpace(z.ID))
                .GroupBy(z => z.ID!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Pid, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Danh mục cấu hình tham số cần cho ma trận, ĐỌC THẲNG TỪ DB.
        /// <para>
        /// Vì sao không đi qua <c>SysConfigDataQueryHandler</c>: handler đó cache toàn bộ danh mục 30
        /// phút dưới một khoá duy nhất và KHÔNG bị xoá khi cấu hình thay đổi, nên sửa thang màu hay
        /// tuyến xong phải restart API mới thấy — đã mất công truy vết hai lần vì chuyện này. Truy vấn
        /// ở đây chỉ lấy vài chục dòng của đúng mấy loại danh mục cần dùng nên rẻ hơn nhiều so với
        /// việc gộp dữ liệu giao thông ngay bên dưới.
        /// </para>
        /// <para>
        /// Thêm một lợi ích: <c>Status</c> ở đây là giá trị thật, nên tắt một dải màu trong danh mục là
        /// nó rơi ra khỏi thang ngay — bản chiếu của handler cũ bỏ qua cột này.
        /// </para>
        /// </summary>
        private async Task<List<ConfigRow>> LoadConfigDataListAsync()
        {
            var typeCodes = new List<string>
            {
                TrafficMatrixConst.ConfigType.SpeedBand,
                TrafficMatrixConst.ConfigType.VolumeBand,
                SysConfigTypeCodeEnum.TmsDirectionConfigCode,
                SysConfigTypeCodeEnum.TmsDirectionAllCode
            };

            // ClearFilter + điều kiện IsDelete tự khai: theo đúng cách AllConfigListInput đang làm,
            // vì filter toàn cục không áp chắc chắn lên bảng được join.
            return await _configDataRepo.AsQueryable()
                .ClearFilter()
                .InnerJoin<SysConfigType>((d, t) => d.ConfigTypeId == t.ID)
                .Where((d, t) => d.IsDelete == null && t.IsDelete == null)
                .Where((d, t) => typeCodes.Contains(t.Code))
                .Select((d, t) => new ConfigRow
                {
                    TypeCode = t.Code,
                    Code = d.Code,
                    Name = d.Name,
                    Value = d.Value,
                    OrderNo = d.OrderNo,
                    Status = d.Status
                })
                .ToListAsync();
        }

        /// <summary>
        /// Cấu hình hoạt động của nhóm ma trận, cũng đọc thẳng từ DB để đổi là ăn ngay.
        /// Trả về map <c>Code → Value</c>.
        /// </summary>
        private async Task<Dictionary<string, string?>> LoadOpConfigMapAsync()
        {
            var rows = await _opConfigRepo.AsQueryable()
                .ClearFilter()
                .Where(o => o.IsDelete == null
                            && o.Status == StatusEnum.Enable
                            && o.GroupCode == TrafficMatrixConst.OpConfig.GroupCode)
                .Select(o => new { o.Code, o.Value })
                .ToListAsync();

            return rows
                .Where(o => !string.IsNullOrWhiteSpace(o.Code))
                .GroupBy(o => o.Code!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Value?.Trim(), StringComparer.OrdinalIgnoreCase);
        }

        #endregion

        #region Cấu hình

        /// <summary>
        /// Đọc một giá trị trong nhóm cấu hình hoạt động của bảng ma trận theo <c>Code</c>.
        /// Chưa cấu hình hoặc để rỗng ⇒ null; bên gọi tự quyết định cách xử lý khi thiếu.
        /// </summary>
        private static string? ResolveOpConfig(Dictionary<string, string?> opConfig, string code)
            => opConfig.TryGetValue(code, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

        /// <summary>
        /// Thang màu đọc từ cấu hình tham số: <c>Code</c> = khoảng giá trị, <c>Name</c> = nhãn,
        /// <c>Value</c> = mã màu, sắp theo <c>OrderNo</c>. Dải bị tắt (<c>Status = Disable</c>) bị loại.
        /// <para>
        /// Bản ghi sai định dạng khoảng bị bỏ qua thay vì làm hỏng cả thang. Chưa cấu hình ⇒ danh sách
        /// rỗng, FE hiển thị số không tô màu — vẫn đọc được, chỉ mất phần trực quan.
        /// </para>
        /// </summary>
        private static List<TrafficMatrixBand> ResolveBands(List<ConfigRow> configDataList, string configTypeCode)
            => Config(configDataList, configTypeCode)
                .Where(item => item.Status != StatusEnum.Disable)
                .OrderBy(item => item.OrderNo ?? int.MaxValue)
                .Select(item => TrafficMatrixConst.ParseBand(item.Code, item.Name, item.Value))
                .Where(band => band is not null)
                .Select(band => band!)
                .ToList();

        #endregion

        #region Zone

        /// <summary>
        /// Các zone làm HÀNG: đúng <paramref name="zoneType"/> và nằm trong nhánh <paramref name="rootZoneId"/>
        /// (nếu có), sắp theo lý trình tăng dần.
        /// <para>
        /// <paramref name="zoneType"/> null (chưa cấu hình <c>row_zone_type</c> và FE cũng không chỉ định)
        /// ⇒ trả danh sách rỗng, hàng sẽ được lấy từ chính zone có dữ liệu
        /// (xem <see cref="AppendZonesPresentInData"/>).
        /// </para>
        /// </summary>
        private static List<ZoneOutput> ResolveRowZones(
            List<ZoneOutput> zoneList, Dictionary<string, string?> parentMap,
            string? zoneType, string? rootZoneId)
        {
            if (string.IsNullOrWhiteSpace(zoneType)) return new List<ZoneOutput>();

            var rows = zoneList
                .Where(z => !string.IsNullOrWhiteSpace(z.ID))
                .Where(z => !string.IsNullOrWhiteSpace(z.Type)
                            && z.Type!.Equals(zoneType, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!string.IsNullOrWhiteSpace(rootZoneId))
                rows = rows.Where(z => IsSelfOrDescendant(parentMap, z.ID, rootZoneId!)).ToList();

            return Sort(rows);
        }

        /// <summary>
        /// Ánh xạ zone LÁ (giá trị <c>TmsTrafficData.ZoneId</c>) → zone HÀNG: leo cây theo <c>Pid</c> tới
        /// tổ tiên đầu tiên nằm trong tập hàng. Dùng bản đồ cha-con đầy đủ nên zone đã xoá mềm vẫn gom
        /// được về đúng đoạn tuyến của nó.
        /// </summary>
        private static Dictionary<string, string> BuildLeafToRowMap(
            Dictionary<string, string?> parentMap, List<ZoneOutput> rowZones)
        {
            var rowIds = new HashSet<string>(
                rowZones.Select(z => z.ID!), StringComparer.OrdinalIgnoreCase);
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var zoneId in parentMap.Keys)
            {
                var rowId = FindRowAncestor(parentMap, rowIds, zoneId);
                if (rowId != null) map[zoneId] = rowId;
            }

            return map;
        }

        private static string? FindRowAncestor(
            Dictionary<string, string?> parentMap, HashSet<string> rowIds, string zoneId)
        {
            var current = zoneId;

            // Chặn số vòng lặp: dữ liệu zone lỗi (Pid trỏ vòng) không được làm treo request.
            for (var depth = 0; depth < MaxZoneDepth; depth++)
            {
                if (rowIds.Contains(current)) return current;
                if (!parentMap.TryGetValue(current, out var pid)) return null;
                if (string.IsNullOrWhiteSpace(pid) || pid == RootPid) return null;

                current = pid!;
            }

            return null;
        }

        private static bool IsSelfOrDescendant(
            Dictionary<string, string?> parentMap, string? zoneId, string rootId)
        {
            if (string.IsNullOrWhiteSpace(zoneId)) return false;

            var current = zoneId!;

            for (var depth = 0; depth < MaxZoneDepth; depth++)
            {
                if (current.Equals(rootId, StringComparison.OrdinalIgnoreCase)) return true;
                if (!parentMap.TryGetValue(current, out var pid)) return false;
                if (string.IsNullOrWhiteSpace(pid) || pid == RootPid) return false;

                current = pid!;
            }

            return false;
        }

        /// <summary>
        /// Bổ sung hàng cho zone CÓ DỮ LIỆU nhưng không gom được về hàng nào — tức không có tổ tiên nào
        /// thuộc loại đang dùng làm hàng.
        /// <para>
        /// Khi ĐANG GIỚI HẠN phạm vi (<paramref name="rootZoneId"/> có giá trị) thì chỉ bổ sung zone
        /// chứng minh được là thuộc nhánh đó — người dùng đã yêu cầu xem một tuyến thì không được chèn
        /// hàng của tuyến khác vào.
        /// </para>
        /// </summary>
        private static void AppendZonesPresentInData(
            List<ZoneOutput> rowZones, Dictionary<MatrixKey, MatrixAccumulator> buckets,
            Dictionary<string, ZoneOutput> zoneById, Dictionary<string, string?> parentMap,
            string? rootZoneId)
        {
            var known = new HashSet<string>(
                rowZones.Select(z => z.ID!), StringComparer.OrdinalIgnoreCase);
            var scoped = !string.IsNullOrWhiteSpace(rootZoneId);

            var missing = buckets.Keys
                .Select(k => k.ZoneId)
                .Where(id => !known.Contains(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(id => !scoped || IsSelfOrDescendant(parentMap, id, rootZoneId!))
                .ToList();

            if (missing.Count == 0) return;

            foreach (var zoneId in missing)
                rowZones.Add(zoneById.TryGetValue(zoneId, out var zone)
                    ? zone
                    : new ZoneOutput { ID = zoneId, Name = zoneId });

            var sorted = Sort(rowZones);
            rowZones.Clear();
            rowZones.AddRange(sorted);
        }

        private static Dictionary<string, ZoneOutput> BuildIndex(List<ZoneOutput> zoneList)
            => zoneList
                .Where(z => !string.IsNullOrWhiteSpace(z.ID))
                .GroupBy(z => z.ID!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        /// <summary>Sắp hàng theo lý trình; zone chưa khai lý trình xuống cuối, sắp theo Code.</summary>
        private static List<ZoneOutput> Sort(List<ZoneOutput> zones)
            => zones
                .OrderBy(z => z.FromKmNumber ?? int.MaxValue)
                .ThenBy(z => z.FromMetNumber ?? int.MaxValue)
                .ThenBy(z => z.Code ?? z.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList();

        #endregion

        #region Hướng tuyến

        /// <summary>
        /// Hướng cần hiển thị. Lấy danh mục <c>direction_config_type</c>, bỏ các mã mang nghĩa "tất cả"
        /// (<c>direction_all_value</c> + <c>SpecialConfigEnum.AllDirection</c>) vì đó là sentinel của bộ
        /// lọc, không phải một hướng thật.
        /// </summary>
        private static List<TrafficMatrixDirection> ResolveDirections(
            List<ConfigRow> configDataList,
            Dictionary<MatrixKey, MatrixAccumulator> buckets,
            string? requested)
        {
            var allCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                SpecialConfigEnum.AllDirection, "all"
            };
            foreach (var item in Config(configDataList, SysConfigTypeCodeEnum.TmsDirectionAllCode))
            {
                if (!string.IsNullOrWhiteSpace(item.Code)) allCodes.Add(item.Code!.Trim());
                if (!string.IsNullOrWhiteSpace(item.Value)) allCodes.Add(item.Value!.Trim());
            }

            var directions = Config(configDataList, SysConfigTypeCodeEnum.TmsDirectionConfigCode)
                .Where(item => !string.IsNullOrWhiteSpace(item.Code))
                .Where(item => !allCodes.Contains(item.Code!.Trim()))
                .OrderBy(item => item.OrderNo ?? int.MaxValue)
                .Select(item => new TrafficMatrixDirection(item.Code!.Trim(), item.Name ?? item.Code!.Trim()))
                .ToList();

            // Danh mục chưa khai ⇒ suy hướng từ chính dữ liệu, còn hơn trả bảng rỗng.
            if (directions.Count == 0)
                directions = buckets.Keys
                    .Select(k => k.Direction)
                    .Where(code => !string.IsNullOrWhiteSpace(code))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
                    .Select(code => new TrafficMatrixDirection(code, code))
                    .ToList();

            var normalized = NormalizeDirection(requested);
            if (!string.IsNullOrWhiteSpace(normalized))
                directions = directions
                    .Where(d => d.Code.Equals(normalized, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            // Camera chưa khai DirectionId vẫn sinh dữ liệu; gom vào một nhóm riêng thay vì bỏ đi.
            if (string.IsNullOrWhiteSpace(normalized)
                && buckets.Keys.Any(k => string.IsNullOrWhiteSpace(k.Direction)))
                directions.Add(new TrafficMatrixDirection(string.Empty, null));

            return directions;
        }

        /// <summary>Mã hướng mang nghĩa "tất cả" được coi như không lọc.</summary>
        private static string? NormalizeDirection(string? direction)
        {
            if (string.IsNullOrWhiteSpace(direction)) return null;

            var code = direction.Trim();
            return code.Equals(SpecialConfigEnum.AllDirection, StringComparison.OrdinalIgnoreCase)
                   || code.Equals("all", StringComparison.OrdinalIgnoreCase)
                ? null
                : code;
        }

        private static IEnumerable<ConfigRow> Config(List<ConfigRow> configDataList, string configTypeCode)
            => configDataList.Where(item => !string.IsNullOrWhiteSpace(item.TypeCode)
                                            && item.TypeCode!.Equals(configTypeCode,
                                                StringComparison.OrdinalIgnoreCase));

        #endregion

        #region Gộp và dựng ma trận

        /// <summary>Gom dòng gộp của SQL về (zone hàng, hướng, giờ).</summary>
        private static Dictionary<MatrixKey, MatrixAccumulator> Rollup(
            List<TrafficMatrixRawRow> rawRows, Dictionary<string, string> leafToRow)
        {
            var buckets = new Dictionary<MatrixKey, MatrixAccumulator>();

            foreach (var row in rawRows)
            {
                if (string.IsNullOrWhiteSpace(row.ZoneId)) continue;
                if (row.Hour is < 0 or >= TrafficMatrixConst.HoursPerDay) continue;

                var zoneId = leafToRow.TryGetValue(row.ZoneId!, out var rowZoneId) ? rowZoneId : row.ZoneId!;
                var key = new MatrixKey(zoneId, row.Direction?.Trim() ?? string.Empty, row.Hour);

                if (!buckets.TryGetValue(key, out var acc))
                    buckets[key] = acc = new MatrixAccumulator();

                acc.Add(row);
            }

            return buckets;
        }

        private static TrafficMatrixGroupOutput BuildGroup(
            TrafficMatrixDirection direction, List<ZoneOutput> rowZones,
            Dictionary<MatrixKey, MatrixAccumulator> buckets, MatrixBands bands)
            => new()
            {
                DirectionCode = string.IsNullOrEmpty(direction.Code) ? null : direction.Code,
                DirectionName = direction.Name,
                Rows = rowZones.Select(zone => BuildRow(zone, direction.Code, buckets, bands)).ToList()
            };

        private static TrafficMatrixRowOutput BuildRow(
            ZoneOutput zone, string directionCode,
            Dictionary<MatrixKey, MatrixAccumulator> buckets, MatrixBands bands)
        {
            var row = new TrafficMatrixRowOutput
            {
                ZoneId = zone.ID,
                ZoneCode = zone.Code,
                ZoneName = zone.Name,
                FromKmNumber = zone.FromKmNumber,
                FromMetNumber = zone.FromMetNumber,
                ToKmNumber = zone.ToKmNumber,
                ToMetNumber = zone.ToMetNumber,
                MaxSpeed = zone.MaxSpeed
            };

            double daySpeedSum = 0;
            var daySpeedSamples = 0;

            for (var hour = 0; hour < TrafficMatrixConst.HoursPerDay; hour++)
            {
                buckets.TryGetValue(new MatrixKey(zone.ID ?? string.Empty, directionCode, hour), out var acc);

                var avgSpeed = acc?.AvgSpeed;
                var vehicleCount = acc?.VehicleCount ?? 0;

                row.Cells.Add(new TrafficMatrixCellOutput
                {
                    Hour = hour,
                    AvgSpeed = avgSpeed,
                    VehicleCount = vehicleCount,
                    SpeedSampleCount = acc?.SpeedSampleCount ?? 0,
                    SpeedBandIndex = TrafficMatrixConst.BandIndexOf(bands.Speed, avgSpeed),
                    VolumeBandIndex = TrafficMatrixConst.BandIndexOf(bands.Volume, vehicleCount)
                });

                row.DayVehicleCount += vehicleCount;

                if (acc is null) continue;
                daySpeedSum += acc.SpeedSum;
                daySpeedSamples += acc.SpeedSampleCount;
            }

            row.DayAvgSpeed = daySpeedSamples == 0 ? null : Math.Round(daySpeedSum / daySpeedSamples, 1);
            row.DaySpeedBandIndex = TrafficMatrixConst.BandIndexOf(bands.Speed, row.DayAvgSpeed);

            return row;
        }

        #endregion

        #region Kiểu nội bộ

        /// <summary>Một dòng đã gộp ở SQL: (ZoneId, Direction, giờ).</summary>
        private sealed class TrafficMatrixRawRow
        {
            public string? ZoneId { get; set; }
            public string? Direction { get; set; }
            public int Hour { get; set; }
            public int VehicleCount { get; set; }

            /// <summary><c>SUM(Speed)</c> — null khi cả nhóm không có bản ghi nào có vận tốc.</summary>
            public float? SpeedSum { get; set; }

            /// <summary><c>COUNT(Speed)</c> — trọng số khi gộp tiếp.</summary>
            public int SpeedSampleCount { get; set; }
        }

        private readonly record struct MatrixKey(string ZoneId, string Direction, int Hour);

        /// <summary>
        /// Một dòng danh mục cấu hình tham số đã kèm mã loại. Khai riêng thay vì dùng DTO của CfgSystem
        /// để chỗ này không phụ thuộc vào cách phân hệ đó chiếu dữ liệu.
        /// </summary>
        private sealed class ConfigRow
        {
            public string? TypeCode { get; set; }
            public string? Code { get; set; }
            public string? Name { get; set; }
            public string? Value { get; set; }
            public int? OrderNo { get; set; }
            public StatusEnum? Status { get; set; }
        }

        private readonly record struct TrafficMatrixDirection(string Code, string? Name);

        /// <summary>
        /// Hai thang màu đã đọc từ cấu hình tham số, gom lại để không phải kéo hai tham số song song
        /// qua từng tầng dựng ma trận.
        /// </summary>
        private sealed record MatrixBands(List<TrafficMatrixBand> Speed, List<TrafficMatrixBand> Volume);

        /// <summary>Bộ cộng của một ô; cộng dồn bằng <c>double</c> để không mất số khi nhiều zone lá.</summary>
        private sealed class MatrixAccumulator
        {
            public int VehicleCount { get; private set; }
            public double SpeedSum { get; private set; }
            public int SpeedSampleCount { get; private set; }

            public double? AvgSpeed => SpeedSampleCount == 0
                ? null
                : Math.Round(SpeedSum / SpeedSampleCount, 1);

            public void Add(TrafficMatrixRawRow row)
            {
                VehicleCount += row.VehicleCount;

                if (row.SpeedSum is not float sum || row.SpeedSampleCount <= 0) return;
                SpeedSum += sum;
                SpeedSampleCount += row.SpeedSampleCount;
            }
        }

        #endregion
    }
}
