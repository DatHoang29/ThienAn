using System.Globalization;
using BaseMsg = Modules.VDS.Core.Exceptions.BaseMsg;
using Modules.VDS.Core.Dto;

namespace Modules.VDS.Core.Constants;

/// <summary>
/// Author: KienLT
/// Description: Mẫu tiếng Việt của các thông báo do <see cref="VdsSegmentTopologyHelper"/> sinh ra, cho worker VDS (service riêng,
/// không có localizer của WebAPI) ghi log như trước. Mẫu phải trùng từng ký tự với giá trị cùng key trong
/// <c>src/TAC_WebAPI/Resources/vi-VN.json</c> (test <c>tests/VdsRouteJourney</c> kiểm) để log worker và API tiếng Việt không lệch.
/// Created date: 22/10/2026
/// </summary>
public static class VdsSegmentTopologyViTextConst
{
    /// <summary>Key thông báo → mẫu tiếng Việt (placeholder <c>{0}</c>, <c>{1}</c>… như resource).</summary>
    public static readonly IReadOnlyDictionary<string, string> Templates = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [BaseMsg.Vds.Message.TopologyRawText] = "{0}",
        [BaseMsg.Vds.Message.TopologyConfigCodeInvalid] = "Mã {0}: không bắt đầu bằng {1} / {2} / {3}.",
        [BaseMsg.Vds.Message.TopologyKmDirInvalid] = "Chiều km {0}={1}: cần Code {{tuyến}}_{{hướng 1/2}} và Value asc/desc.",
        [BaseMsg.Vds.Message.TopologyKmDirDuplicate] = "Chiều km {0}: trùng cấu hình tuyến/hướng.",
        [BaseMsg.Vds.Message.TopologyCameraIncomplete] = "Camera {0}: thiếu km/tuyến/hướng, bỏ khỏi bộ sinh đoạn.",
        [BaseMsg.Vds.Message.TopologyPairMultiplePaths] = "Cặp {0}→{1} có nhiều đường ({2} / {3}); cần cấu hình đường đi duy nhất.",
        [BaseMsg.Vds.Message.TopologyCameraSameKm] = "Camera {0} và {1} trùng km trên {2} hướng {3}; không xác định được thứ tự.",
        [BaseMsg.Vds.Message.TopologyTurnMissingCode] = "Lượt rẽ: thiếu Code.",
        [BaseMsg.Vds.Message.TopologyTurnDuplicate] = "Lượt rẽ {0}: trùng mã.",
        [BaseMsg.Vds.Message.TopologyTurnInvalidJson] = "Lượt rẽ {0}: Value không phải JSON object hợp lệ.",
        [BaseMsg.Vds.Message.TopologyTurnInvalidFields] = "Lượt rẽ {0}: thiếu hoặc sai kiểu junction/in/inDir/out/outDir (hướng phải là số nguyên).",
        [BaseMsg.Vds.Message.TopologyTurnJunctionNotFound] = "Lượt rẽ {0}: nút {1} không tồn tại hoặc đang lỗi.",
        [BaseMsg.Vds.Message.TopologyTurnRoutesInvalid] = "Lượt rẽ {0}: tuyến vào/ra phải là hai tuyến khác nhau của nút {1}.",
        [BaseMsg.Vds.Message.TopologyTurnInboundMismatch] = "Lượt rẽ {0}: {1} hướng {2} chạy RA XA nút theo chiều km — lượt rẽ không khớp chiều km.",
        [BaseMsg.Vds.Message.TopologyTurnOutboundMismatch] = "Lượt rẽ {0}: {1} hướng {2} chạy VỀ nút theo chiều km — lượt rẽ không khớp chiều km.",
        [BaseMsg.Vds.Message.TopologyTurnNoCamera] = "Lượt rẽ {0}: không có camera trên {1} hướng {2}.",
        [BaseMsg.Vds.Message.TopologyTurnUnreadable] = "Lượt rẽ {0}: không đọc được ({1}).",
        [BaseMsg.Vds.Message.TopologySegmentCodeDuplicate] = "Mã đoạn {0} trùng cho {1} cặp camera khác nhau.",
        [BaseMsg.Vds.Message.TopologyPlausibleSpeedInvalid] = "{0}={1}: phải là số nguyên km/h từ {2} đến {3}.",
        [BaseMsg.Vds.Message.TopologyPlausibleSpeedUnrealistic] = "{0}={1} km/h: vượt {2} km/h, chỉ nên dùng khi test (lọc ghép nhầm biển gần như tắt).",
        [BaseMsg.Vds.Message.TopologyJunctionInvalidJson] = "Nút {0}: Value không phải JSON object hợp lệ.",
        [BaseMsg.Vds.Message.TopologyJunctionInvalidFields] = "Nút {0}: cần a, aPos, b, bPos và hai tuyến khác nhau.",
        [BaseMsg.Vds.Message.TopologyJunctionDuplicate] = "Nút {0}: trùng mã.",
        [BaseMsg.Vds.Message.TopologyJunctionUnreadable] = "Nút {0}: không đọc được ({1}).",
    };

    /// <summary>
    /// Author: KienLT
    /// Description: Định dạng thông báo theo mẫu tiếng Việt, dịch cả thông báo lồng trong tham số. Key không có trong bảng
    /// ⇒ trả key kèm tham số để log vẫn đủ thông tin.
    /// Created date: 22/10/2026
    /// </summary>
    public static string Format(VdsSegmentTopologyMessageOutput message)
    {
        var arguments = message.Arguments
            .Select(argument => argument is VdsSegmentTopologyMessageOutput nested ? Format(nested) : argument).ToArray();
        return Templates.TryGetValue(message.Key, out var template)
            ? string.Format(CultureInfo.InvariantCulture, template, arguments)
            : arguments.Length == 0 ? message.Key : $"{message.Key} ({string.Join(", ", arguments)})";
    }
}
