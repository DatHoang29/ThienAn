using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Dto.EventSnapshot;
using Modules.VDS.Infrastructure.Services.Config;
using Modules.VDS.Core.Constants;
using Modules.VDS.Core.Helper;
using Modules.VDS.Core.Entities;
using Modules.VDS.Core.Exceptions;
using Shared.DTO.Enums;
using System.Text.RegularExpressions;
using Modules.VDS.Infrastructure.Services.Config.Rules;
using Shared.Core.Utilities.Constants;

namespace Modules.VDS.Infrastructure.Services.Config;

/// <summary>
/// Author: KienLT
/// Description: Đường kiểm tra DUY NHẤT cho mọi lần ghi bảng <c>VdsConfig</c>.
///
/// <para><b>Vì sao có lớp này.</b> Bảng <c>VdsConfig</c> có hai API ghi — <c>vdsConfig</c> (CRUD theo dòng,
/// cho người quản trị) và <c>vdsConfiguration</c> (upsert theo mã, cho người vận hành). Trước 22/09/2026 mỗi
/// bên tự kiểm, và ba chỗ lệch nhau đã được xác minh:</para>
/// <list type="bullet">
/// <item>Thời lượng: <c>Configuration</c> chỉ khớp regex hậu tố nên <c>31d</c> LƯU ĐƯỢC, còn
/// <c>VdsConfigRules</c> chặn trên 30 ngày. Hệ quả: bên đọc gọi <c>DurationSeconds("31d")</c> nhận null rồi
/// im lặng dùng giá trị rơi về — API đã báo thành công.</item>
/// <item>Tốc độ: <c>Configuration</c> là <c>int</c> 0..300, <c>VdsConfigRules</c> là <c>double</c> hữu hạn
/// ≥ 0. Cùng một giá trị lưu được qua đường này nhưng bị từ chối qua đường kia.</item>
/// <item>Cặp min/max: <c>Config</c> chỉ so với dòng đang <c>Enable</c>, <c>Configuration</c> không lọc
/// <c>Status</c> — nên một dòng đã tắt vẫn chặn được cấu hình đang chạy.</item>
/// </list>
///
/// <para><b>Giữ nguyên HAI API.</b> Lớp này không gộp endpoint, chỉ gộp phần kiểm tra và chuẩn hoá giá trị.
/// Mỗi API vẫn giữ hình dạng request, route và phạm vi mã riêng của nó.</para>
///
/// <para><b>Phân vai:</b> <see cref="VdsConfigRules.Validate"/> giữ luật THUẦN (không cần DB) — bắt buộc, độ
/// dài, dải, định dạng. Lớp này giữ luật cần DB — trùng mã, tham chiếu tồn tại, và so cặp min/max trên bộ
/// cấu hình CÓ HIỆU LỰC.</para>
/// Created date: 22/09/2026
/// </summary>
// KienLT - 31/10/2026: thêm questHaOptions (số node) để kiểm slot lịch backup QuestDB HA trên bộ hiệu lực khi lưu.
public sealed class VdsConfigWriteService(BaseRepository<VdsConfig> repo, IMessageBus bus, IOptions<Shared.QuestHa.Options.QuestHaOptions> questHaOptions) : IScoped
{
    /// <summary>Định dạng một phần tử trong danh sách camera theo hướng.</summary>
    private static readonly Regex EquipmentIdPattern = new(@"^[\w-]+$", RegexOptions.Compiled);

    /// <summary>Độ dài tối đa của một ID camera trong danh sách.</summary>
    private const int MaxEquipmentIdLength = 64;

    /// <summary>
    /// Kiểm tra một lần ghi và trả về giá trị ĐÃ CHUẨN HOÁ để lưu.
    ///
    /// <para>Ném <c>Oops.Oh</c> với khóa localization ở lỗi đầu tiên tìm thấy.</para>
    /// </summary>
    /// <param name="group">Mã nhóm sau khi trim.</param>
    /// <param name="code">Mã cấu hình sau khi trim.</param>
    /// <param name="value">Giá trị sau khi trim.</param>
    /// <param name="name">Tên hiển thị sau khi trim.</param>
    /// <param name="status">Trạng thái SẼ CÓ sau khi ghi — quyết định dòng này có vào bộ hiệu lực hay không.</param>
    /// <param name="currentId">ID dòng đang sửa; để trống khi thêm mới. Dùng để tự loại mình khỏi phép so.</param>
    public async Task<string?> ValidateAsync(string? group, string? code, string? value, string? name,
        BaseEnums.StatusEnum status, string? currentId)
    {
        var error = VdsConfigRules.Validate(group, code, value, name, (int)status);
        if (error != null) throw Oops.Oh(error);

        if (await repo.IsAnyAsync(x => x.IsDelete == null && x.GroupCode == group && x.Code == code && x.ID != currentId))
            throw Oops.Oh(BaseMsg.Vds.Exception.CodeExistInGroup);

        var normalized = value;

        if (group is VdsConfigConst.Group.ImageReuse)
        {
            var tenant = string.IsNullOrWhiteSpace(currentId)
                ? App.User?.FindFirst(ClaimConst.TenantId)?.Value
                : (await repo.GetFirstAsync(x => x.ID == currentId && x.IsDelete == null))?.TenantId;
            if (string.IsNullOrWhiteSpace(tenant) || (await FindEventTypeAsync(code, tenant)).TenantCount != 1)
                throw Oops.Oh(BaseMsg.Vds.Exception.EventTypeNotFound);
        }

        if (group == VdsConfigConst.Group.ListEquipment && !await EquipmentExistsAsync(code))
            throw Oops.Oh(BaseMsg.Vds.Exception.CameraNotFound);

        if (group == VdsConfigConst.Group.MappingEventType)
        {
            // SQL Server so mã không phân biệt hoa thường nhưng QuestDB thì có, và worker lưu nguyên
            // giá trị ánh xạ vào eventType. Lưu đúng cách viết của danh mục để bản thô khớp SSE sự kiện.
            var mappedCode = value?.Trim();
            var catalogCode = string.IsNullOrEmpty(mappedCode) ? null : (await FindEventTypeAsync(mappedCode)).CatalogCode;
            if (string.IsNullOrWhiteSpace(catalogCode)) throw Oops.Oh(BaseMsg.Vds.Exception.EventTypeNotFound);
            normalized = catalogCode;
        }

        if (group is VdsConfigConst.Group.EventSkip or VdsConfigConst.Group.EventDedup && !await EventTypeCodeExistsAsync(code))
            throw Oops.Oh(BaseMsg.Vds.Exception.SkipDedupCodeInvalid);

        // Chỉ loại nhóm event đang bật (trừ unknown) mới lên SSE sự kiện; khai cho loại khác là cấu hình không bao giờ có tác dụng.
        // Danh mục loại sự kiện thuộc TMS nên hỏi qua bus, không đọc thẳng bảng của module khác.
        if (group == VdsConfigConst.Group.EventDisplay && (string.Equals(code?.Trim(), Modules.TMS.Core.Constants.IncidentEventTypeConst.Unknown,
                StringComparison.OrdinalIgnoreCase)
                || !(await bus.InvokeAsync<TmsEventDisplayTypeOutput>(new TmsEventDisplayTypeInput { Code = code })).Exists))
            throw Oops.Oh(BaseMsg.Vds.Exception.EventTypeNotFound);

        if (code is not null && (code.StartsWith(VdsConfigConst.Pattern.CountDirectionEquipmentPrefix, StringComparison.Ordinal)
            || code.StartsWith(VdsConfigConst.Pattern.SpeedDirectionEquipmentPrefix, StringComparison.Ordinal)))
            normalized = await NormalizeEquipmentListAsync(value);

        if (code is not null && code.StartsWith(VdsConfigConst.Pattern.SpeedDirectionEquipmentPrefix, StringComparison.Ordinal))
        {
            var ids = normalized!.Split(',');
            var direction = code[VdsConfigConst.Pattern.SpeedDirectionEquipmentPrefix.Length..];
            if (group != VdsConfigConst.Group.Config || ids.Length != 2 || string.IsNullOrWhiteSpace(direction))
                throw Oops.Oh(BaseMsg.Vds.Exception.EquipmentListInvalid);
            var pair = await repo.Context.Queryable<TmsEquipment>()
                .Where(x => x.IsDelete == null && ids.Contains(x.ID)).Select(x => new { x.DirectionId, x.TenantId }).ToListAsync();
            var tenant = string.IsNullOrWhiteSpace(currentId) ? App.User?.FindFirst(ClaimConst.TenantId)?.Value
                : (await repo.GetFirstAsync(x => x.ID == currentId && x.IsDelete == null))?.TenantId;
            if (pair.Count != 2 || string.IsNullOrWhiteSpace(tenant)
                || pair.Any(x => x.DirectionId?.ToString() != direction || x.TenantId != tenant))
                throw Oops.Oh(BaseMsg.Vds.Exception.EquipmentListInvalid);
        }

        // Chỉ dòng SẼ CÓ HIỆU LỰC mới phải so cặp; dòng sắp bị tắt không thể xung đột với gì.
        if (status == BaseEnums.StatusEnum.Enable)
        {
            await EnsureSpeedPairAsync(group, code, normalized, currentId);
            await EnsureVehicleSizePairAsync(group, code, normalized, currentId);
        }
        // KienLT - 31/10/2026: QuestDB HA kiểm cả khi tắt dòng/đổi mã — bỏ một dòng khỏi bộ cũng có thể làm bộ hỏng (vd về Critical mặc định).
        await EnsureQuestHaEffectiveAsync(group, code, normalized, status, currentId);

        return normalized;
    }

    private Task<bool> EquipmentExistsAsync(string? equipmentId)
        => repo.Context.Queryable<TmsEquipment>()
            .InnerJoin<TmsEquipmentType>((e, t) => e.EquipmentTypeId == t.ID)
            .Where((e, t) => e.IsDelete == null && e.ID == equipmentId && t.IsDelete == null && t.Code == "VDS").AnyAsync();

    private async Task<bool> EventTypeCodeExistsAsync(string? code)
        => (await FindEventTypeAsync(code)).CatalogCode != null;

    /// <summary>Danh mục loại sự kiện thuộc TMS nên hỏi qua bus, không đọc thẳng bảng của module khác.</summary>
    private Task<TmsEventTypeCodeOutput> FindEventTypeAsync(string? code, string? tenantId = null)
        => bus.InvokeAsync<TmsEventTypeCodeOutput>(new TmsEventTypeCodeInput { Code = code, TenantId = tenantId });

    /// <summary>
    /// Danh sách camera theo hướng: tách, bỏ trùng, và bắt buộc mọi ID phải tồn tại trong <c>TmsEquipment</c>.
    ///
    /// <para>Trả về danh sách đã nối lại bằng dấu phẩy để giá trị lưu trong DB luôn ở một dạng duy nhất.</para>
    /// </summary>
    private async Task<string> NormalizeEquipmentListAsync(string? value)
    {
        var ids = (value ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (ids.Length == 0 || ids.Any(x => x.Length > MaxEquipmentIdLength || !EquipmentIdPattern.IsMatch(x)))
            throw Oops.Oh(BaseMsg.Vds.Exception.EquipmentListInvalid);

        var existing = await repo.Context.Queryable<TmsEquipment>()
            .InnerJoin<TmsEquipmentType>((e, t) => e.EquipmentTypeId == t.ID)
            .Where((e, t) => e.IsDelete == null && ids.Contains(e.ID) && t.IsDelete == null && t.Code == "VDS").CountAsync();
        if (existing != ids.Length) throw Oops.Oh(BaseMsg.Vds.Exception.EquipmentListInvalid);

        return string.Join(",", ids);
    }

    /// <summary>
    /// So ngưỡng tốc độ với ngưỡng còn lại của cùng hướng, CHỈ khi ngưỡng đó đang có hiệu lực.
    ///
    /// <para><b>Bộ hiệu lực = <c>Status = Enable</c> và chưa xóa mềm</b> — đúng bộ mà worker đọc
    /// (<c>VdsCatalogRepository.QueryOpConfigAsync</c> lọc <c>Status = 1</c>). Một dòng đang tắt KHÔNG được
    /// chặn dòng đang chạy: nó không nằm trong bộ mà worker thấy.</para>
    /// <para>Vì phép so chạy theo trạng thái SẼ CÓ sau khi ghi, thao tác <b>bật lại</b> một ngưỡng cũng được
    /// kiểm — đó là lúc cặp mới thật sự thành hiệu lực.</para>
    /// </summary>
    private async Task EnsureSpeedPairAsync(string? group, string? code, string? value, string? currentId)
    {
        if (group != VdsConfigConst.Group.Config || !VdsConfigRules.IsRouteSpeedCode(code)) return;

        var mine = VdsConfigRules.SpeedKmh(value);
        if (mine is null) return;

        var peerCode = VdsConfigRules.PeerSpeedCode(code);
        var peer = await repo.GetFirstAsync(x => x.IsDelete == null && x.Status == BaseEnums.StatusEnum.Enable
            && x.GroupCode == group && x.Code == peerCode && x.ID != currentId);
        var bound = VdsConfigRules.SpeedKmh(peer?.Value);
        if (bound is null) return;

        var invalid = VdsConfigRules.IsMinSpeedCode(code) ? mine > bound : mine < bound;
        if (invalid) throw Oops.Oh(BaseMsg.Vds.Exception.MinSpeedGreaterThanMax);
    }

    // KienLT - 31/10/2026: Manager từ chối CẢ gói QuestDB HA khi bộ hiệu lực sai ⇒ chặn ngay lúc lưu/tắt/xoá thay vì để DB chứa gói hỏng.
    /// <summary>
    /// Kiểm việc xoá một dòng: bỏ dòng <c>vds_questha_*</c> đang bật khỏi bộ hiệu lực mà bộ còn lại sai (vd xoá Critical khi Warn
    /// lớn hơn Critical mặc định) ⇒ ném <c>Oops.Oh</c>. Dòng không phải QuestDB HA ⇒ không làm gì.
    /// </summary>
    public Task EnsureQuestHaDeleteAsync(string? currentId)
        => EnsureQuestHaEffectiveAsync(group: null, code: null, value: null, BaseEnums.StatusEnum.Disable, currentId);

    /// <summary>
    /// Dựng bộ <c>vds_questha_*</c> SẼ CÓ hiệu lực sau thao tác (dòng đang bật, chưa xoá, cùng tenant, trừ dòng đang sửa; cộng giá trị
    /// mới nếu dòng sau lưu vẫn bật; mã thiếu ⇒ mặc định) rồi kiểm bằng cùng luật Manager dùng khi áp. Chỉ chạy khi dòng trước HOẶC
    /// sau thao tác là mã QuestDB HA — bộ đang hỏng sẵn trong DB không chặn việc lưu cấu hình khác.
    /// </summary>
    private async Task EnsureQuestHaEffectiveAsync(string? group, string? code, string? value, BaseEnums.StatusEnum status, string? currentId)
    {
        var after = group == VdsConfigConst.Group.Config && VdsBaseHelper.QuestHaConfigHp.IsQuestHaCode(code);
        var stored = string.IsNullOrWhiteSpace(currentId) ? null : await repo.GetFirstAsync(x => x.ID == currentId && x.IsDelete == null);
        var before = stored is not null && stored.GroupCode == VdsConfigConst.Group.Config
            && VdsBaseHelper.QuestHaConfigHp.IsQuestHaCode(stored.Code) && stored.Status == BaseEnums.StatusEnum.Enable;
        if (!after && !before) return;

        var tenant = stored?.TenantId ?? App.User?.FindFirst(ClaimConst.TenantId)?.Value;
        var rows = await repo.Context.Queryable<VdsConfig>()
            .Where(x => x.IsDelete == null && x.Status == BaseEnums.StatusEnum.Enable && x.GroupCode == VdsConfigConst.Group.Config
                && x.Code!.StartsWith(VdsConfigConst.QuestHa.Prefix) && x.ID != currentId)
            .WhereIF(!string.IsNullOrWhiteSpace(tenant), x => x.TenantId == tenant)
            .Select(x => new { x.Code, x.Value })
            .ToListAsync();
        var effective = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var row in rows.Where(r => !string.IsNullOrEmpty(r.Code))) effective[row.Code!] = row.Value;
        if (after && status == BaseEnums.StatusEnum.Enable) effective[code!] = value;

        var baseline = VdsBaseHelper.QuestHaConfigHp.SaveBaseline(questHaOptions.Value.Nodes.Count);
        if (VdsConfigRules.QuestHaEffectiveError(effective, baseline) is { } error) throw Oops.Oh(error);
    }

    /// <summary>
    /// So ngưỡng cỡ xe <c>shortMax</c> với <c>mediumMax</c> trên cùng bộ hiệu lực như ngưỡng tốc độ.
    ///
    /// <para>Giữ phép so NGHIÊM NGẶT (<c>&gt;=</c> / <c>&lt;=</c>) như trước: hai ngưỡng bằng nhau thì dải
    /// "medium" rỗng, không phân loại được xe nào.</para>
    /// </summary>
    private async Task EnsureVehicleSizePairAsync(string? group, string? code, string? value, string? currentId)
    {
        if (group != VdsConfigConst.Group.MappingVehicleSize
            || code is not (VdsConfigConst.VehicleSize.ShortMax or VdsConfigConst.VehicleSize.MediumMax)) return;

        if (!double.TryParse(value, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var mine)) return;

        var peerCode = code == VdsConfigConst.VehicleSize.ShortMax
            ? VdsConfigConst.VehicleSize.MediumMax
            : VdsConfigConst.VehicleSize.ShortMax;
        var peer = await repo.GetFirstAsync(x => x.IsDelete == null && x.Status == BaseEnums.StatusEnum.Enable
            && x.GroupCode == group && x.Code == peerCode && x.ID != currentId);
        if (!double.TryParse(peer?.Value, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var bound)) return;

        var invalid = code == VdsConfigConst.VehicleSize.ShortMax ? mine >= bound : mine <= bound;
        if (invalid) throw Oops.Oh(BaseMsg.Vds.Exception.ShortMaxGreaterThanMedium);
    }
}
