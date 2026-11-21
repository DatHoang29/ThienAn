namespace Modules.VDS.Core.Dto;

/// <summary>
/// Author: KienLT
/// Description: Raw detection identity, independent of the incident's viewable CCTV cameras.
/// Created date: 02/10/2026
/// </summary>
public sealed record VdsRegistrationImageSourceOutput(long EventId, DateTime DetectTimeUtc, string EquipmentId,
    string? CameraIp, string? EventType);
