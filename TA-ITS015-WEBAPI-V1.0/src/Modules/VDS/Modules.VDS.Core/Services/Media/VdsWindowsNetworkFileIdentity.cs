using Modules.VDS.Core.Interfaces;
using Modules.VDS.Core.Options;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;
using Modules.VDS.Core.Entities;

namespace Modules.VDS.Core.Services.Media;

/// <summary>
/// Author: KienLT
/// Description: Explicit SMB credentials inside a private Windows logon session, never the caller's shared session.
/// Created date: 02/10/2026
/// </summary>
public sealed class VdsWindowsNetworkFileIdentity : IVdsNetworkFileIdentity
{
    public async Task<T> RunAsync<T>(string configuredRoot, VdsImageShareOptions settings, Func<Task<T>> operation, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Configured UNC image-share credentials require a Windows host.");
        var (user, domain) = Account(settings);
        if (!LogonUser(user, domain, settings.Password ?? "", 9, 3, out var token))
        {
            var error = Marshal.GetLastWin32Error();
            token?.Dispose();
            throw new IOException("Cannot authenticate the VdsConfig media storage account; Windows error " + error + ".",
                new Win32Exception(error));
        }
        using (token)
            return await WindowsIdentity.RunImpersonatedAsync(token, async () =>
            {
                ct.ThrowIfCancellationRequested();
                var share = VdsImageShareAccess.UncShare(configuredRoot)
                    ?? throw new IOException("Configured image storage must be a UNC root for SMB authentication.");
                var networkUser = string.IsNullOrWhiteSpace(domain) ? user : domain + @"\" + user;
                var result = WNetAddConnection2(new NetResource { RemoteName = share },
                    settings.Password ?? "", networkUser, 0);
                if (result != 0)
                    throw new IOException("Cannot connect to the configured image share; Windows error " + result + ".",
                        new Win32Exception(result));
                try { return await operation().ConfigureAwait(false); }
                finally
                {
                    // A returned lock FileStream may still be open. Never force its disconnection.
                    // Both connect and cleanup run in this token's isolated logon session.
                    WNetCancelConnection2(share, 0, false);
                }
            }).ConfigureAwait(false);
    }

    public static (string User, string? Domain) Account(VdsImageShareOptions settings)
    {
        var user = settings.UserName?.Trim() ?? "";
        var domain = string.IsNullOrWhiteSpace(settings.Domain) ? null : settings.Domain.Trim();
        var separator = user.IndexOf('\\');
        if (separator > 0) return (user[(separator + 1)..], user[..separator]);
        return (user, user.Contains('@') ? null : domain);
    }

    [DllImport("advapi32.dll", EntryPoint = "LogonUserW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool LogonUser(string username, string? domain, string password,
        int logonType, int provider, out SafeAccessTokenHandle token);

    [DllImport("mpr.dll", EntryPoint = "WNetAddConnection2W", CharSet = CharSet.Unicode)]
    private static extern int WNetAddConnection2(NetResource resource, string password, string username, int flags);

    [DllImport("mpr.dll", EntryPoint = "WNetCancelConnection2W", CharSet = CharSet.Unicode)]
    private static extern int WNetCancelConnection2(string name, int flags, [MarshalAs(UnmanagedType.Bool)] bool force);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private sealed class NetResource
    {
        public int Scope;
        public int ResourceType = 1;
        public int DisplayType;
        public int Usage;
        public string? LocalName;
        public string? RemoteName;
        public string? Comment;
        public string? Provider;
    }
}
