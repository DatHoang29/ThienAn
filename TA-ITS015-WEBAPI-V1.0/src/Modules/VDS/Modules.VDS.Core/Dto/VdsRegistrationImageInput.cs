namespace Modules.VDS.Core.Dto;

/// <summary>
/// Author: KienLT
/// Description: Yêu cầu nhập ảnh đăng ký VDS cho một hồ sơ sự cố (trạng thái WaitingForSource → Ready/MissingSource).
/// Created date: 02/10/2026
/// </summary>
public sealed record VdsRegistrationImageInput(string TenantId, string IncidentId, DateTime RegistrationTime,
    DateTime WaitUntilUtc, VdsRegistrationImageSourceOutput Source)
{
    public string Status { get; init; } = "WaitingForSource";
    public string? FileName { get; init; }
    public string? ContentType { get; init; }
    public string? Error { get; init; }
}
