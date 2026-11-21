using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modules.Chainzones.Controllers
{
    public class VMSChainzonesAdapter : IVMSChanzoneAdapter
    {
        IVMSChanzoneAdapter vms;
        public VMSChainzonesAdapter(IVMSChanzoneAdapter _vms)
        {
            vms = _vms;

        }



        /// <summary>
        /// Khoi tao ket noi VMS
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public bool fn_InitVMS(string ip, int port, int width, int height)
        {
            return vms.fn_InitVMS(ip, port, width, height);

        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public bool fn_SendImage(string filePath)
        {
            return vms.fn_SendImage(filePath);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public Image fn_ReadImage()
        {
            return vms.fn_ReadImage();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public bool fn_SendText(string text)
        {
            return vms.fn_SendText(text);
        }

        #region API
        public bool fn_AdjustBright_API(ushort bright)
        {
            return vms.fn_AdjustBright_API(bright);
        }
        public bool fn_InitVMS_Api(string ip, uint port = 9520)
        {
            return vms.fn_InitVMS_Api(ip, port);
        }

        public Tuple<byte, byte, byte> fn_ReadBrightness_API()
        {
            return vms.fn_ReadBrightness_API();
        }

        public bool fn_ReadPicture_API(string filePath, string ip, uint port = 9520)
        {
            return vms.fn_ReadPicture_API(filePath, ip, port);
        }
        public bool fn_SendImage_API(string filePath)
        {
            return vms.fn_SendImage_API(filePath);
        }

        public object fn_ReadStats_API(params int[] chooseTypes)
        {
            return vms.fn_ReadStats_API(chooseTypes);
        }

        public bool fn_ResetSystem()
        {
            return vms.fn_ResetSystem();
        }
        #endregion
    }
}
