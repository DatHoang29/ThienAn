namespace Tests;

/// <summary>
/// Description: Phần mở rộng Test Host cho phân hệ ShareData — sở hữu vòng đời mock server đối tác
///              (HttpListener thật trên 127.0.0.1, cổng 18090-18093). Tệp nằm trong tests/ShareData/
///              nên khi module ShareData không còn trong nhánh, tệp bị loại khỏi biên dịch và phần thân
///              partial biến mất cùng lời gọi bên Host.cs.
/// Created date: 30/09/2026
/// </summary>
public partial class Host
{
    /// <summary>
    /// Description: Mock server đối tác dùng chung cho toàn bộ test ShareData trong Collection "api".
    /// Created date: 30/09/2026
    /// </summary>
    public ShareDataPartnerServerMock PartnerServer { get; } = new();

    partial void StartShareDataTestServers() => PartnerServer.Start(ShareDataPartnerServerMock.DefaultPorts);

    partial void StopShareDataTestServers() => PartnerServer.Dispose();
}
