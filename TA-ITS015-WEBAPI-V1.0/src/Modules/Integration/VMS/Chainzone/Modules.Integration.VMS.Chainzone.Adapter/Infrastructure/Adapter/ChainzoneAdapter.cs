using System.Drawing;
using Modules.Integration.VMS.Base.Core.Enums;

namespace Modules.Integration.VMS.Chainzone.Adapter.Infrastructure.Adapter
{
    public class ChainzoneAdapter
    {
        private readonly czVMSII.czVMSII _czVMSII;
        private readonly ChainzoneApiAdapter _apiAdapter;

        /// <summary>
        /// Constructor
        /// </summary>
        public ChainzoneAdapter()
        {
            //Sdk init
            _czVMSII = new czVMSII.czVMSII();

            //_czVMSII.boardHeight = 64;
            //_czVMSII.boardWidth = 560;

            //Các cấu hình mặc định cho thiết bị
            _czVMSII.justificationPage = czVMSII.DMSVariables.JustificationPage.Middle;
            _czVMSII.justificationLine = czVMSII.DMSVariables.JustificationLine.Center;

            _czVMSII.colorBackground = Color.Black;
            _czVMSII.colorText = Color.Green;

            _czVMSII.SpaceText = 1;
            _czVMSII.SpaceLine = 3;
            _czVMSII.NumberFont = 24;

            //API init
            _apiAdapter = new ChainzoneApiAdapter();

        }

        #region Sdk

        #region Init

        /// <summary>
        /// Khởi tạo thông tin kết nối qua Sdk
        /// </summary>
        /// <param name="ip">Ip thiết bị</param>
        /// <param name="port">Port Sdk thiết bị</param>
        /// <param name="width">Chiều rộng</param>
        /// <param name="height">Chiều cao</param>
        /// <returns></returns>
        public bool Initialize(string ip, int port = 161, int width = default, int height = default)
        {
            try
            {
                _czVMSII.IPAdress = ip;
                _czVMSII.Port = port;

                _czVMSII.boardHeight = height;
                _czVMSII.boardWidth = width;
                _czVMSII.Init();

                return true;

            }
            catch (Exception)
            {

                return false;
            }

        }


        #endregion


        #region Send 

        /// <summary>
        /// Gửi hình ảnh qua Sdk
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public bool SendImage(string filePath)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists)
                {
                    return false;
                }
                Image img = Image.FromFile(filePath);
                bool flag = _czVMSII.SendImage(img, 1, "image1");
                return flag;
            }
            catch (Exception)
            {

                return false;
            }
        }

        /// <summary>
        /// Gửi nội dung thông diệp qua Api
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public bool SendText(string text)
        {
            try
            {

                bool flag = _czVMSII.SendText(text, 1);
                return flag;
            }
            catch (Exception)
            {

                return false;
            }
        }



        #endregion


        #region Read

        /// <summary>
        /// Đọc hình ảnh qua sdk
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public Image ReadImage()
        {
            try
            {
                Image img = _czVMSII.GetImage(Convert.ToInt32(1));
                return img;
            }
            catch (Exception)
            {

                return null;
            }
        }

        #endregion


        #region System



        #endregion

        #endregion



        #region Api

        
        #region Init

        
        /// <summary>
        /// Khơi tạo thông tin kết nối qua API
        /// </summary>
        /// <param name="ip">Ip thiết bị</param>
        /// <param name="port">Port API thiết bị</param>
        /// <returns></returns>
        public bool InitializeApi(string ip, uint port = 9520)
        {
            try
            {
                return _apiAdapter.Initialize(ip, port);



            }
            catch (Exception)
            {

                return false;
            }

        }

        #endregion


        #region Send 

        
        /// <summary>
        /// Gửi hình ảnh qua Api
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public bool SendImageApi(string filePath)
        {
            try
            {
                _apiAdapter.SendPicture(filePath);
                return true;

            }
            catch (Exception)
            {

                return false;
            }
        }

        /// <summary>
        /// Thiết lập độ sáng thiết bị
        /// Giá trị: 0 - 100
        /// 0: tự động (sử dụng cảm biến)
        /// 1 - 100: giá trị độ sáng
        /// </summary>
        /// <param name="brightness"></param>
        /// <returns></returns>

        public bool AdjustBrightnessApi(ushort brightness)
        {
            try
            {
                return _apiAdapter.AdjustBrightness(brightness);
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion


        #region Read

       /// <summary>
       /// Đọc hình ảnh qua Api
       /// </summary>
       /// <param name="filePath"></param>
       /// <param name="ipAddress"></param>
       /// <param name="port"></param>
       /// <returns></returns>
        public bool ReadImageApi(string filePath)
        {
            try
            {

                string deviceFilePath = "D:\\p\\1.bmp";

                var t = new Task(() =>
                {
                    _apiAdapter.ReadPicture(filePath, deviceFilePath, _apiAdapter.IpAddress, _apiAdapter.Port);
                });
                t.Start();
                var rs = t.Wait(1500); // chờ 1.5s
                return rs;


            }
            catch (Exception ex)
            {
                throw ex;
                return false;
            }
        }


        /// <summary>
        /// Đọc độ sáng thiết bị qua API
        /// </summary>
        /// <returns></returns>
        public Tuple<byte, byte, byte>? ReadBrightnessApi()
        {
            try
            {
                byte bright = default;
                byte value = default;
                byte ad = default;
                _apiAdapter.ReadBrightness(ref bright, ref value, ref ad);
                return new Tuple<byte, byte, byte>(bright, value, ad);

            }
            catch (Exception ex)
            {
                return null;
            }
        }


        /// <summary>
        /// Đọc thông số hoạt động qua Api
        /// </summary>
        /// <param name="chooseTypes">Lựa chọn nội dung thông số</param>
        /// <returns></returns>
        public Dictionary<string,object>? ReadStat_API(params IntgVmsEnums.IntgChainzoneStatType[] chooseTypes)
        {
            try
            {
                var statTypes = Enum.GetValues(typeof(IntgVmsEnums.IntgChainzoneStatType)).Cast<IntgVmsEnums.IntgChainzoneStatType>()
                    .ToDictionary(e => e, e => (byte)e);

                //Dictionary<byte, string> statTypes = new Dictionary<byte, string>()
                //{
                //    { 0, "all status" },
                //    { 1, "<i class=\"fas fa-temperature-high\">{0}</i>" },
                //    { 2, "temp outside" },
                //    { 3, "humidity inside" },
                //    { 4, "humidity outside" },
                //    { 5, "gradient /tilting angle" },
                //    { 6, "wind direction" },
                //    { 7, "wind speed" },
                //    { 8, "<i class=\"fas fa-sun\">{0}</i>" },
                //    { 9, "door open" },
                //    { 10, "power status" },
                //    { 11, "firmware Ver" },
                //    { 12, "pixel error count" }
                //};

                if (chooseTypes.Length > 0)
                {
                    statTypes = statTypes.Where(l => chooseTypes.Contains(l.Key)).ToDictionary(l => l.Key, l => l.Value);

                }
                Dictionary<string,object> output = new Dictionary<string,object>();
                foreach (var item in statTypes)
                {
                    Byte value = default;
                    byte stats = item.Value;

                    var t = new Task(() =>
                    {
                        _apiAdapter.ReadStat(stats, ref value, 1);
                    });
                    t.Start();

                    var rs = t.Wait(2000); //Chờ 2s
                    //if (rs)
                    //{
                        // If the task completes within the timeout, return true
                        output.Add(item.Value +"",value +"" ?? "");
                    //}
                    //else
                    //{
                    //    // If the task exceeds the timeout, return false
                    //    //return false;
                    //    output.Add(string.Format(item.Value, ""));
                    //    continue;
                    //}
                }


                return output;


            }
            catch (Exception)
            {

                return null;
            }
        }


        #endregion


        #region System

        /// <summary>
        /// Reset device qua Api
        /// </summary>
        /// <returns></returns>
        public bool ResetDevice()
        {
            try
            {
                return _apiAdapter.ResetDevice();
            }
            catch (Exception)
            {

                return false;
            }
        }


        #endregion


        #endregion

    }
}
