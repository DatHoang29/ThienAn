using Microsoft.Extensions.Localization;
using Modules.TMS.Core.Constants;
using Newtonsoft.Json.Linq;

namespace Modules.TMS.Report.Infrastructure.Services.Incident;

/// <summary>
/// Author: KienLT
/// Description: Triển khai IncidentReportInformation.
/// Created date: 02/10/2026
/// </summary>
public static class IncidentReportInformation
{
    public static string Build(string? metadata, string? description, IStringLocalizer localizer)
    {
        var data = IncidentMetadataConst.Read(metadata);
        var parts = new List<string>();
        foreach (var (key, unit) in new[] {
            (IncidentMetadataConst.VehiclePlate, ""), (IncidentMetadataConst.VehicleSpeed, " km/h"),
            (IncidentMetadataConst.AverageSpeed, " km/h"), (IncidentMetadataConst.VehicleLength, " m"),
            (IncidentMetadataConst.VehicleType, ""), (IncidentMetadataConst.VehicleColor, ""),
            (IncidentMetadataConst.VehicleSize, ""), (IncidentMetadataConst.Lane, ""),
            ("FromCamera", ""), ("ToCamera", ""), ("DistanceMeter", " m"),
            ("TravelSeconds", " s"), ("SpeedLimit", " km/h") })
        {
            if (data[key] is not JValue value || value.Value is null) continue;
            var text = value.ToString();
            if (string.IsNullOrWhiteSpace(text)) continue;
            parts.Add($"{localizer[Modules.TMS.Core.Exceptions.BaseMsg.TmsIncident.MetadataLabelPrefix + key].Value}: {text}{unit}");
        }
        if (!string.IsNullOrWhiteSpace(description)) parts.Add(description);
        return string.Join(Environment.NewLine, parts);
    }
}
