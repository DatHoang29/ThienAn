using Modules.VDS.Core.Dto;
using Modules.VDS.Core.Helper;
using Modules.VDS.Core.Services.Media;
using Modules.TMS.Core.Constants;
using Services.Shared.Persistence;
using Services.Vds.Shared.Persistence;
using Services.Vds.Configuration;
using Shared.DTO.Enums;
using Shared.Utility.Utilities;
using FluentValidation;
using Modules.TMS.Core.Helper;

namespace Services.Vds.Features.Events;

/// <summary>Kết quả ghi sự cố: <c>Created=false</c> nghĩa là đã có sự cố cho cùng sự kiện.</summary>
public readonly record struct IncidentInsertResult(string Id, bool Created);

/// <summary>
/// Author: KienLT
/// Description: Đọc/ghi bảng sự cố và danh mục loại sự cố trên SQL SERVER.
///
/// <b>Vì sao sự cố ở lại SQL Server</b>: bảng này có VÒNG ĐỜI TRẠNG THÁI (xác nhận, phê duyệt, kết thúc) và
/// được người dùng sửa trên web. QuestDB là cơ sở dữ liệu chuỗi thời gian chỉ ghi thêm, không phù hợp với
/// bảng cần UPDATE từng dòng.
/// Created date: 26/08/2026
/// </summary>
public sealed class IncidentRepository : VdsRepositoryBase
{
    public Task<TmsIncident?> FindJourneyAsync(string segmentId, string plate, DateTime fromTime, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var keys = Modules.VDS.Core.Constants.VdsJourneyEventConst.LookupEventIds(segmentId, plate, fromTime);
        return Client.Queryable<TmsIncident>().Where(x => x.TenantId == TenantId && keys.Contains(x.EventId!)).FirstAsync();
    }

    private readonly IVdsConfigManager _config;
    private readonly VdsRegistrationImageStore _registrationImages;
    public IncidentRepository(DatabaseManager databaseManager, IVdsConfigManager config, VdsRegistrationImageStore registrationImages)
        : base(databaseManager, config)
    {
        _config = config;
        _registrationImages = registrationImages;
    }

    /// <summary>
    /// Ghi sự cố, LUỸ ĐẲNG theo <c>EventId</c> (id bản tin của thiết bị).
    ///
    /// Đây là cổng chống replay theo bản tin: ConditionKey dùng để gộp sự việc theo trạng thái, còn khi
    /// replay thì ràng buộc UNIQUE trên (TenantId, EventId) giữ danh tính bền vững. Cùng một bản tin
    /// không thể sinh sự cố thứ hai.
    ///
    /// Xử lý trùng theo cách an toàn với race: thử insert, lỗi thì TRA LẠI — hai tiến trình cùng ghi thì bên
    /// thua vẫn nhận được id của bản ghi đã có thay vì ném lỗi ra ngoài.
    ///
    /// <paramref name="requestAutomation"/> (KienLT - 14/10/2026): bên gọi đã đối chiếu loại sự kiện với danh sách
    /// loại suy từ cấu hình tự động TMS (<c>TmsApiOptions.AutomationEventTypes</c>); false thì KHÔNG gắn marker nhờ TMS lập lịch (marker là nguồn chính của việc gọi
    /// API, hàng đợi chỉ là gợi ý). Khác <paramref name="suppressAutomation"/>: không đánh dấu VdsAutomationSuppressed.
    /// </summary>
    public async Task<IncidentInsertResult> InsertIdempotentAsync(
        TmsIncident incident, CancellationToken ct = default, bool suppressAutomation = false,
        VdsRegistrationImageSourceOutput? registrationSource = null, bool requestAutomation = true)
    {
        ArgumentNullException.ThrowIfNull(incident);
        ct.ThrowIfCancellationRequested();

        var existing = await FindByEventIdAsync(incident.EventId, ct).ConfigureAwait(false);
        if (existing is not null)
        {
            await EnsureRegistrationImageAsync(existing, registrationSource, ct);
            return new IncidentInsertResult(existing.ID, false);
        }

        if (string.IsNullOrEmpty(incident.ID)) incident.ID = Guid.NewGuid().ToString();
        incident.TenantId = TenantId;
        // Different vehicles can share the same detection second. Keep the manual code format;
        // choose a distinct value when that generated code is already occupied.
        if (await Client.Queryable<TmsIncident>().Where(x => x.TenantId == TenantId
            && x.IsDelete == null && x.Code == incident.Code).AnyAsync())
            incident.Code = IncidentNamingHelper.Disambiguate(incident.Code!, incident.EventId ?? incident.ID);
        incident.CreateTime = BaseUtil.TimeServer.GetTimeServer();
        incident.CreateUId = ServiceUserId;
        if (suppressAutomation && incident.EventGroup == IncidentEventGroupConst.Event)
        {
            var metadata = string.IsNullOrWhiteSpace(incident.Metadata) ? new Newtonsoft.Json.Linq.JObject()
                : Newtonsoft.Json.Linq.JObject.Parse(incident.Metadata);
            metadata["VdsAutomationSuppressed"] = true;
            incident.Metadata = metadata.ToString(Newtonsoft.Json.Formatting.None);
        }
        if (!suppressAutomation && requestAutomation && _config.Current.TmsApi.Enabled
            && IncidentEventGroupConst.AllowsProcessing(incident.EventGroup, incident.State))
            incident.Metadata = WithPendingDispatch(incident.Metadata);

        // Apply the registration rules used by the manual API after adding service-owned fields.
        new Modules.TMS.Core.Validators.IncidentEntityValidator().ValidateAndThrow(incident);

        // KHÔNG gán IsDelete: cột này là MỐC THỜI GIAN xoá mềm, null nghĩa là chưa xoá. Gán giá trị vào đây
        // là đánh dấu bản ghi đã xoá ngay lúc tạo.

        try
        {
            await Client.Insertable(incident).ExecuteCommandAsync(ct);
            await EnsureRegistrationImageAsync(incident, registrationSource, ct);
            return new IncidentInsertResult(incident.ID, true);
        }
        catch (Exception ex) when (ex is SqlSugarException or System.Data.Common.DbException)
        {
            // Có thể là trùng khoá do một tiến trình khác vừa ghi. Tra lại: có rồi thì coi như không tạo
            // mới, không có thì lỗi là thật và phải ném để Wolverine retry.
            var raced = await FindByEventIdAsync(incident.EventId, ct).ConfigureAwait(false);
            if (raced is not null)
            {
                await EnsureRegistrationImageAsync(raced, registrationSource, ct);
                return new IncidentInsertResult(raced.ID, false);
            }

            throw;
        }
    }

    public Task EnsureRegistrationImageAsync(TmsIncident incident, VdsRegistrationImageSourceOutput? source, CancellationToken ct)
    {
        var registration = incident.CreatedDate ?? incident.StartDate;
        if (source is null || registration is null || !VdsBaseHelper.MediaPathHp.IsVdsRegistration(incident.Source, 1))
            return Task.CompletedTask;
        return _registrationImages.RegisterAsync(incident.TenantId ?? TenantId, incident.ID, registration.Value,
            BaseUtil.TimeServer.ToUtc(registration.Value), source, ct);
    }

    public static string WithPendingDispatch(string? metadata)
    {
        var json = string.IsNullOrWhiteSpace(metadata) ? new Newtonsoft.Json.Linq.JObject()
            : Newtonsoft.Json.Linq.JObject.Parse(metadata);
        json["VdsAutomationDispatch"] = "pending";
        return json.ToString(Newtonsoft.Json.Formatting.None);
    }

    public Task<List<TmsIncident>> ReadPendingDispatchAsync(DateTime sinceLocal, CancellationToken ct, string? afterId = null)
    {
        var tenant = TenantId;
        return Client.Queryable<TmsIncident>().Where(x => x.TenantId == tenant && x.IsDelete == null
                && x.CreateTime >= sinceLocal && x.Metadata != null
                && x.Metadata.Contains("\"VdsAutomationDispatch\":\"pending\""))
            .WhereIF(!string.IsNullOrEmpty(afterId), x => SqlFunc.GreaterThan(x.ID, afterId))
            .OrderBy(x => x.ID).Take(500).ToListAsync(ct);
    }

    public async Task<int> ExpireDispatchAsync(DateTime cutoff, CancellationToken ct)
    {
        var tenant = TenantId;
        var rows = await Client.Queryable<TmsIncident>().Where(x => x.TenantId == tenant && x.IsDelete == null
            && x.CreateTime < cutoff && x.Metadata != null
            && x.Metadata.Contains("\"VdsAutomationDispatch\":\"pending\""))
            .OrderBy(x => x.ID).Take(500).ToListAsync(ct);
        var count = 0;
        foreach (var row in rows) count += await UpdateDispatchAsync(row, "expired", "RetentionExceeded", ct);
        return count;
    }

    public async Task<int> CompleteDispatchAsync(string id, CancellationToken ct, string? reason = null, bool failed = false)
    {
        var tenant = TenantId;
        // A metadata compare-and-set preserves a receipt written concurrently by the API.
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var row = await Client.Queryable<TmsIncident>().Where(x => x.ID == id && x.TenantId == tenant).FirstAsync(ct);
            if (row == null) return 0;
            var result = await UpdateDispatchAsync(row, failed ? "failed" : "done", reason, ct);
            if (result != 0) return result;
        }
        return 0;
    }

    private async Task<int> UpdateDispatchAsync(TmsIncident row, string status, string? reason, CancellationToken ct)
    {
        var json = Newtonsoft.Json.Linq.JObject.Parse(row.Metadata ?? "{}");
        if ((string?)json["VdsAutomationDispatch"] != "pending") return 0;
        json["VdsAutomationDispatch"] = status;
        if (reason != null) json["VdsAutomationDispatchReason"] = reason;
        var updated = json.ToString(Newtonsoft.Json.Formatting.None);
        if (updated.Length > 512) throw new InvalidOperationException("Incident dispatch metadata exceeds 512 characters.");
        return await Client.Updateable<TmsIncident>().SetColumns(x => new TmsIncident { Metadata = updated })
            .Where(x => x.ID == row.ID && x.TenantId == row.TenantId && x.Metadata == row.Metadata)
            .ExecuteCommandAsync(ct);
    }

    /// <summary>Sự cố đã sinh từ một bản tin cụ thể; <paramref name="eventId"/> rỗng ⇒ null.</summary>
    public async Task<TmsIncident?> FindByEventIdAsync(string? eventId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(eventId)) return null;

        var key = eventId.Trim();
        var tenantId = TenantId;

        return await EventIdentityQuery(Client, tenantId, key).FirstAsync(ct);
    }

    // Replay identity is independent of lifecycle/soft deletion. The UNIQUE EventId index
    // also includes deleted incidents; filtering them here would cause endless insert retries.
    internal static ISugarQueryable<TmsIncident> EventIdentityQuery(ISqlSugarClient db, string tenantId, string key) =>
        db.Queryable<TmsIncident>().Where(x => x.TenantId == tenantId && x.EventId == key);

    /// <summary>
    /// Trạng thái sự cố tính là ĐANG MỞ. Cột <c>State</c> lưu dạng CHUỖI số nên phải so bằng chuỗi.
    ///
    /// Sự cố đã đóng (Skipped=0, Finished=4, Canceled=5) KHÔNG chặn bản tin mới: nếu tính cả trạng thái đóng
    /// thì một sự cố đã kết thúc vẫn khoá vĩnh viễn mọi sự cố cùng điều kiện/vị trí sau nó.
    /// </summary>
    private static readonly string[] OpenStates =
    {
        ((int)BaseEnums.TmsIncidentStateEnum.Registration).ToString(),
        ((int)BaseEnums.TmsIncidentStateEnum.Verified).ToString(),
        ((int)BaseEnums.TmsIncidentStateEnum.InProgress).ToString(),
    };

    /// <summary>
    /// Sự cố ĐANG MỞ cùng KHOÁ ĐIỀU KIỆN (<see cref="TmsIncident.ConditionKey"/>) trong
    /// <paramref name="windowSeconds"/> giây gần nhất — cửa chống trùng thứ nhất khi nâng sự cố.
    ///
    /// <para><b>Vì sao truy vấn DB thay vì cache trong tiến trình.</b> Bản cũ giữ mốc "sự việc này vừa nâng
    /// sự cố" trong RAM + file <c>recent/{loại}.json</c>. Cache đó chỉ biết THỜI GIAN, không biết TRẠNG
    /// THÁI: sự cố người trực đã kết thúc rồi mà bản tin mới vẫn bị chặn cho tới khi hết cửa sổ (với cháy là
    /// 600 giây), tức sự việc tái diễn ngay sau đó KHÔNG được báo. Trạng thái chỉ có ở DB — TMS là nơi đổi
    /// nó (xác thực, xử lý, kết thúc, huỷ) và VDS không hề được thông báo — nên cache không thể đúng.</para>
    ///
    /// <para>Chi phí: thêm MỘT truy vấn cho mỗi bản tin thuộc loại sự cố. Bản tin lượt xe bình thường
    /// (<c>Normal</c>) KHÔNG đi qua đây vì cửa "loại được phép" đã chặn trước, nên lưu lượng của truy vấn này
    /// phụ thuộc số frame qua SKIP và luật; cần đo tải thực tế. Chỉ mục
    /// <c>IX_TmsIncident_ConditionKey</c> (xem migration <c>20260910_TmsIncident_ConditionKey.sql</c>) phủ
    /// đúng bộ cột của truy vấn này.</para>
    ///
    /// <para><b>Cửa sổ vẫn còn tác dụng</b> dù đã có trạng thái: nó là chốt trên để một sự cố bị bỏ quên ở
    /// trạng thái mở không khoá vĩnh viễn mọi báo động cùng điều kiện. Cửa sổ lấy từ danh mục
    /// <c>vds_event_dedup</c> nên người vận hành đổi được mà không cần triển khai lại.</para>
    ///
    /// <para>So với <c>StartDate</c> nên <paramref name="now"/> phải là GIỜ HỆ THỐNG, không phải giờ UTC.</para>
    /// </summary>
    /// <returns>Sự cố đang mở cùng khoá điều kiện, hoặc null nếu không có ⇒ được tạo sự cố mới.</returns>
    public async Task<TmsIncident?> FindOpenByConditionAsync(
        string? conditionKey, DateTime now, int windowSeconds, CancellationToken ct = default)
    {
        // Không có khoá điều kiện thì không có cơ sở gộp — để cửa UNIQUE theo EventId lo.
        if (string.IsNullOrWhiteSpace(conditionKey)) return null;

        // Cửa sổ 0 nghĩa là người vận hành TẮT cửa này; đó là lựa chọn hợp lệ (mọi bản tin thành sự cố).
        if (windowSeconds <= 0) return null;

        var key = conditionKey.Trim();
        var since = now.AddSeconds(-windowSeconds);
        var tenantId = TenantId;

        // Sao ra BIẾN CỤC BỘ: SqlSugar dịch biểu thức bằng reflection và không đọc được field private
        // ("Field \"OpenStates\" can't be private"). Tham chiếu thẳng static field trong lambda là lỗi lúc chạy.
        return await OpenConditionQuery(Client, tenantId, key, since, now).FirstAsync(ct);
    }

    internal static ISugarQueryable<TmsIncident> OpenConditionQuery(
        ISqlSugarClient db, string tenantId, string key, DateTime since, DateTime now)
    {
        var openStates = OpenStates;
        return db.Queryable<TmsIncident>()
            .Where(x => x.IsDelete == null && x.TenantId == tenantId)
            .Where(x => x.ConditionKey == key)
            .Where(x => x.StartDate != null && x.StartDate >= since && x.StartDate <= now)
            .Where(x => openStates.Contains(x.State) && (!(x.EventGroup == IncidentEventGroupConst.Event || (x.EventGroup == null && SqlFunc.Subqueryable<TmsEventType>().Where(t => t.ID == x.EventTypeId && t.IsDelete == null && t.Type.Trim().ToLower() == IncidentEventGroupConst.Event).Any()))))
            .OrderBy(x => x.StartDate, OrderByType.Desc);
    }

    /// <summary>
    /// Sự cố ĐANG MỞ (State ∈ {Registration=1, Verified=2, InProgress=3}) cùng vị trí và cùng loại trong
    /// <paramref name="skipSeconds"/> giây gần nhất.
    ///
    /// Đây là cửa chống trùng thứ hai, độc lập với khoá điều kiện: hai camera khác nhau tại cùng một điểm
    /// vẫn là một sự việc ngoài đường. So với <c>StartDate</c> nên <paramref name="now"/> phải là GIỜ HỆ
    /// THỐNG, không phải giờ UTC.
    ///
    /// Sự cố đã ĐÓNG (Skipped=0, Finished=4, Canceled=5) KHÔNG được tính là trùng — cho phép tạo sự cố mới
    /// cùng vị trí sau khi cái cũ đã kết thúc.
    ///
    /// <b>THIẾU VỊ TRÍ ⇒ KHÔNG GỘP</b>: km/met đều rỗng thì trả null ngay. Nếu để nguyên, điều kiện
    /// <c>KmNumber == null AND MetNumber == null</c> sẽ khớp MỌI sự cố thiếu vị trí và gộp bừa các sự việc
    /// chẳng liên quan gì nhau — "không biết ở đâu" không phải căn cứ để kết luận "cùng một chỗ".
    ///
    /// <paramref name="location"/> (tuyến) và <paramref name="influenceScope"/> (hướng) chỉ được đưa vào
    /// điều kiện khi có giá trị. Ở tuyến hiện tại mỗi <c>(km, met)</c> chỉ có MỘT camera nên chúng gần như
    /// không đổi kết quả; giữ trong khoá để sau này lắp hai camera đối diện tại cùng cột mốc thì hai hướng
    /// không bị gộp thành một sự cố.
    ///
    /// <paramref name="eventTypeId"/> rỗng ⇒ bỏ cửa vị trí để không gộp các loại sự cố khác nhau.
    /// Cửa ConditionKey và EventId vẫn hoạt động.
    /// </summary>
    public async Task<TmsIncident?> FindOpenByLocationAsync(
        int? km,
        int? met,
        string? eventTypeId,
        DateTime now,
        int skipSeconds,
        string? location = null,
        string? influenceScope = null,
        CancellationToken ct = default)
    {
        if (skipSeconds <= 0) return null;
        if (string.IsNullOrWhiteSpace(eventTypeId)) return null;

        // Không có vị trí thì không có cơ sở để gộp theo vị trí.
        if (km is null && met is null) return null;

        var since = now.AddSeconds(-skipSeconds);
        var tenantId = TenantId;
        var hasType = !string.IsNullOrWhiteSpace(eventTypeId);
        var typeId = eventTypeId?.Trim();
        var hasLocation = !string.IsNullOrWhiteSpace(location);
        var locationValue = location?.Trim();
        var hasScope = !string.IsNullOrWhiteSpace(influenceScope);
        var scopeValue = influenceScope?.Trim();

        // CHỈ tính sự cố ĐANG MỞ là trùng — xem <see cref="OpenStates"/>.
        var openStates = OpenStates;

        return await Client.Queryable<TmsIncident>()
            .Where(x => x.IsDelete == null && x.TenantId == tenantId)
            .Where(x => x.KmNumber == km && x.MetNumber == met)
            .Where(x => x.StartDate != null && x.StartDate >= since && x.StartDate <= now)
            .Where(x => openStates.Contains(x.State) && (!(x.EventGroup == IncidentEventGroupConst.Event || (x.EventGroup == null && SqlFunc.Subqueryable<TmsEventType>().Where(t => t.ID == x.EventTypeId && t.IsDelete == null && t.Type.Trim().ToLower() == IncidentEventGroupConst.Event).Any()))))
            .WhereIF(hasType, x => x.EventTypeId == typeId)
            .WhereIF(hasLocation, x => x.Location == locationValue)
            .WhereIF(hasScope, x => x.InfluenceScope == scopeValue)
            .OrderBy(x => x.StartDate, OrderByType.Desc)
            .FirstAsync(ct);
    }

    /// <summary>Một dòng danh mục loại sự cố đã tra được.</summary>
    public readonly record struct EventTypeRow(string Id, string? Code, string? Name, string? Group = null, string? WarningType = null, string? Icon = null, int? Priority = null);

    /// <summary>Loại sự cố theo <c>Code</c> trong danh mục; không có ⇒ null.</summary>
    public async Task<EventTypeRow?> FindTypeByCodeAsync(string? code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;

        var key = code.Trim();
        var row = await Client.Queryable<TmsEventType>()
            .Where(x => x.IsDelete == null && x.Code == key)
            .Select(x => new { x.ID, x.Code, x.Name, x.Type, x.WarningType, x.Url, x.Priority })
            .FirstAsync(ct);

        return row is null ? null : new EventTypeRow(row.ID, row.Code, row.Name, IncidentEventGroupConst.Normalize(row.Type), row.WarningType, row.Url, row.Priority);
    }
}
