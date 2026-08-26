using Tests.Modules.VideoWall.MockServer;

using var mock = new VwISAPIMockServerHikvision();
mock.Start();

Console.WriteLine($"[VwMockServerRunner] Giả lập thiết bị Hikvision DS-C66S-H88-CL tại {mock.BaseUrl}");
Console.WriteLine($"[VwMockServerRunner] Port: {string.Join(", ", VwISAPIMockServerHikvision.DefaultPorts)}");
Console.WriteLine($"[VwMockServerRunner] Account: {VwISAPIMockServerHikvision.DefaultUser} | Password: {VwISAPIMockServerHikvision.DefaultPassword}");
Console.WriteLine("[VwMockServerRunner] Đang chạy — nhấn Ctrl+C hoặc dừng debugger để thoát.");

var exitEvent = new ManualResetEventSlim(false);
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    exitEvent.Set();
};

exitEvent.Wait();
Console.WriteLine("[VwMockServerRunner] Đang dừng...");
