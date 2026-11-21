using System.Net.NetworkInformation;

namespace Shared.Utility.Helpers
{
    public class NetworkHelper
    {
        /// <summary>
        /// Description: Ping kiểm tra kết nối tới IP
        /// Hàm này sử dụng task và thread để kiểm tra kết nối tới IP
        /// Author: 
        /// Create Date:
        /// </summary>
        public static bool CheckConnect(string ipAddress, int timeout = 200)
        {
            var rs = ForcePingTimeoutWithThreads(ipAddress, timeout);
            if (rs != null && rs.Status == IPStatus.Success)
            {
                return true;

            }
            return false;
        }
        /// <summary>
        /// Description: Ping kiểm tra kết nối tới Ip (cách cũ)
        /// Author: 
        /// Create Date:
        /// </summary>
        public static bool CheckConnectOld(string ipAddress, int timeout = 200)
        {
            return PingHost(ipAddress, timeout);
        }

        

        /// <summary>
        /// Description: Ping kiểm tra kết nối IP 
        /// Author: 
        /// Create Date:
        /// </summary>

        private static bool PingHost(string nameOrAddress, int timeout = 10)
        {
            bool flag = false;
            Ping ping = (Ping)null;
            try
            {
                ping = new Ping();
                flag = ping.Send(nameOrAddress, timeout).Status == IPStatus.Success;
            }
            catch (PingException ex)
            {
            }
            finally
            {
                ping?.Dispose();
            }

            return flag;
        }

        /// <summary>
        /// Description: 
        /// Author: 
        /// Create Date:
        /// </summary>
        private static PingReply? ForcePingTimeoutWithThreads(string hostname, int timeout)
        {
            PingReply reply = null;
            var a = new Thread(() => reply = NormalPing(hostname, timeout));
            a.Start();
            a.Join(timeout);

            return reply;
        }


        /// <summary>
        /// Description: 
        /// Author: 
        /// Create Date:
        /// </summary>
        private static PingReply? NormalPing(string hostname, int timeout)
        {
            try
            {
                return new Ping().Send(hostname, timeout);
            }
            catch
            {
                return null;
            }
        }
        
    }
}
