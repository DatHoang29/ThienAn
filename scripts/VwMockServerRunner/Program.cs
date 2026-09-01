using System;
using System.Linq;
using System.Threading;
using Tests.Modules.VideoWall.MockServer;

using var mock = new VwISAPIMockServerHikvision();

// --- Cờ điều khiển từ CLI / ENV (M9) ---
bool Flag(string name) =>
    args.Contains("--" + name, StringComparer.OrdinalIgnoreCase)
    || string.Equals(Environment.GetEnvironmentVariable("VWMOCK_" + name.Replace('-', '_').ToUpperInvariant()), "1", StringComparison.OrdinalIgnoreCase);

if (Flag("closeall-ok"))            mock.ScreenCtrlCloseAllThrowsInvalidOperation = false;
if (Flag("nonce-expiry"))          mock.SimulateNonceExpiry = true;
if (Flag("savedata-fail"))         mock.SimulateSaveDataFailure = true;
if (Flag("no-bound-wall"))         mock.SimulateNoBoundWall = true;
if (Flag("multi-bound-wall"))      mock.SimulateMultipleBoundWalls = true;
if (Flag("unreachable"))           mock.SimulateUnreachable = true;
if (Flag("verify-digest"))         mock.VerifyDigestResponseHash = true;

foreach (var a in args)
{
    if (a.StartsWith("--lockout=", StringComparison.OrdinalIgnoreCase)
        && int.TryParse(a["--lockout=".Length..], out var th))
        mock.FailedAuthLockoutThreshold = th;
    if (a.StartsWith("--max-scene=", StringComparison.OrdinalIgnoreCase)
        && int.TryParse(a["--max-scene=".Length..], out var ms))
        mock.MaxSceneNums = ms;
    if (a.StartsWith("--not-connected=", StringComparison.OrdinalIgnoreCase))
        foreach (var idStr in a["--not-connected=".Length..].Split(',', StringSplitOptions.RemoveEmptyEntries))
            if (int.TryParse(idStr, out var oid))
                mock.NotConnectedOutputChannels.Add(oid);
}

mock.Start();

Console.WriteLine($"[VwMockServerRunner] Giả lập thiết bị Hikvision DS-C66S-H88-CL tại {mock.BaseUrl}");
Console.WriteLine($"[VwMockServerRunner] Port: {string.Join(", ", VwISAPIMockServerHikvision.DefaultPorts)}");
Console.WriteLine($"[VwMockServerRunner] Account: {VwISAPIMockServerHikvision.DefaultUser} | Password: {VwISAPIMockServerHikvision.DefaultPassword}");
Console.WriteLine($"[VwMockServerRunner] closeAll={(!mock.ScreenCtrlCloseAllThrowsInvalidOperation ? "OK" : "invalidOperation")} | nonceExpiry={mock.SimulateNonceExpiry} | lockout={mock.FailedAuthLockoutThreshold} | maxScene={mock.MaxSceneNums}");
Console.WriteLine("[VwMockServerRunner] Đang chạy — nhấn Ctrl+C hoặc dừng debugger để thoát.");

var exitEvent = new ManualResetEventSlim(false);
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    exitEvent.Set();
};

exitEvent.Wait();
Console.WriteLine("[VwMockServerRunner] Đang dừng...");
