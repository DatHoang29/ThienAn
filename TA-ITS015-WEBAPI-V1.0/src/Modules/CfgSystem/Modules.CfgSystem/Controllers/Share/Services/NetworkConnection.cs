using System.Net;
using System.Runtime.InteropServices;

namespace Modules.CfgSystem.Controllers.Share.Services
{
    public class NetworkConnection : IDisposable
    {
        private string _networkName;

        public NetworkConnection(string networkName, NetworkCredential credentials)
        {
            _networkName = networkName;

            var netResource = new NetResource
            {
                Scope = 2,
                ResourceType = 1,
                DisplayType = 3,
                RemoteName = networkName
            };

            var result = WNetAddConnection2(
                netResource,
                credentials.Password,
                credentials.UserName,
                0);

            if (result != 0)
            {
                throw new InvalidOperationException($"Error connecting to network share: {result}");
            }
        }

        ~NetworkConnection()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            WNetCancelConnection2(_networkName, 0, true);
        }

        [DllImport("mpr.dll")]
        private static extern int WNetAddConnection2(NetResource netResource,
            string password, string username, int flags);

        [DllImport("mpr.dll")]
        private static extern int WNetCancelConnection2(string name, int flags, bool force);

        [StructLayout(LayoutKind.Sequential)]
        private class NetResource
        {
            public int Scope;
            public int ResourceType;
            public int DisplayType;
            public int Usage;
            public string LocalName;
            public string RemoteName;
            public string Comment;
            public string Provider;
        }
    }
}
