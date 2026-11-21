namespace Module.VideoWall.WPF.Api.Direct;

/// <summary>
/// Description: Thông tin chứng thực và địa chỉ mạng để kết nối trực tiếp thiết bị VideoWall
/// Created date: 13/09/2026
/// </summary>
public record VwDirectDeviceCredentials(string Ip, int Port, string Account, string Password);
