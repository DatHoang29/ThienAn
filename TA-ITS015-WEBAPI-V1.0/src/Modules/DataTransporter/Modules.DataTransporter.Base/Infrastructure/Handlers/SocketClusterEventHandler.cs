using Modules.DataTransporter.Base.Core.Abstractions;
using Modules.DataTransporter.SocketCluster.Infrastructure.Handlers;
namespace Modules.DataTransporter.Base.Infrastructure.Handlers
{

    public delegate void MessageReceived(string channelName, object data);

    /// <summary>
    /// Handler xử lý dữ liệu qua socket cluster
    /// </summary>
    public class SocketClusterEventHandler : IBaseEventHandler
    {
        private SocketClusterClient _socketClusterClient;
        public event MessageReceived OnMessageReceived;
        public event DataReceivedHandler OnDataReceived;
        public SocketClusterEventHandler(bool isInit = false, string socketURL = "")
        {
            _socketClusterClient = new SocketClusterClient(isInit, socketURL);
            
        }
        /// <summary>
        /// Description: Đăng ký channel/topic
        /// Author: SonTH
        /// Create date: 2024/10/16
        /// </summary>
        /// <param name="keyName"></param>
        /// <returns></returns>
        public bool RegisterChannel(string keyName)
        {
            return _socketClusterClient.RegisterChannel(keyName);
        }

        /// <summary>
        /// Description: Gửi dữ liệu lên channel/topic
        /// Author: SonTH
        /// Create date: 2024/10/16
        /// </summary>
        /// <param name="keyName"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool PublishData(string keyName, object data)
        {
            return _socketClusterClient.BroadcastData(keyName, data);
        }

        /// <summary>
        /// Description: TODO Đọc dữ liệu từ channel/topic
        /// Author: SonTH
        /// Create date: 2024/10/16
        /// </summary>
        /// <param name="keyName"></param>
        /// <returns></returns>
        public bool SubscribeData(string keyName)
        {
            var channel = _socketClusterClient.SubscribeData(keyName);
            channel.OnMessage((name, data) =>
            {
                
                HandleMessage(name, data);
            });
            return true;
        }

        private void HandleMessage(string channelName, object data)
        {
            OnMessageReceived?.Invoke(channelName, data);
            OnDataReceived?.Invoke(channelName, data);
        }
    }
}
