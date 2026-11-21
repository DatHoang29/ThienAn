using Modules.TMS.Core.Constants;
using Modules.VDS.Core.Constants;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Services.Vds.Shared.Domain;
using Services.Vds.Shared.Messaging;
using Services.Vds.Shared.State;
using Services.Vds.Configuration;
using Services.Vds.Catalog;
using Services.Vds.Features.Events.Automation;
using Shared.Utility.Utilities;
using Shared.DTO.Constants.Application;

namespace Services.Vds.Features.Events;

/// <summary>Kết quả nâng sự cố.</summary>
public readonly record struct PromoteResult(bool IncidentCreated, string? IncidentId, string? BlockReason);

/// <summary>
/// Author: KienLT
/// Description: Nâng một bản tin sự kiện lên SỰ CỐ, với BA cửa chống trùng xếp tầng — thiếu bất kỳ cửa nào
/// là người trực nhận báo động trùng vài giây một lần cho cùng một sự việc:
///
/// <list type="number">
/// <item><b>Loại được phép</b> — chỉ những loại khai trong cấu hình mới được nâng.</item>
/// <item><b>Khoá ĐIỀU KIỆN + TRẠNG THÁI</b> (<see cref="IncidentRepository.FindOpenByConditionAsync"/>) —
/// cùng thiết bị, cùng loại, cùng đối tượng, và sự cố cũ CÒN MỞ thì là MỘT sự việc.</item>
/// <item><b>VỊ TRÍ + TRẠNG THÁI</b> — hai camera khác nhau tại cùng km/met vẫn là một sự việc ngoài đường.</item>
/// </list>
///
/// Cửa cuối cùng và là cửa DURABLE thật sự nằm ở tầng ghi: ràng buộc UNIQUE theo id bản tin thiết bị
/// (<see cref="IncidentRepository.InsertIdempotentAsync"/>).
///
/// <para><b>Đổi 10/09/2026 — cửa 2 chuyển từ cache sang TRUY VẤN DB.</b> Trước đó cửa này là
/// <c>EventDedupStore</c>: giữ mốc "sự việc này vừa nâng sự cố" trong RAM và ghi ra
/// <c>{StateDirectory}/recent/{loại}.json</c>. Cache đó chỉ biết THỜI GIAN chứ không biết TRẠNG THÁI sự cố,
/// mà trạng thái lại do TMS đổi (xác thực / đang xử lý / kết thúc / huỷ) và VDS không được thông báo. Hệ quả
/// thực tế: người trực kết thúc sự cố xong, sự việc tái diễn ngay sau đó vẫn bị chặn cho tới khi hết cửa sổ
/// (với cháy là 600 giây) — tức mất báo động. Nay cả hai cửa cửa sổ đều truy vấn thẳng bảng sự cố và đều lọc
/// <c>State</c> ∈ {mở}, nên "đã kết thúc" ⇒ được báo lại ngay. Khoá điều kiện được lưu bền vững vào cột
/// <c>TmsIncident.ConditionKey</c> nên cửa này sống qua restart mà không cần file trạng thái nào.</para>
///
/// Bản tin trùng chỉ trả về sự cố đang mở, KHÔNG ghi và KHÔNG phát bản tin.
/// Created date: 26/08/2026
/// </summary>
public sealed class IncidentPromoter
{
    /// <summary>Nguồn phát hiện sự cố — mã của danh mục nguồn phát hiện.</summary>
    private const string SourceDetectVds = "vds";

    /// <summary>Độ dài cột danh sách thiết bị của bảng sự cố.</summary>


    private readonly IncidentRepository _incidents;
    private readonly VdsCatalogRepository _catalog;
    private readonly ConfigDataManager _configData;

    private readonly VdsPublisher _publisher;
    private readonly IncidentAutomationQueue _automation;
    private readonly IVdsConfigManager _config;
    private readonly ILogger<IncidentPromoter> _logger;


    private readonly IncidentTypeCache _incidentTypes;

    /// <summary>Một dòng danh mục loại đã tra được: id để lưu, mã và tên để đặt mã/tên sự cố.</summary>
    private readonly record struct EventTypeRef(string? Id, string Code, string? Name, string? Group = null, string? WarningType = null, string? Icon = null, int? Priority = null);

    public IncidentPromoter(
        IncidentRepository incidents,
        ConfigDataManager configData,
        ZoneManager zones,
        VdsPublisher publisher,
        IncidentAutomationQueue automation,
        IVdsConfigManager config,
        ILogger<IncidentPromoter> logger,
        IncidentTypeCache incidentTypes, VdsCatalogRepository catalog)
    {
        _incidents = incidents;
        _catalog = catalog;
        _configData = configData;

        _publisher = publisher;
        _automation = automation;
        _config = config;
        _logger = logger;
        _incidentTypes = incidentTypes;
    }

    /// <summary>
    /// Xử lý một bản tin đã ghi dữ liệu thô xong.
    ///
    /// Lỗi DB KHÔNG bị bắt: Wolverine phải thấy để retry, và cổng UNIQUE làm lần ghi lại an toàn.
    /// </summary>
    public async Task<PromoteResult> PromoteAsync(
        VdsEventContext ctx, EventDecision decision, DateTime occurredUtc, CancellationToken ct, bool recovery = false)
    {
        var events = ctx.Events;
        var row = ctx.Row;
        // Tên AID của bản tin (luật, ánh xạ loại TMS và cửa chống trùng đều cấu hình theo AID); EventType đã là mã lưu trữ.
        var eventType = row.AidEventType ?? row.EventType ?? string.Empty;

        if (!decision.Promote)
            return new PromoteResult(false, null, decision.SkipReason);



        // Hai mốc thời gian KHÁC NHAU, không được dùng lẫn:
        // - occurredUtc: mốc thiết bị phát hiện (UTC), dùng cho log và cho hành trình.
        // - startLocal: dùng cho mọi cột thời gian của bảng sự cố VÀ cho cả hai truy vấn chống trùng, vì cột
        //   StartDate trong DB là GIỜ HỆ THỐNG. Trộn hai cái này làm sự cố hiện lệch múi giờ và làm cửa
        //   chống trùng so sai 7 tiếng.
        var startLocal = BaseUtil.TimeServer.GetTimeServer(occurredUtc)
                         ?? BaseUtil.TimeServer.GetTimeServer();

        var window = events.DedupWindowFor(eventType);
        var conditionKey = ConditionHash.Create(row.EquipmentId, eventType, decision.SubKey);

        // CỬA ĐIỀU KIỆN + TRẠNG THÁI: truy vấn thẳng bảng sự cố. Trùng CHỈ khi sự cố cùng khoá điều kiện còn
        // ĐANG MỞ — đã kết thúc thì sự việc tái diễn phải được báo lại, kể cả còn trong cửa sổ.
        //
        // Không có pha "chốt mốc" nào phải gọi sau khi ghi: dấu vết chống trùng CHÍNH LÀ dòng sự cố vừa
        // insert (cột ConditionKey). Nhờ vậy nếu insert lỗi và Wolverine retry thì không có mốc nào bị ghi
        // oan để tự chặn lượt sau — đúng tính chất mà bản cache phải tách Peek/Commit mới có được.
        var eventTypeRef = await ResolveEventTypeAsync(eventType, events, ct).ConfigureAwait(false);
        if (SkipReason(eventTypeRef, eventType) is { } skip) return new PromoteResult(false, null, skip);
        var openByCondition = await _incidents.FindOpenByConditionAsync(conditionKey, startLocal, window, ct).ConfigureAwait(false);
        var duplicateId = openByCondition?.ID;

        // Vị trí và phạm vi ảnh hưởng lấy theo DANH MỤC CAMERA, không lấy từ payload thiết bị: cột Location
        // của bảng sự cố là mã tuyến trong danh mục, còn payload chỉ có toạ độ.
        // Tính TRƯỚC cửa vị trí vì cửa đó cũng lọc theo tuyến + hướng.
        var location = ctx.Camera.Location;
        var influenceScope = ctx.Camera.Direction;

        // CỬA VỊ TRÍ CHỈ DÙNG CHO SỰ VIỆC HIỆN TRƯỜNG, KHÔNG dùng cho vi phạm của TỪNG XE.
        //
        // Phân biệt bằng chính danh mục so biển (vds_event_check_plate) — không cần thêm cấu hình:
        // - Loại CÓ so biển (Normal, Speeding...) là vi phạm của một CHIẾC XE cụ thể. Hai xe khác biển vượt
        //   tốc cùng cột mốc trong vài giây là HAI vi phạm, phải ra HAI sự cố. Cửa vị trí khoá theo
        //   (km, met, loại) và KHÔNG có biển, nên nếu áp vào đây sẽ gộp mất một xe.
        // - Loại KHÔNG so biển (cháy, tắc, vật rơi...) là sự việc tại HIỆN TRƯỜNG, không gắn với xe nào.
        //   Hai camera cùng nhìn một đám cháy có ConditionHash khác nhau (khoá theo camera) nên cửa dedup
        //   không thấy nhau; chỉ cửa vị trí mới gộp được về MỘT sự cố.
        var isPerVehicle = events.IsCheckPlateType(eventType);
        if (!isPerVehicle)
        {
            var skipSeconds = events.IncidentSkipFor(eventType);
            var openByLocation = await _incidents
                .FindOpenByLocationAsync(
                    row.KmNumber, row.MetNumber, eventTypeRef.Id, startLocal, skipSeconds,
                    location, influenceScope, ct)
                .ConfigureAwait(false);
            if (openByLocation is not null)
            {
                // Preserve a canceled duplicate for audit without changing the original incident.
                duplicateId ??= openByLocation.ID;
            }
        }

        var code = VdsIncidentNaming.Code(
            eventTypeRef.Code, location, influenceScope, row.KmNumber, row.MetNumber, startLocal);
        var name = VdsIncidentNaming.Name(
            eventTypeRef.Name ?? eventType,
            _configData.NameOf(ConfigDataManager.ZoneLocationType, location),
            _configData.NameOf(ConfigDataManager.DirectionType, influenceScope),
            row.KmNumber, row.MetNumber, startLocal);

        var position = await _catalog.QueryIncidentPositionAsync(
            row.KmNumber, row.MetNumber, location, influenceScope, _config.Current.Catalog.CctvModuleId, ct);
        var recordedLocal = BaseUtil.TimeServer.GetTimeServer();
        var incident = new TmsIncident
        {
            Code = code,
            Name = name,
            EventTypeId = eventTypeRef.Id,
            EventGroup = eventTypeRef.Group,

            // Id bản tin của thiết bị làm khoá chống trùng durable. Bản cũ trỏ sang bảng sự kiện riêng;
            // bảng đó đã bỏ nên dùng thẳng id thiết bị cấp — vẫn truy được về dòng dữ liệu thô.
            EventId = row.VendorEventId.ToString(CultureInfo.InvariantCulture),

            // Khoá điều kiện lưu BỀN VỮNG để cửa chống trùng ở trên truy vấn được. Đây là thứ thay cho file
            // recent/{loại}.json của bản cũ: dấu vết sống cùng dòng sự cố, nên restart không mất và trạng
            // thái đóng/mở luôn khớp với thực tế.
            ConditionKey = conditionKey,

            StartDate = startLocal,
            KmNumber = row.KmNumber,
            MetNumber = row.MetNumber,
            Location = location,
            InfluenceScope = influenceScope,
            Source = SourceDetectVds,
            ZoneId = position.ZoneIds,

            // Thiết bị của sự cố là các camera XEM ĐƯỢC cùng vùng, KHÔNG phải camera đã phát hiện: camera
            // phát hiện không stream được nên giao diện mở lên là màn hình đen.
            EquipmentId = position.EquipmentIds,

            CreatedBy = SourceDetectVds,
            CreatedDate = recordedLocal,
            Metadata = BuildMetadata(row),
            Description = BuildDescription(row),
            State = ((int)global::Shared.DTO.Enums.BaseEnums.TmsIncidentStateEnum.Registration).ToString(CultureInfo.InvariantCulture),
        };

        if (duplicateId is not null) IncidentRecordingConst.ApplyToDuplicate(incident, duplicateId);
        var registrationSource = new Modules.VDS.Core.Dto.VdsRegistrationImageSourceOutput(row.VendorEventId,
            DateTime.SpecifyKind(row.DetectTime, DateTimeKind.Utc), row.EquipmentId!, row.CameraIp, row.AidEventType ?? row.EventType);
        // Chốt chặn phía VDS: chỉ loại có cấu hình tự động TMS cho nguồn VDS (TmsApiOptions.AutomationEventTypes) mới gửi sang TMS.
        var requestAutomation = _config.Current.TmsApi.AllowsAutomationFor(eventTypeRef.Code);
        var result = await _incidents.InsertIdempotentAsync(incident, ct, suppressAutomation: recovery,
            registrationSource: registrationSource, requestAutomation: requestAutomation).ConfigureAwait(false);
        incident.ID = result.Id;
        code = incident.Code ?? code;

        if (!result.Created)
        {
            // Cửa UNIQUE đã chặn: sự cố cho bản tin này có rồi (thường do replay).
            return new PromoteResult(false, result.Id, "duplicate_event_id");
        }

        if (!recovery && duplicateId is null && _config.Current.Publish.PublishIncident)
        {
            await _publisher
                .SendAsync(VdsSubjects.Incident, VdsIncidentPayload.From(incident) with { EventTypeCode = eventTypeRef.Code, EventTypeName = eventTypeRef.Name, WarningType = eventTypeRef.WarningType, Icon = eventTypeRef.Icon, Priority = eventTypeRef.Priority }, ct)
                .ConfigureAwait(false);
        }

        // Nhờ TMS lập lịch kịch bản tự động — CHỈ khi thực sự tạo mới. Bản tin trùng mà vẫn đẩy thì một sự
        // việc sẽ kích kịch bản nhiều lần (VMS đổi nội dung, LCS đóng làn lặp lại).
        //
        // Chỉ ĐẨY VÀO HÀNG ĐỢI, không gọi HTTP tại đây: luồng này đang giữ chỗ trong queue tuần tự của
        // pipeline, một lượt API chậm sẽ chặn mọi bản tin phía sau.
        //
        // Hàng đợi này chỉ là GỢI Ý cho worker chạy sớm. Việc "còn phải bàn giao" đã được ghi bền vững
        // thành marker ngay trong lệnh INSERT sự cố (IncidentRepository.WithPendingDispatch), nên mất hàng
        // đợi vì restart KHÔNG làm mất lệnh lập lịch tự động.
        if (!recovery && duplicateId is null && requestAutomation) _automation.Enqueue(result.Id, code);

        _logger.LogInformation(
            "[Incident] Tạo sự cố {Code} (loại {Type}, km={Km}+{Met}, event={EventId}).",
            code, eventType, row.KmNumber, row.MetNumber, row.VendorEventId);

        return new PromoteResult(true, result.Id, null);
    }

    /// <summary>
    /// Nâng sự cố QUÁ TỐC ĐỘ TRUNG BÌNH của một hành trình A→B (khác quá tốc độ tức thời: tốc độ ở đây là
    /// trung bình cả đoạn = quãng đường / thời gian di chuyển, ghép từ hai lượt của cùng biển số).
    ///
    /// Dùng lại ba cửa chống trùng như <see cref="PromoteAsync"/> nhưng khoá theo (đoạn, biển số) thay vì
    /// theo bản tin thiết bị, và <c>EventId</c> tất định theo hành trình để replay không tạo sự cố thứ hai.
    ///
    /// <para>Vị trí/hướng lấy theo CAMERA CUỐI (điểm B) — nơi hoàn tất hành trình. Loại sự cố tra theo
    /// <see cref="VdsJourneyEventConst.EventTypeName"/> (loại riêng <c>speedingAvg</c>).</para>
    /// </summary>
    /// <param name="toCamera">Camera điểm B của đoạn — nguồn của km/met/vị trí/hướng cho sự cố.</param>
    /// <param name="segmentId">Đoạn A→B, thành phần khoá chống trùng và ghi vào metadata.</param>
    /// <param name="licensePlate">Biển số đã chuẩn hoá của hành trình.</param>
    /// <param name="avgSpeedKmh">Tốc độ trung bình đã tính (km/h).</param>
    /// <param name="fromTimeUtc">Mốc bắt đầu hành trình (UTC) — để dựng EventId tất định.</param>
    /// <param name="occurredUtc">Mốc kết thúc hành trình (UTC) — mốc thời gian của sự cố.</param>
    public async Task<PromoteResult> PromoteAverageSpeedAsync(
        CameraInfo toCamera,
        string segmentId,
        string licensePlate,
        double avgSpeedKmh,
        DateTime fromTimeUtc,
        DateTime occurredUtc,
        CancellationToken ct, bool recovery = false, AverageSpeedDetail? detail = null,
        Modules.VDS.Core.Dto.VdsRegistrationImageSourceOutput? registrationSource = null)
    {
        var existingJourney = await _incidents.FindJourneyAsync(segmentId, licensePlate, fromTimeUtc, ct).ConfigureAwait(false);
        if (existingJourney is not null)
        {
            await _incidents.EnsureRegistrationImageAsync(existingJourney, registrationSource, ct);
            return new PromoteResult(false, existingJourney.ID, "duplicate_event_id");
        }

        var events = _config.Current.Events;

        // Tên loại, slug điều kiện và công thức EventId lấy từ hợp đồng dùng chung ở Modules.VDS.Core —
        // đổi sang loại sự cố riêng về sau chỉ sửa ở VdsJourneyEventConst, không sửa ở đây.
        const string eventType = VdsJourneyEventConst.EventTypeName;

        // Mốc thời gian: startLocal dùng cho cột thời gian bảng sự cố VÀ cho truy vấn chống trùng (StartDate
        // là giờ hệ thống). occurredUtc chỉ dùng để dựng EventId tất định.
        var startLocal = BaseUtil.TimeServer.GetTimeServer(occurredUtc)
                         ?? BaseUtil.TimeServer.GetTimeServer();

        // Khoá điều kiện theo ĐOẠN + chiều "avg" + biển: hai xe khác nhau vượt tốc TB trên cùng đoạn là hai
        // việc; cùng một xe được ghép lại ở lượt quét sau (cửa sổ chồng lấn) là một việc.
        var subKey = VdsJourneyEventConst.DedupSubKey(licensePlate);
        var window = events.DedupWindowFor(eventType);
        var conditionKey = ConditionHash.Create(segmentId, eventType, subKey);

        // Cùng cửa điều kiện + trạng thái như PromoteAsync: truy vấn DB, trùng chỉ khi sự cố cũ CÒN MỞ.
        var eventTypeRef = await ResolveEventTypeAsync(eventType, events, ct).ConfigureAwait(false);
        if (SkipReason(eventTypeRef, eventType) is { } skip) return new PromoteResult(false, null, skip);
        var openByCondition = await _incidents.FindOpenByConditionAsync(conditionKey, startLocal, window, ct).ConfigureAwait(false);
        var duplicateId = openByCondition?.ID;

        // Quá tốc độ TRUNG BÌNH dùng loại riêng 'speedingAvg' trong TmsEventType (từ 29/09/2026).

        var location = toCamera.Location;
        var influenceScope = toCamera.Direction;

        // KHÔNG dùng cửa vị trí ở đây: quá tốc độ TRUNG BÌNH luôn là vi phạm của MỘT chiếc xe (khoá chống
        // trùng đã có biển số trong subKey). Hai xe khác biển hoàn tất cùng một đoạn trong cùng cửa sổ là
        // HAI vi phạm — gộp theo (km, met, loại) sẽ làm mất một xe. Xem giải thích ở PromoteAsync.
        // Cửa dedup theo (đoạn + biển) ở trên và cửa UNIQUE theo EventId bên dưới đã đủ chống trùng.

        var code = VdsIncidentNaming.Code(
            eventTypeRef.Code, location, influenceScope, toCamera.Km, toCamera.Met, startLocal);
        var name = VdsIncidentNaming.Name(
            eventTypeRef.Name ?? eventType,
            _configData.NameOf(ConfigDataManager.ZoneLocationType, location),
            _configData.NameOf(ConfigDataManager.DirectionType, influenceScope),
            toCamera.Km, toCamera.Met, startLocal);

        var position = await _catalog.QueryIncidentPositionAsync(
            toCamera.Km, toCamera.Met, location, influenceScope, _config.Current.Catalog.CctvModuleId, ct);
        var recordedLocal = BaseUtil.TimeServer.GetTimeServer();
        var incident = new TmsIncident
        {
            Code = code,
            Name = name,
            EventTypeId = eventTypeRef.Id,
            EventGroup = eventTypeRef.Group,

            // EventId tất định theo hành trình: khoá chống trùng durable (UNIQUE TenantId, EventId), replay
            // cùng hành trình không sinh sự cố thứ hai. Công thức nằm ở VdsJourneyEventConst.EventId — trước
            // 25/09/2026 nó bị viết lại ở đây và một lần nữa ở VdsJourneyKey bên WebAPI, lệch một ký tự là
            // bên đọc không tìm thấy gì mà không có lỗi nào để lần ra.
            EventId = VdsJourneyEventConst.EventId(segmentId, licensePlate, fromTimeUtc),

            // Khoá điều kiện bền vững cho cửa chống trùng (xem PromoteAsync).
            ConditionKey = conditionKey,

            StartDate = startLocal,
            KmNumber = toCamera.Km,
            MetNumber = toCamera.Met,
            Location = location,
            InfluenceScope = influenceScope,
            Source = SourceDetectVds,
            ZoneId = position.ZoneIds,
            EquipmentId = position.EquipmentIds,
            CreatedBy = SourceDetectVds,
            CreatedDate = recordedLocal,
            Metadata = BuildAverageSpeedMetadata(segmentId, licensePlate, avgSpeedKmh, detail),
            // Mô tả cho người trực đọc thẳng trên danh sách/chi tiết sự cố: đi đoạn nào, bao lâu, nhanh bao nhiêu.
            Description = BuildAverageSpeedDescription(licensePlate, avgSpeedKmh, detail),
            State = ((int)global::Shared.DTO.Enums.BaseEnums.TmsIncidentStateEnum.Registration).ToString(CultureInfo.InvariantCulture),
        };

        if (duplicateId is not null) IncidentRecordingConst.ApplyToDuplicate(incident, duplicateId);
        // Chốt chặn phía VDS: chỉ loại có cấu hình tự động TMS cho nguồn VDS (TmsApiOptions.AutomationEventTypes) mới gửi sang TMS.
        var requestAutomation = _config.Current.TmsApi.AllowsAutomationFor(eventTypeRef.Code);
        var result = await _incidents.InsertIdempotentAsync(incident, ct, suppressAutomation: recovery,
            registrationSource: registrationSource, requestAutomation: requestAutomation).ConfigureAwait(false);
        incident.ID = result.Id;
        code = incident.Code ?? code;

        if (!result.Created)
            return new PromoteResult(false, result.Id, "duplicate_event_id");

        if (!recovery && duplicateId is null && _config.Current.Publish.PublishIncident)
        {
            await _publisher
                .SendAsync(VdsSubjects.Incident, VdsIncidentPayload.From(incident) with { EventTypeCode = eventTypeRef.Code, EventTypeName = eventTypeRef.Name, WarningType = eventTypeRef.WarningType, Icon = eventTypeRef.Icon, Priority = eventTypeRef.Priority }, ct)
                .ConfigureAwait(false);
        }

        //
        // Hàng đợi này chỉ là GỢI Ý cho worker chạy sớm. Việc "còn phải bàn giao" đã được ghi bền vững
        // thành marker ngay trong lệnh INSERT sự cố (IncidentRepository.WithPendingDispatch), nên mất hàng
        // đợi vì restart KHÔNG làm mất lệnh lập lịch tự động.
        if (!recovery && duplicateId is null && requestAutomation) _automation.Enqueue(result.Id, code);

        _logger.LogInformation(
            "[Incident] Tạo sự cố quá tốc độ TB {Code} (đoạn {Segment}, biển {Plate}, {Speed:0.#} km/h, " +
            "km={Km}+{Met}).",
            code, segmentId, PlateRules.Mask(licensePlate), avgSpeedKmh, toCamera.Km, toCamera.Met);

        return new PromoteResult(true, result.Id, null);
    }

    /// <summary>
    /// Metadata JSON cho sự cố quá tốc độ trung bình.
    ///
    /// <para><b>Giữ GỌN</b>: cột Metadata chỉ 512 ký tự và tầng tự động hoá còn ghi thêm khoá của nó
    /// (VdsAutomationDispatch, VdsEventDueV1, VdsDuplicateOf…) — vượt độ dài thì các bước đó bỏ ghi hoặc lỗi.
    /// Thông tin để người đọc nằm ở Description, ở đây chỉ số liệu máy cần.</para>
    /// </summary>
    private static string BuildAverageSpeedMetadata(string segmentId, string plate, double avgSpeedKmh, AverageSpeedDetail? d)
    {
        var data = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [IncidentMetadataConst.AidEventType] = VdsJourneyEventConst.EventTypeName,
            ["SegmentId"] = segmentId,
            ["VehiclePlate"] = plate,
            ["AverageSpeed"] = Math.Round(avgSpeedKmh, 2),
        };
        if (d is not null)
        {
            if (!string.IsNullOrWhiteSpace(d.FromCode)) data["FromCamera"] = d.FromCode!;
            if (!string.IsNullOrWhiteSpace(d.ToCode)) data["ToCamera"] = d.ToCode!;
            data["DistanceMeter"] = d.DistanceMeter;
            data["TravelSeconds"] = d.TravelSeconds;
            if (d.SpeedLimitKmh is double limit)
            {
                // SpeedLimit giữ cho FE/báo cáo sự cố đang hiển thị; SpeedMax cùng giá trị cho token VMS dùng chung.
                data["SpeedLimit"] = Math.Round(limit, 1);
                data[IncidentMetadataConst.SpeedMax] = Math.Round(limit, 1);
            }
            if (d.SpeedMinKmh is double min) data[IncidentMetadataConst.SpeedMin] = Math.Round(min, 1);
        }
        // Giữ AID và giới hạn cột metadata của hồ sơ hành trình.
        while (JsonSerializer.Serialize(data).Length > EntityConst.Length512 && data.Count > 1)
            data.Remove(data.Keys.Last());
        return JsonSerializer.Serialize(data);
    }

    /// <summary>
    /// Mô tả tiếng Việt cho sự cố quá tốc độ trung bình, ví dụ: <c>Xe 51A12345 đi VDS-01 → VDS-03 (3,3 km) trong
    /// 2 phút 00 giây (08:00:00 → 08:02:00), tốc độ trung bình 99,0 km/h, vượt giới hạn 50 km/h (+49,0 km/h).</c>
    /// Cắt theo độ dài cột (512).
    /// </summary>
    internal static string BuildAverageSpeedDescription(string plate, double avgSpeedKmh, AverageSpeedDetail? d)
    {
        var vi = CultureInfo.GetCultureInfo("vi-VN");
        var sb = new StringBuilder();
        sb.Append("Xe ").Append(plate);
        if (d is not null)
        {
            sb.Append(" đi ").Append(d.FromCode ?? "?").Append(" → ").Append(d.ToCode ?? "?");
            sb.Append(" (").Append((d.DistanceMeter / 1000.0).ToString("0.0##", vi)).Append(" km)");
            var travel = TimeSpan.FromSeconds(Math.Max(0, d.TravelSeconds));
            sb.Append(" trong ");
            if (travel.TotalHours >= 1) sb.Append((int)travel.TotalHours).Append(" giờ ");
            sb.Append(travel.Minutes).Append(" phút ").Append(travel.Seconds.ToString("00", vi)).Append(" giây");
            var from = BaseUtil.TimeServer.GetTimeServer(d.FromTimeUtc);
            var to = BaseUtil.TimeServer.GetTimeServer(d.ToTimeUtc);
            if (from is not null && to is not null)
                sb.Append(" (").Append(from.Value.ToString("HH:mm:ss", vi)).Append(" → ")
                  .Append(to.Value.ToString("HH:mm:ss", vi)).Append(')');
        }
        sb.Append(", tốc độ trung bình ").Append(avgSpeedKmh.ToString("0.0", vi)).Append(" km/h");
        if (d?.SpeedLimitKmh is double limit && limit > 0)
            sb.Append(", vượt giới hạn ").Append(limit.ToString("0.#", vi)).Append(" km/h (+")
              .Append((avgSpeedKmh - limit).ToString("0.0", vi)).Append(" km/h)");
        sb.Append('.');
        var text = sb.ToString();
        return text.Length <= 512 ? text : text[..512];
    }

    /// <summary>
    /// Thông tin phụ dạng JSON — thêm khoá mới khi cần thay vì thêm cột. Khoá không có dữ liệu thì bỏ hẳn.
    ///
    /// Chiều dài lấy từ cột đã chuẩn hoá của bản ghi thô (MILIMÉT) rồi quy về MÉT, không đọc lại payload:
    /// cột đó đã đi qua bộ phân cỡ nên phủ cả hai phiên bản payload và cả cấu hình đơn vị của thiết bị. Giữ
    /// đơn vị mét để không phá dữ liệu sự cố cũ.
    ///
    /// <para>Kèm <c>SpeedMin</c>/<c>SpeedMax</c> = ngưỡng TẠI THỜI ĐIỂM phát hiện đã lưu trên bản ghi
    /// thô (<see cref="VdsTrafficData.SpeedMin"/>/<see cref="VdsTrafficData.SpeedMax"/>) để nội dung VMS tự động dùng làm
    /// token (ví dụ <c>VƯỢT TỐC ĐỘ {VehicleSpeed}/{SpeedMax} KM/H</c>). Đọc từ bản ghi chứ không tra lại ⇒ sự cố và
    /// QuestDB luôn cùng một giá trị. Không có ngưỡng ⇒ bỏ khoá.</para>
    /// </summary>
    // KienLT - 23/10/2026: dựng nội dung qua helper dùng chung với luồng "tạo sự cố từ sự kiện" của WebAPI.
    private static string? BuildMetadata(VdsTrafficData row) =>
        Modules.TMS.Core.Helper.IncidentVdsDetailHelper.Metadata(row.AidEventType ?? row.EventType, row.SpeedMin, row.SpeedMax,
            row.LicensePlate, row.Speed, row.LengthMm, row.VehicleType, row.VehicleColor, row.VehicleSize, row.Lane);

    private static string? BuildDescription(VdsTrafficData row) =>
        Modules.TMS.Core.Helper.IncidentVdsDetailHelper.Description(row.LicensePlate, row.Speed, row.Lane);

    /// <summary>
    /// Tra danh mục theo ánh xạ; thiếu ánh xạ dùng unknown, không suy mã từ slug.
    /// Cache theo mã đã resolve; refresh catalog mã 1/0 xóa cả kết quả thành công và thất bại.
    ///
    /// Tra THẤT BẠI chỉ được nhớ trong một khoảng ngắn, không nhớ vĩnh viễn: danh mục seed sau lúc service
    /// đã chạy vẫn phải có hiệu lực mà không cần restart. Khi đó vẫn trả về mã đã suy ra để mã sự cố không
    /// mất thành phần loại.
    /// </summary>
    private async Task<EventTypeRef> ResolveEventTypeAsync(
        string eventType, EventOptions events, CancellationToken ct)
    {
        var code = ResolveEventTypeCode(eventType, events);
        // Unknown không bao giờ ghi hồ sơ: khỏi tra danh mục (và khỏi cảnh báo khi môi trường chưa seed unknown).
        if (VdsEventGate.IsUnknown(code)) return new EventTypeRef(null, code, null);
        var row = await _incidentTypes.GetAsync(code, events.EventTypeLookupMissTtlSeconds, async () =>
        {
            var found = await _incidents.FindTypeByCodeAsync(code, ct).ConfigureAwait(false);
            if (found is { } value && !string.IsNullOrWhiteSpace(value.Id)) return found;
            _logger.LogWarning(
                "[Incident] Không có loại sự cố '{Code}' cho '{Type}'. Raw vẫn được lưu; bỏ tạo hồ sơ và automation. " +
                "Kiểm tra vds_mapping_event_type và TmsEventType, sau đó gửi refresh mã 1 hoặc 0.",
                code, eventType);
            return null;
        }).ConfigureAwait(false);

        return row is { } resolved
            ? new EventTypeRef(resolved.Id, code, resolved.Name, resolved.Group, resolved.WarningType, resolved.Icon, resolved.Priority)
            : new EventTypeRef(null, code, null);
    }

    /// <summary>
    /// Loại AID này có được ghi hồ sơ <c>TmsIncident</c> không. Cho bên gọi kiểm TRƯỚC những
    /// việc tốn kém chỉ phục vụ hồ sơ (ví dụ đọc nguồn ảnh của hành trình từ QuestDB).
    /// </summary>
    public async Task<bool> RecordsIncidentAsync(string aidEventType, CancellationToken ct)
    {
        var events = _config.Current.Events;
        var eventTypeRef = await ResolveEventTypeAsync(aidEventType, events, ct).ConfigureAwait(false);
        return VdsEventGate.SkipReason(eventTypeRef.Code, eventTypeRef.Group, eventTypeRef.Id is not null) is null;
    }

    /// <summary>
    /// Lý do không ghi hồ sơ (xem <see cref="VdsEventGate"/>), kèm log. Bỏ qua sự kiện/unknown
    /// là hành vi bình thường nên chỉ Debug; thiếu danh mục là lỗi cấu hình nên Warning.
    /// </summary>
    private string? SkipReason(EventTypeRef eventTypeRef, string eventType)
    {
        var reason = VdsEventGate.SkipReason(eventTypeRef.Code, eventTypeRef.Group, eventTypeRef.Id is not null);
        if (reason == VdsEventGate.NotConfigured)
            _logger.LogWarning("[Event] Loại {Type}/{Code} thiếu danh mục hoặc Type Event/Incident; không tạo hồ sơ.", eventType, eventTypeRef.Code);
        else if (reason is not null)
            _logger.LogDebug("[Event] Loại {Type}/{Code} bỏ tạo hồ sơ: {Reason}.", eventType, eventTypeRef.Code, reason);
        return reason;
    }

    /// <summary>
    /// Mã TMS chỉ lấy từ vds_mapping_event_type, chưa ánh xạ dùng unknown.
    /// </summary>
    private static string ResolveEventTypeCode(string eventType, EventOptions events)
    {
        var mapped = events.MappedEventTypeCode(eventType.Trim());
        if (!string.IsNullOrWhiteSpace(mapped)) return mapped;

        return IncidentEventTypeConst.Unknown;
    }
}


/// <summary>
/// Author: KienLT
/// Description: Chi tiết hành trình để ghi Mô tả và Metadata của sự cố quá tốc độ trung bình.
/// Created date: 30/09/2026
/// </summary>
/// <param name="FromCode">Mã camera đầu (VDS-01…).</param>
/// <param name="ToCode">Mã camera cuối.</param>
/// <param name="DistanceMeter">Khoảng cách đoạn (m).</param>
/// <param name="TravelSeconds">Thời gian đi (giây).</param>
/// <param name="SpeedLimitKmh">Ngưỡng tốc độ đã dùng để xét vi phạm (km/h).</param>
/// <param name="FromTimeUtc">Mốc qua camera đầu (UTC).</param>
/// <param name="ToTimeUtc">Mốc qua camera cuối (UTC).</param>
/// <param name="SpeedMinKmh">Ngưỡng tối thiểu dễ dãi nhất của hai camera (nhỏ nhất) — chỉ để hiển thị/VMS, không dùng xét vi phạm.</param>
public sealed record AverageSpeedDetail(
    string? FromCode, string? ToCode, int DistanceMeter, int TravelSeconds, double? SpeedLimitKmh,
    DateTime FromTimeUtc, DateTime ToTimeUtc, double? SpeedMinKmh = null);
