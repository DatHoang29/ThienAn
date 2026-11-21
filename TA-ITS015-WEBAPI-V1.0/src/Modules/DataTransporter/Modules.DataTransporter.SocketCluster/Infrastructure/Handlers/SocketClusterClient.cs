using System.Configuration;
using System.Diagnostics;
using ScClient;
using Socket = ScClient.Socket;
namespace Modules.DataTransporter.SocketCluster.Infrastructure.Handlers
{
    /// <summary>
    /// Description: 
    /// Author:
    /// Create Date:
    ///</summary> 
    ///
    class SocketListener : IBasicListener
    {
        public void OnConnected(Socket socket)
        {

        }

        public void OnDisconnected(Socket socket)
        {
            try
            {
                Console.WriteLine("disconnected got called");

            }
            catch (Exception)
            {
            }
        }

        public void OnConnectError(Socket socket, SuperSocket.ClientEngine.ErrorEventArgs e)
        {

        }

        public void OnConnectError(Socket socket, ErrorEventArgs e)
        {
            try
            {
                Console.WriteLine("on connect error got called");

            }
            catch (Exception)
            {

            }

        }

        public void OnAuthentication(Socket socket, bool status)
        {
            try
            {
                Console.WriteLine(status ? "Socket is authenticated" : "Socket is not authenticated");
            }
            catch (Exception)
            {

            }
        }

        public void OnSetAuthToken(string token, Socket socket)
        {
            try
            {
                socket.SetAuthToken(token);
                Console.WriteLine("on set auth token got called");
            }
            catch (Exception)
            {

            }

        }

    }


    public class SocketClusterClient
    {

        protected static string socketEventServerUrl = "";
        protected static ScClient.Socket socketIOClient;
        private Dictionary<string, Socket.Channel> listChannel = new Dictionary<string, Socket.Channel>();


        static System.Timers.Timer tmrCheckSocket = new System.Timers.Timer();
        static bool isRunningCheck = false;
        static bool isConnected = false;
        public SocketClusterClient(bool isInit = false, string socketURL = "")
        {

            if (!string.IsNullOrEmpty(socketURL))
                socketEventServerUrl = socketURL;
            else
            {
                socketEventServerUrl = "ws://127.0.0.1:7046/socketcluster/";
            }
            if (isInit == true) //Có khởi tạo >> kết nối
                StartConnect();

            tmrCheckSocket = new System.Timers.Timer(10000);
            tmrCheckSocket.Elapsed += TmrCheckSocket_Elapsed;
            //tmrCheckSocket.Start();
            tmrCheckSocket.AutoReset = true;



        }
        /// <summary>
        /// Kiểm tra kết nối
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TmrCheckSocket_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {

            CheckConnect();

            isRunningCheck = false;
        }

        /// <summary>
        /// Kết nối
        /// </summary>
        public async void StartConnect()
        {
            try
            {
                
                socketIOClient = new ScClient.Socket(socketEventServerUrl);
                socketIOClient.SetListerner(new SocketListener());
                socketIOClient.SetReconnectStrategy(new ReconnectStrategy().SetMaxAttempts(0));
                socketIOClient._socket.NoDelay = true;
                socketIOClient.Connect();

                //Opened event
                socketIOClient._socket.Opened += (object sender, EventArgs e) =>
                {
                    isConnected = true;
                    tmrCheckSocket.Stop();
                    //Đăng ký lại channel
                    ReRegisterChannel();
                };

                //Closed event
                socketIOClient._socket.Closed += (object sender, EventArgs e) =>
                {
                    isConnected = false;
                    if (isRunningCheck) return;
                    isRunningCheck = true;
                    //Bật timer check reconnect
                    tmrCheckSocket.Start();
                    Debug.WriteLine("Stop socket");
                };

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Kiểm tra kết nối
        /// </summary>
        public void CheckConnect()
        {
            try
            {
                if (socketIOClient._socket.State != WebSocket4Net.WebSocketState.Open)
                {
                    socketIOClient.Disconnect();
                    socketIOClient.Connect();

                }
                else
                {
                    tmrCheckSocket.AutoReset = false;
                }
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Đăng ký channel
        /// </summary>
        /// <param name="channelName"></param>
        /// <returns></returns>
        public bool RegisterChannel(string channelName)
        {
            try
            {
                if (listChannel.Any(l => l.Key == channelName)) return true;

                listChannel.Add(channelName, socketIOClient.CreateChannel(channelName));
                return true;
            }
            catch
            {
                return false;

            }

        }
        /// <summary>
        /// Đăng ký lại tất cả channel
        /// </summary>
        /// <returns></returns>
        public bool ReRegisterChannel()
        {
            try
            {
                var existChannels = socketIOClient.GetChannels();

                foreach (var channel in listChannel)
                {
                    if(existChannels.All(l => l.GetChannelName() != channel.Key))
                        listChannel[channel.Key] = socketIOClient.CreateChannel(channel.Key);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Lấy ra channel từ danh sách
        /// </summary>
        /// <param name="channelName"></param>
        /// <returns></returns>
        public Socket.Channel? SelectChannel(string channelName)
        {
            return listChannel.FirstOrDefault(l => l.Key == channelName).Value;

        }



        /// <summary>
        /// Gửi dữ liệu lên channel
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool BroadcastData(string channelName, object data)
        {
            try
            {
                if (!isConnected) return false;
                SelectChannel(channelName)?.Publish(data);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Subscribe channel để đọc dữ liệu từ channel
        /// Sử dụng event OnMessage để đọc
        /// </summary>
        /// <param name="channelName"></param>
        /// <returns></returns>
        public Socket.Channel? SubscribeData(string channelName)
        {
            return SelectChannel(channelName)?.Subscribe();
            
        }

    }
}
