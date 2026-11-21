using Module.ShareData.Core.Entities;
using Module.ShareData.Core.Helpers;
using ShareDataWorker.Core.Constants;
using ShareDataWorker.Core.Enums;
using Shared.DTO.Enums;

namespace ShareDataWorker.Core.Helpers;

/// <summary>
/// Description: Bộ hỗ trợ metadata, chính sách gửi dữ liệu suy từ câu lệnh SQL và các quy tắc nghiệp vụ định danh gói tin ShareData.
/// Created date: 16/09/2026
/// Modified date: 22/10/2026
/// </summary>
public static class PacketHelper
{
    /// <summary>
    /// Description: Chuẩn hoá mã gói tin (chuỗi số thuần hoặc tiền tố số) về mã chuẩn duy nhất (Canonical Packet Code) trong PacketCodeConst.
    ///              Ví dụ: "101" hoặc "101_commonData" -> "101_commonData"; "111" -> "111".
    ///              Nếu không phân giải được thì trả về chuỗi gốc (đã trim) hoặc chuỗi rỗng.
    /// Created date: 27/09/2026
    /// </summary>
    public static string NormalizePacketCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return string.Empty;

        if (!TryGetPacketNumber(code, out var num))
            return code.Trim();

        return num switch
        {
            101 => PacketCodeConst.CommonData,
            102 => PacketCodeConst.CctvData,
            103 => PacketCodeConst.VdsData,
            104 => PacketCodeConst.WeatherData,
            105 => PacketCodeConst.RfidData,
            106 => PacketCodeConst.WimData,
            107 => PacketCodeConst.IncidentData,
            108 => PacketCodeConst.VmsInfo,
            109 => PacketCodeConst.EtcData,
            110 => PacketCodeConst.WpData,
            111 => PacketCodeConst.InterCenter,
            _ => code.Trim()
        };
    }

    /// <summary>
    /// Description: Phân giải mã gói tin (chuỗi số thuần hoặc tiền tố số) về số hiệu gói chuẩn (101-111).
    /// Created date: 23/09/2026
    /// Modified date: 24/09/2026
    /// </summary>
    public static bool TryGetPacketNumber(string? packetCode, out int packetNumber)
    {
        packetNumber = 0;
        if (string.IsNullOrWhiteSpace(packetCode))
            return false;

        var key = packetCode.Trim();

        // 1. Khớp số hiệu thuần trong khoảng chuẩn 101-111
        if (int.TryParse(key, out var num) && num >= 101 && num <= 111)
        {
            packetNumber = num;
            return true;
        }

        // 2. Fallback bóc tiền tố số ("103_vdsData" -> 103)
        var digits = new string(key.TakeWhile(char.IsDigit).ToArray());
        if (int.TryParse(digits, out num) && num >= 101 && num <= 111)
        {
            packetNumber = num;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Description: Phân giải chính sách xuất bản theo câu lệnh SQL trích xuất (có tham chiếu biến mốc @lastKey là AlwaysIncremental, ngược lại là Snapshot).
    /// Created date: 22/10/2026
    /// </summary>
    public static ShareDataEnum.PacketPolicy GetPolicyFromSql(string? sql, IReadOnlyCollection<string>? cursorVariableNames = null)
    {
        return SqlValidatorHelper.IsIncrementalSql(sql, cursorVariableNames)
            ? ShareDataEnum.PacketPolicy.AlwaysIncremental
            : ShareDataEnum.PacketPolicy.Snapshot;
    }

    /// <summary>
    /// Description: Cố gắng phân giải enum DatatypeIdEnum từ chuỗi datatypeId.
    /// </summary>
    public static bool TryGetDatatypeEnum(string? datatypeId, out ShareDataEnum.DatatypeIdEnum datatypeEnum)
    {
        return Enum.TryParse(datatypeId, true, out datatypeEnum);
    }
}
