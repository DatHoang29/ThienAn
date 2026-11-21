using System.Drawing;

namespace Modules.Chainzones.Controllers.Chainzone
{
    public class VMSChainzoneControl : IVMSChanzoneAdapter
    {
        czVMSII.czVMSII vczVMSII;
        VMSChainzoneApi api;

        /// <summary>
        /// 
        /// </summary>
        public VMSChainzoneControl()
        {
            vczVMSII = new czVMSII.czVMSII();

            //vczVMSII.boardHeight = 64;
            //vczVMSII.boardWidth = 560;

            vczVMSII.justificationPage = czVMSII.DMSVariables.JustificationPage.Middle;
            vczVMSII.justificationLine = czVMSII.DMSVariables.JustificationLine.Center;

            vczVMSII.colorBackground = Color.Black;
            vczVMSII.colorText = Color.Red;

            vczVMSII.SpaceText = 1;
            vczVMSII.SpaceLine = 3;
            vczVMSII.NumberFont = 24;

            //API
            api = new VMSChainzoneApi();
            //vczVMSII.Init();}
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public bool fn_InitVMS(string ip, int port = 161, int width = default, int height = default)
        {
            try
            {
                vczVMSII.IPAdress = ip;
                vczVMSII.Port = port;

                vczVMSII.boardHeight = height;
                vczVMSII.boardWidth = width;
                vczVMSII.Init();

                return true;

            }
            catch (Exception)
            {

                return false;
            }

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public bool fn_InitVMS_Api(string ip, uint port = 9520)
        {
            try
            {
                return api.fn_Init(ip, port);



            }
            catch (Exception)
            {

                return false;
            }

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public bool fn_SendImage(string filePath)
        {
            try
            {
                FileInfo f = new FileInfo(filePath);
                Image img = Image.FromFile(filePath);
                bool flag = vczVMSII.SendImage(img, 1, "image1");
                return flag;
            }
            catch (Exception)
            {

                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public Image fn_ReadImage()
        {
            try
            {
                Image img = vczVMSII.GetImage(Convert.ToInt32(1));
                return img;
            }
            catch (Exception)
            {

                return null;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public bool fn_SendText(string text)
        {
            try
            {

                bool flag = vczVMSII.SendText(text, 1);
                return flag;
            }
            catch (Exception)
            {

                return false;
            }
        }




        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public Tuple<byte, byte, byte> fn_ReadBrightness_API()
        {
            try
            {
                byte bright = default;
                byte value = default;
                byte ad = default;
                api.fn_ReadBrightInfo(ref bright, ref value, ref ad);
                return new Tuple<byte, byte, byte>(bright, value, ad);

            }
            catch (Exception)
            {

                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public bool fn_ReadPicture_API(string filePath, string Ip, uint port = 9520)
        {
            try
            {

                string PictureName = "D:\\p\\1.bmp";

                //var read = Task.Run(() => api.fn_ReadPicture(filePath, PictureName, Ip, port));
                //var read = api.fn_ReadPicture(filePath, PictureName,Ip,port);

                var t = new Task(() =>
                {
                    api.fn_ReadPicture(filePath, PictureName, Ip, port);
                });
                t.Start();
                var rs = t.Wait(1500);
                return rs;

                //if (Task.WaitAll(new Task[] { read }, 1500))
                //{
                //    // If the task completes within the timeout, return true
                //    return true;
                //}
                //else
                //{
                //    // If the task exceeds the timeout, return false
                //    return false;
                //}

            }
            catch (Exception ex)
            {
                throw ex;
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public bool fn_SendImage_API(string filePath)
        {
            try
            {
                api.fn_SendPicture(filePath);
                return true;

            }
            catch (Exception)
            {

                return false;
            }
        }

        ///
        public bool fn_ResetSystem()
        {
            try
            {
                return api.fn_ResetSystem();
            }
            catch (Exception)
            {

                return false;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public object fn_ReadStats_API(params int[] chooseTypes)
        {
            try
            {
                Dictionary<byte, string> statTypes = new Dictionary<byte, string>()
                {
                    { 0, "all status" },
                    { 1, "<i class=\"fas fa-temperature-high\">{0}</i>" },
                    { 2, "temp outside" },
                    { 3, "humidity inside" },
                    { 4, "humidity outside" },
                    { 5, "gradient /tilting angle" },
                    { 6, "wind direction" },
                    { 7, "wind speed" },
                    { 8, "<i class=\"fas fa-sun\">{0}</i>" },
                    { 9, "door open" },
                    { 10, "power status" },
                    { 11, "firmware Ver" },
                    { 12, "pixel error count" }
                };

                if (chooseTypes.Length > 0)
                {
                    statTypes = statTypes.Where(l => chooseTypes.Contains(l.Key)).ToDictionary(l => l.Key, l => l.Value);

                }
                List<string> statsList = new List<string>();
                foreach (var item in statTypes)
                {
                    Byte doorStat = default;
                    byte stats = item.Key;
                    var t = new Task(() =>
                    {
                        api.fn_ReadStats(stats, ref doorStat, 1);
                    });
                    t.Start();
                    var rs = t.Wait(2000);
                    //var read = api.fn_ReadPicture(filePath, PictureName,Ip,port);
                    if (rs)
                    {
                        // If the task completes within the timeout, return true
                        statsList.Add(string.Format(item.Value, doorStat));
                    }
                    else
                    {
                        // If the task exceeds the timeout, return false
                        return false;
                    }
                }


                return statsList;


            }
            catch (Exception)
            {

                return false;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>

        public bool fn_AdjustBright_API(ushort bright)
        {
            try
            {
                return api.fn_AdjustBright(bright);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
