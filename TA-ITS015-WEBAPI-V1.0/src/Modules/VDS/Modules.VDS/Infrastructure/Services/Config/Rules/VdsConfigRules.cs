using Modules.TMS.Core.Constants;
using Modules.VDS.Core.Dto;
using System.Globalization;
using System.Text.RegularExpressions;
using Modules.VDS.Core.Exceptions;
using Modules.VDS.Core.Constants;
using Shared.Utility.Helpers;
using Modules.VDS.Core.Helper;
using Shared.QuestHa.Options;

namespace Modules.VDS.Infrastructure.Services.Config.Rules;

/// <summary>
/// Author: KienLT
/// Description: Luật NGHIỆP VỤ của một dòng cấu hình VDS, dùng chung cho mọi đường ghi.
///
/// <para><b><see cref="Validate"/> trả về KHÓA LOCALIZATION, không phải câu tiếng Việt.</b> Người gọi ném
/// bằng <c>Oops.Oh(key)</c> nên câu hiển thị đi qua localizer và đổi được theo ngôn ngữ. Trước 21/09/2026
/// hàm này trả 14 câu tiếng Việt cứng, nên client <c>en-US</c> vẫn nhận tiếng Việt.</para>
///
/// <para>Phần kiểm tra CẤU TRÚC (bắt buộc, độ dài, dải trạng thái) cũng còn ở đây dù validator
/// FluentValidation đã phủ: validator chỉ tự chạy cho tham số action, còn handler còn được gọi qua message
/// bus. Hai lớp là cố ý.</para>
///
/// <para><b>Vị trí (đổi 23/09/2026).</b> Trước ở <c>Modules.VDS.Core/Configuration</c> rồi
/// <c>Core/Domain</c>. Đây là luật của MỘT module, chỉ <c>Modules.VDS</c> gọi, nên thuộc
/// <c>Infrastructure/Services/Config</c> cạnh <c>VdsConfigWriteService</c> — nơi dùng nó nhiều nhất và là
/// đường kiểm tra duy nhất của mọi lần ghi bảng <c>VdsConfig</c>. Phép đổi chuỗi thời lượng thành giây đã
/// tách sang <c>Shared.Utility/Helpers/DurationHelper</c> vì nó không có gì của VDS và <c>Modules.TMS</c>
/// cũng cần.</para>
/// Created date: 16/09/2026
/// </summary>
public static class VdsConfigRules
{
    /// <summary>Biên thời gian hiển thị sự kiện trên SSE (giây): ngắn hơn chu kỳ poll thì nhấp nháy, dài quá thì ứ màn hình.</summary>
    public const int MinEventDisplaySeconds = 5;
    public const int MaxEventDisplaySeconds = 3600;

    /// <summary>Biên chốt sự kiện chậm/nghẽn của trạng thái vùng (giây).</summary>
    public const int MinZoneEventTtlSeconds = 5;
    public const int MaxZoneEventTtlSeconds = 3600;
    /// <summary>Biên khoảng nghỉ gom chuyến lọc xe (<c>vds_vehicle_filter_trip_gap</c>): 1 phút..24 giờ.</summary>
    public const int MinVehicleFilterTripGapSeconds = 60;
    public const int MaxVehicleFilterTripGapSeconds = 86400;

    /// <summary>Độ dài tối đa của mã nhóm cấu hình.</summary>
    private const int MaxGroupLength = 64;

    /// <summary>Độ dài tối đa của mã cấu hình.</summary>
    private const int MaxCodeLength = 128;

    /// <summary>Độ dài tối đa của tên và giá trị cấu hình.</summary>
    private const int MaxTextLength = 256;

    /// <summary>
    /// Ngưỡng tốc độ tối thiểu được phép khai (km/h).
    ///
    /// <para><b>Là 1, không phải 0.</b> Worker coi cặp ngưỡng chỉ hợp lệ khi <c>Min &gt; 0 &amp;&amp; Max &gt; 0</c>
    /// (<c>Services.Vds/Shared/Domain/SpeedRange.cs</c>), và khi không hợp lệ thì nó BỎ CẢ HƯỚNG kèm một
    /// dòng Warning. Cho lưu 0 ở đây nghĩa là API báo thành công trong khi hướng đó mất ngưỡng hoàn toàn.</para>
    /// </summary>
    public const int MinSpeedKmh = 1;

    /// <summary>
    /// Ngưỡng tốc độ tối đa được phép khai (km/h).
    ///
    /// <para><b>Số 300 là THỪA HƯỞNG, không phải do tầng nào đặc tả.</b> Nó vốn là literal nằm thẳng trong
    /// <c>ConfigurationCommandHandler</c>; không có tài liệu nào giải thích, và worker thì không chặn trên.
    /// Giữ lại vì đó là trần duy nhất đang tồn tại và bỏ đi là nới lỏng một lớp bảo vệ; đặt thành hằng ở đây
    /// để đổi một chỗ khi có đặc tả thật từ AID.</para>
    /// </summary>
    public const int MaxSpeedKmh = 300;

    public static bool IsSecret(string? code) => Regex.IsMatch(code ?? "", "password|token|secret|api_key", RegexOptions.IgnoreCase);

    /// <summary>Mã này có phải một ngưỡng tốc độ theo tuyến/hướng hay không.</summary>
    public static bool IsRouteSpeedCode(string? code)
        => code is not null
           && code.StartsWith(VdsConfigConst.Pattern.RoutePrefix, StringComparison.Ordinal)
           && code.EndsWith(VdsConfigConst.Pattern.SpeedSuffix, StringComparison.Ordinal);

    /// <summary>
    /// Ngưỡng tốc độ đã quy về đúng kiểu worker dùng, hoặc <c>null</c> nếu không dùng được.
    ///
    /// <para><b>Phải là SỐ NGUYÊN.</b> Worker parse bằng <c>int.TryParse(..., InvariantCulture)</c> rồi bỏ
    /// dòng nào không phải integer. Trước 22/09/2026 luật ghi ở đây nhận <c>double</c> nên <c>"50.5"</c> lưu
    /// được mà worker im lặng loại — API báo thành công, ngưỡng không bao giờ có hiệu lực.</para>
    /// </summary>
    public static int? SpeedKmh(string? raw)
        => int.TryParse((raw ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
           && value is >= MinSpeedKmh and <= MaxSpeedKmh
            ? value
            : null;

    /// <summary>Mã ngưỡng còn lại của cùng một hướng: min ⇄ max. <c>null</c> nếu mã không phải ngưỡng tốc độ.</summary>
    public static string? PeerSpeedCode(string? code)
    {
        if (code is null) return null;
        if (code.EndsWith(VdsConfigConst.Pattern.MinSpeedSuffix, StringComparison.Ordinal))
            return code[..^VdsConfigConst.Pattern.MinSpeedSuffix.Length] + VdsConfigConst.Pattern.MaxSpeedSuffix;
        if (code.EndsWith(VdsConfigConst.Pattern.MaxSpeedSuffix, StringComparison.Ordinal))
            return code[..^VdsConfigConst.Pattern.MaxSpeedSuffix.Length] + VdsConfigConst.Pattern.MinSpeedSuffix;
        return null;
    }

    /// <summary>Mã này là ngưỡng TỐI THIỂU (true) hay TỐI ĐA (false) của một hướng.</summary>
    public static bool IsMinSpeedCode(string? code)
        => code is not null && code.EndsWith(VdsConfigConst.Pattern.MinSpeedSuffix, StringComparison.Ordinal);

    /// <summary>
    /// Thư mục kho media có dùng được hay không: phải là đường dẫn tuyệt đối và không chứa ký tự cấm.
    ///
    /// <para>Luật này trước đây chỉ có ở đường ghi <c>Configuration</c>; dời vào đây để cả hai đường ghi cùng
    /// chặn. Đường dẫn UNC (<c>\\server\share</c>) được <c>IsPathFullyQualified</c> chấp nhận — giá trị đang
    /// chạy trên DEV chính là UNC.</para>
    /// </summary>
    public static bool IsUsableStorageDirectory(string? value)
    {
        var path = (value ?? "").Trim();
        return path.Length > 0 && Path.IsPathFullyQualified(path) && path.IndexOfAny(Path.GetInvalidPathChars()) < 0;
    }

    // KienLT - 31/10/2026: luật lưu mã vds_questha_*.
    /// <summary>
    /// Lỗi của một dòng <c>vds_questha_*</c> (khoá localization) hoặc <c>null</c> nếu hợp lệ/không phải mã QuestDB HA: sai nhóm, mã lạ,
    /// thời lượng thiếu hậu tố s/m/h/d, sai định dạng hoặc ngoài biên của <see cref="QuestHaRuntimeOptions.Validate"/>.
    /// </summary>
    public static string? QuestHaValueError(string? group, string? code, string? value)
    {
        if (!VdsBaseHelper.QuestHaConfigHp.IsQuestHaCode(code)) return null;
        if (group != VdsConfigConst.Group.Config) return BaseMsg.Vds.Exception.QuestHaGroupInvalid;
        if (!VdsBaseHelper.QuestHaConfigHp.IsKnownCode(code)) return BaseMsg.Vds.Exception.QuestHaCodeUnknown;
        if (VdsBaseHelper.QuestHaConfigHp.IsDurationCode(code) && !VdsBaseHelper.QuestHaConfigHp.HasDurationSuffix(value))
            return BaseMsg.Vds.Exception.DurationSuffixRequired;
        var runtime = new QuestHaRuntimeOptions();
        return VdsBaseHelper.QuestHaConfigHp.TryRead(runtime, code!, value) is not null || runtime.Validate().Count > 0
            ? BaseMsg.Vds.Exception.QuestHaValueInvalid
            : null;
    }

    /// <summary>
    /// So một mã <c>vds_questha_*</c> với mã cùng cặp đang có hiệu lực (<paramref name="peerValue"/> null = chưa khai ⇒ không so):
    /// Warn phải nhỏ hơn Critical; đầu và cuối cửa sổ backup không được trùng. Trả khoá localization hoặc <c>null</c>.
    /// </summary>
    public static string? QuestHaPairError(string? code, string? value, string? peerValue)
    {
        var peer = VdsBaseHelper.QuestHaConfigHp.PeerCode(code);
        if (peer is null || peerValue is null) return null;
        var runtime = new QuestHaRuntimeOptions();
        // Giá trị của dòng kia đọc không được thì không có gì để so; dòng đó đã bị chặn khi lưu.
        if (VdsBaseHelper.QuestHaConfigHp.TryRead(runtime, code!, value) is not null
            || VdsBaseHelper.QuestHaConfigHp.TryRead(runtime, peer, peerValue) is not null
            || runtime.Validate().Count == 0) return null;
        return code is VdsConfigConst.QuestHa.BackupWindowStart or VdsConfigConst.QuestHa.BackupWindowEnd
            ? BaseMsg.Vds.Exception.QuestHaBackupWindowInvalid
            : BaseMsg.Vds.Exception.QuestHaWarnNotBelowCritical;
    }

    // KienLT - 31/10/2026: kiểm cả BỘ sẽ có hiệu lực sau khi lưu, đúng luật QuestHaManager dùng lúc áp.
    /// <summary>
    /// Lỗi của bộ <c>vds_questha_*</c> SẼ CÓ hiệu lực sau khi lưu (<paramref name="effective"/>: dòng đang bật của tenant + giá trị mới,
    /// mã thiếu ⇒ giá trị của <paramref name="baseline"/>) hoặc <c>null</c> nếu Manager sẽ nhận. Luật lấy từ
    /// <see cref="QuestHaRuntimeOptions.ValidateEffective"/> (cùng hàm Manager gọi khi áp); ở đây chỉ chọn khoá thông báo:
    /// cặp Warn ≥ Critical, cửa sổ trùng, slot lịch backup ngoài cửa sổ/qua ngày, còn lại là giá trị sai.
    /// </summary>
    public static string? QuestHaEffectiveError(IReadOnlyDictionary<string, string?> effective, QuestHaOptions baseline)
    {
        var errors = new List<string>();
        var runtime = VdsBaseHelper.QuestHaConfigHp.Build(effective, errors);
        if (errors.Count == 0 && runtime.ValidateEffective(baseline).Count == 0) return null;

        // Bộ đủ mọi thuộc tính (mã thiếu lấy baseline) để biết cặp nào hỏng, kể cả khi chỉ khai một nửa cặp.
        var full = runtime.Effective(baseline);
        foreach (var (first, second) in VdsBaseHelper.QuestHaConfigHp.PairCodes)
        {
            if (!VdsBaseHelper.QuestHaConfigHp.PairViolated(full, first, second)) continue;
            return first == VdsConfigConst.QuestHa.BackupWindowStart
                ? BaseMsg.Vds.Exception.QuestHaBackupWindowInvalid
                : BaseMsg.Vds.Exception.QuestHaWarnNotBelowCritical;
        }
        // Mọi giá trị và mọi cặp đều đúng mà bộ vẫn bị từ chối ⇒ chỉ còn luật slot lịch backup của từng node.
        return errors.Count == 0 && full.Validate().Count == 0
            ? BaseMsg.Vds.Exception.QuestHaBackupSlotOutsideWindow
            : BaseMsg.Vds.Exception.QuestHaValueInvalid;
    }

    /// <summary>Khóa localization của lỗi đầu tiên tìm thấy, hoặc <c>null</c> nếu hợp lệ.</summary>
    public static string? Validate(string? group, string? code, string? value, string? name, int status)
    {
        if (group == VdsConfigConst.Group.MappingEventType
            && (string.Equals(value?.Trim(), IncidentEventTypeConst.Unknown, StringComparison.OrdinalIgnoreCase)
                || new[] { "Normal", "Regular", "Statistic" }.Contains(code?.Trim(), StringComparer.OrdinalIgnoreCase)))
            return BaseMsg.Vds.Exception.ParameterNotSupported;
        if (string.IsNullOrWhiteSpace(group) || !group.StartsWith("vds_", StringComparison.Ordinal) || group.Length > MaxGroupLength)
            return BaseMsg.Vds.Exception.GroupPrefixInvalid;
        if (string.IsNullOrWhiteSpace(code) || code.Length > MaxCodeLength) return BaseMsg.Vds.Exception.CodeRequired;
        if (string.IsNullOrWhiteSpace(name) || name.Length > MaxTextLength) return BaseMsg.Vds.Exception.NameRequired;
        if (value?.Length > MaxTextLength) return BaseMsg.Vds.Exception.ValueTooLong;
        if (status is not (0 or 1)) return BaseMsg.Vds.Exception.StatusInvalid;

        if (group == VdsConfigConst.Group.ImageReuse && VdsImageReusePolicyOutput.ParseSeconds(value) is null)
            return BaseMsg.Vds.Exception.DurationOutOfRange;

        if (group is VdsConfigConst.Group.EventSkip or VdsConfigConst.Group.EventDedup or VdsConfigConst.Group.ConfigDefault
            or VdsConfigConst.Group.EventDisplay)
        {
            // BẮT BUỘC hậu tố s/m/h/d, dù DurationHelper vẫn hiểu số trần là phút: gõ "30" khi có ý "30 giây"
            // là cái bẫy sai gấp 60 lần, chặn ngay tại cửa nhập còn hơn để nó thành cấu hình chạy thật.
            // Luật này trước đây chỉ có ở API Catalog (đã bỏ 21/09/2026); dời vào đây để đường CRUD duy nhất
            // còn lại không lỏng hơn đường cũ. Toàn bộ 24 dòng đang có trong DB đều đã có hậu tố.
            if (!Regex.IsMatch(value?.Trim() ?? "", @"^\d+(s|m|h|d)$")) return BaseMsg.Vds.Exception.DurationSuffixRequired;
            if (BaseHelper.DurationHp.TryParseSeconds(value) is null) return BaseMsg.Vds.Exception.DurationOutOfRange;
        }

        if ((group == VdsConfigConst.Group.EventDisplay || code == VdsConfigConst.Code.EventDisplayDefault)
            && BaseHelper.DurationHp.TryParseSeconds(value) is not (>= MinEventDisplaySeconds and <= MaxEventDisplaySeconds))
            return BaseMsg.Vds.Exception.DurationOutOfRange;
        if (code == VdsConfigConst.Code.ZoneStaleSeconds && (group != VdsConfigConst.Group.ConfigDefault || BaseHelper.DurationHp.TryParseSeconds(value) is not (>= 1 and <= 86400)))
            return BaseMsg.Vds.Exception.ZoneStaleRangeInvalid;
        // Khoảng nghỉ gom chuyến lọc xe: bắt buộc hậu tố (gõ "10" dễ hiểu nhầm phút/giây) và 1 phút..24 giờ.
        if (code == VdsConfigConst.Code.VehicleFilterTripGap)
        {
            if (group != VdsConfigConst.Group.ConfigDefault || !Regex.IsMatch(value?.Trim() ?? "", @"^\d+(s|m|h|d)$"))
                return BaseMsg.Vds.Exception.DurationSuffixRequired;
            if (BaseHelper.DurationHp.TryParseSeconds(value) is not (>= MinVehicleFilterTripGapSeconds and <= MaxVehicleFilterTripGapSeconds))
                return BaseMsg.Vds.Exception.DurationOutOfRange;
        }
        // Chốt sự kiện chậm/nghẽn của trạng thái vùng: worker ghi giữ nhịp suốt thời gian chốt, nên gõ "60" (hiểu là
        // 60 PHÚT) sẽ treo vùng ở chậm/nghẽn cả giờ trên bigscreen ⇒ bắt buộc hậu tố và giới hạn 5s..1h.
        if (code is VdsConfigConst.Code.ZoneSlowEventTtl or VdsConfigConst.Code.ZoneCongestionEventTtl)
        {
            if (!Regex.IsMatch(value?.Trim() ?? "", @"^\d+(s|m|h|d)$")) return BaseMsg.Vds.Exception.DurationSuffixRequired;
            if (BaseHelper.DurationHp.TryParseSeconds(value) is not (>= MinZoneEventTtlSeconds and <= MaxZoneEventTtlSeconds))
                return BaseMsg.Vds.Exception.DurationOutOfRange;
        }
        if ((code is VdsConfigConst.Code.JourneyMinTravelDefault or VdsConfigConst.Code.JourneyMaxTravelDefault)
            && (group != VdsConfigConst.Group.ConfigDefault || VdsJourneyTravelConst.ParseSeconds(value) is null))
            return BaseMsg.Vds.Exception.DurationOutOfRange;
        // khoảng nghỉ gom chuyến (lưới Tốc độ trung bình) — 1 phút..24 giờ, chỉ nhóm mặc định.
        if (code == VdsConfigConst.Code.JourneyTripGap && (group != VdsConfigConst.Group.ConfigDefault
            || BaseHelper.DurationHp.TryParseSeconds(value) is not (>= Journey.Rules.VdsJourneyTripRules.MinGapSeconds and <= Journey.Rules.VdsJourneyTripRules.MaxGapSeconds)))
            return BaseMsg.Vds.Exception.DurationOutOfRange;
        if (code == VdsConfigConst.Code.StatisticPeriodMinutes && (!int.TryParse(value, out var period) || period is < 1 or > 60))
            return BaseMsg.Vds.Exception.StatisticPeriodInvalid;
        if (IsRouteSpeedCode(code) && SpeedKmh(value) is null)
            return BaseMsg.Vds.Exception.SpeedThresholdInvalid;
        if (code == VdsConfigConst.Code.MediaStorageDirectory && !IsUsableStorageDirectory(value))
            return BaseMsg.Vds.Exception.MediaDirectoryInvalid;
        // KienLT - 31/10/2026: tham số vận hành QuestDB HA — chỉ nhóm vds_config, thời lượng bắt buộc hậu tố, biên lấy từ thư viện QuestHa
        // (cùng luật Worker áp lúc chạy) để không lưu được giá trị mà Worker sẽ từ chối.
        if (QuestHaValueError(group, code, value) is { } questHaError) return questHaError;

        // Các nhóm này CỐ Ý cho giá trị rỗng: vds_event_check_plate và vds_list_equipment
        // dùng chính Code làm dữ liệu (mã loại sự kiện / ID camera), Value không mang nghĩa.
        var optionalStorageValue = group == VdsConfigConst.Group.Config
            && (code == VdsConfigConst.Code.MediaStorageDomain
                || status == 0 && code is VdsConfigConst.Code.MediaStorageUsername or VdsConfigConst.Code.MediaStoragePassword);
        if (!optionalStorageValue && group is not (VdsConfigConst.Group.EventCheckPlate or VdsConfigConst.Group.ListEquipment) && string.IsNullOrWhiteSpace(value))
            return BaseMsg.Vds.Exception.ValueRequired;

        if (group == VdsConfigConst.Group.MappingVehicleSize)
        {
            if (code is not ("mode" or "unit" or "shortMax" or "mediumMax")) return BaseMsg.Vds.Exception.VehicleSizeKeyInvalid;
            if (code == "mode" && value is not ("length" or "random")) return BaseMsg.Vds.Exception.VehicleSizeModeInvalid;
            if (code == "unit" && value is not ("mm" or "millimeter" or "cm" or "centimeter" or "m" or "meter"))
                return BaseMsg.Vds.Exception.VehicleSizeUnitInvalid;
            if (code is "shortMax" or "mediumMax")
                if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var size) || !double.IsFinite(size) || size <= 0)
                    return BaseMsg.Vds.Exception.VehicleSizeThresholdInvalid;
        }

        return null;
    }
}
