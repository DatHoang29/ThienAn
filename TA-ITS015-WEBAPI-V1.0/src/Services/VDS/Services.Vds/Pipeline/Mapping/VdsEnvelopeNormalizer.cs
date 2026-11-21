using Newtonsoft.Json.Linq;
using Services.Vds.Shared.Domain;
using Services.Vds.Pipeline.Contracts;

namespace Services.Vds.Pipeline.Mapping;

/// <summary>
/// Author: KienLT
/// Description: Chuẩn hoá khác biệt payload VDS v1.0/v1.3 trước khi ánh xạ entity.
/// Created date: 25/08/2026
/// </summary>
public static class VdsEnvelopeNormalizer
{
    private const string LaneNoField = "lane_no.";
    private const string DetectionRegionField = "detection_region";
    private const string VendorEventTypeField = "vendor_event_type";
    private const string DataListField = "data_list";

    public const string DefaultLane = "1";

    /// <summary>Trả true khi tên event legacy đã được đổi sang tên chuẩn.</summary>
    public static bool Normalize(VdsSseEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var convertedLegacyType = NormalizeType(envelope);
        NormalizeLane(envelope);
        return convertedLegacyType;
    }

    private static bool NormalizeType(VdsSseEnvelope envelope)
    {
        var original = envelope.Type?.Trim();
        if (string.IsNullOrEmpty(original)) return false;

        var canonical = VdsEventTypes.Canonical(original);
        envelope.Type = canonical;

        if (string.Equals(original, canonical, StringComparison.Ordinal)) return false;

        if (envelope.Metadata is JObject metadata && IsBlank(metadata[VendorEventTypeField]))
            metadata[VendorEventTypeField] = original;

        return true;
    }

    private static void NormalizeLane(VdsSseEnvelope envelope)
    {
        if (envelope.Metadata is not JObject metadata) return;

        if (string.Equals(envelope.Type, VdsEventTypes.Statistic, StringComparison.OrdinalIgnoreCase))
        {
            if (metadata[DataListField] is JArray list)
            {
                foreach (var item in list)
                    if (item is JObject lane) FillLane(lane);
            }

            return;
        }

        FillLane(metadata);
    }

    private static void FillLane(JObject holder)
    {
        var lane = FirstNonBlank(holder[LaneNoField], holder[DetectionRegionField]) ?? DefaultLane;
        holder[LaneNoField] = lane;
        holder[DetectionRegionField] = lane;
    }

    private static string? FirstNonBlank(params JToken?[] tokens)
    {
        foreach (var token in tokens)
        {
            if (IsBlank(token)) continue;

            var value = token!.ToString().Trim();
            if (value.Length > 0 && value != "-") return value;
        }

        return null;
    }

    private static bool IsBlank(JToken? token)
    {
        if (token is null || token.Type is JTokenType.Null or JTokenType.Undefined) return true;

        var value = token.ToString();
        return string.IsNullOrWhiteSpace(value) || value.Trim() == "-";
    }
}
