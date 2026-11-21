using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Modules.TMS.Core.Helper;

/// <summary>
/// Author: KienLT
/// Description: Same catalog values and naming format as manual registration.
/// Created date: 02/10/2026
/// </summary>
public static class IncidentNamingHelper
{
    public static string Disambiguate(string code, string identity)
    {
        var suffix = "_" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)))[..8];
        return code[..Math.Min(code.Length, 64 - suffix.Length)] + suffix;
    }

    public static string Code(string? eventTypeCode, string? location, string? direction,
        int? km, int? met, DateTime startDate)
    {
        var parts = new List<string> { "ICC" };
        foreach (var value in new[] { eventTypeCode, location, direction })
            if (!string.IsNullOrWhiteSpace(value)) parts.Add(value.Trim());
        parts.Add((km ?? 0).ToString(CultureInfo.InvariantCulture));
        parts.Add((met ?? 0).ToString(CultureInfo.InvariantCulture));
        parts.Add(startDate.ToString("ddMMyyyyHHmmss", CultureInfo.InvariantCulture));
        return string.Join('_', parts);
    }

    public static string Name(string? eventTypeName, string? locationName, string? directionName,
        int? km, int? met, DateTime startDate)
    {
        var parts = new List<string>();
        foreach (var value in new[] { eventTypeName, locationName, directionName })
            if (!string.IsNullOrWhiteSpace(value)) parts.Add(value.Trim());
        var suffix = $"Km{km ?? 0}+{met ?? 0} " + startDate.ToString("dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture);
        var heading = string.Join(' ', parts);
        // The manual form and database both limit Name to 64 characters. Preserve position and time.
        var available = Math.Max(0, 64 - suffix.Length - 1);
        if (heading.Length > available) heading = heading[..available].TrimEnd();
        return string.IsNullOrEmpty(heading) ? suffix : heading + " " + suffix;
    }
}
