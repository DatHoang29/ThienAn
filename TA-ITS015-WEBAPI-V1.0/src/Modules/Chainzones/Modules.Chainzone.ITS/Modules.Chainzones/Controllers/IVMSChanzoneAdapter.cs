using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modules.Chainzones.Controllers
{
    public interface IVMSChanzoneAdapter
    {

        bool fn_InitVMS(string ip, int port, int width, int height);
        bool fn_SendImage(string filePath);
        Image fn_ReadImage();
        bool fn_SendText(string text);
        bool fn_InitVMS_Api(string ip, uint port = 9520);
        Tuple<byte, byte, byte> fn_ReadBrightness_API();
        bool fn_ReadPicture_API(string filePath, string Ip, uint port = 9520);
        bool fn_SendImage_API(string filePath);
        bool fn_AdjustBright_API(ushort bright);
        object fn_ReadStats_API(params int[] chooseTypes);
        bool fn_ResetSystem();
    }
}
