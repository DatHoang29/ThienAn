using Mapster;
using Modules.VMS.Controllers.Control.Dtos;
using Modules.VMS.Controllers.Template.Dto;
using Modules.VMS.Core.Exceptions;
using Shared.DTO.Constants.Module;
using Shared.DTO.Upload;
using System.Drawing;
using System.Drawing.Imaging;

namespace Modules.VMS.Infrastructure.Services.Imaging
{
    /// <summary>
    /// Description: Service gen ảnh VMS dùng chung cho luồng điều khiển thiết bị.
    ///              Một entry GenerateImage tự switch theo ControlType, gọi lõi render tương ứng,
    ///              lưu file và trả về url tương đối.
    /// Author: ChuongLP
    /// Created date: 2026-06-25
    /// </summary>
    public class VmsImageGenService : IScoped
    {
        private readonly VmsImageRender _renderer;

        public VmsImageGenService(VmsImageRender renderer)
        {
            _renderer = renderer;
        }

        /// <summary>
        /// Author: ChuongLP
        /// Description: Hàm vào duy nhất - tự switch loại điều khiển, render ảnh rồi lưu file, trả về url tương đối.
        /// Created date: 2026-06-25
        /// </summary>
        /// <param name="input">Thông tin điều khiển (text/message/phrase/template)</param>
        /// <param name="savePath">Thư mục gốc để lưu file (thường là WebRootPath)</param>
        /// <returns>Url tương đối của ảnh đã lưu</returns>
        public string GenerateImage(AddControlInput input, string savePath)
        {
            // 1. Render bitmap theo loại điều khiển
            Bitmap bitmap = RenderByControlType(input);

            // 2. Lưu file và trả url
            try
            {
                string url = SaveBitmap(bitmap, savePath);
                return url;
            }
            finally
            {
                bitmap.Dispose();
            }
        }

        /// <summary>
        /// Author: ChuongLP
        /// Description: Switch trung tâm - chọn cách render theo ControlType.
        ///              Template thì render từ JSON; Text/Message/Phrase thì render 2 dòng nội dung.
        /// Created date: 2026-06-25
        /// </summary>
        /// <param name="input">Thông tin điều khiển</param>
        /// <returns>Bitmap đã render (caller chịu trách nhiệm Dispose)</returns>
        private Bitmap RenderByControlType(AddControlInput input)
        {
            string size = string.IsNullOrEmpty(input.Size) ? "384x64" : input.Size;
            int brightness = input.Brightness ?? 100;

            switch (input.ControlType)
            {
                case VmsControlTypeConst.Template:
                    return _renderer.RenderTemplate(input.JsonContent ?? "", size, brightness);

                case VmsControlTypeConst.Text:
                case VmsControlTypeConst.Message:
                case VmsControlTypeConst.Phrase:
                    var style = BuildTextStyle(input);
                    return _renderer.RenderTwoContent(style, input.Content1 ?? "", input.Content2 ?? "", size, brightness);

                default:
                    throw Oops.Oh(BaseMsg.BaseException.IncorrectAcceptValue, BaseMsg.BaseEntity.Type);
            }
        }

        /// <summary>
        /// Dựng TextStyle từ input, gom giá trị mặc định về một chỗ.
        /// Author: ChuongLP
        /// Date: 2026-06-29
        /// </summary>
        private static TextStyle BuildTextStyle(AddControlInput input) => new TextStyle
        {
            Color = string.IsNullOrEmpty(input.Color) ? "Green" : input.Color,
            BackColor = string.IsNullOrEmpty(input.BackColor) ? "Black" : input.BackColor,
            Font = input.Font,
            FontSize = input.FontSize
        };


        /// <summary>
        /// Author: ChuongLP
        /// Description: Lưu bitmap ra thư mục savePath/subFolder và trả về url tương đối.
        /// Created date: 2026-06-25
        /// </summary>
        /// <param name="bitmap">Ảnh cần lưu</param>
        /// <param name="savePath">Thư mục gốc để lưu (WebRootPath)</param>
        /// <returns>Url tương đối subFolder/fileName</returns>
        private string SaveBitmap(Bitmap bitmap, string savePath)
        {
            DateTime date = DateTime.Now;
            string fileName = $"vms_{date:yyyyMMdd_HHmmss_fff}.bmp";
            string subFolder = Path.Combine(BaseUploadConfig.Vms.Template, $"{date:yyyyMM}", $"{date:dd}");
            string imageDirectory = Path.Combine(savePath, subFolder);

            if (!Directory.Exists(imageDirectory))
            {
                Directory.CreateDirectory(imageDirectory);
            }

            string imageFilePath = Path.Combine(savePath, subFolder, fileName);
            bitmap.Save(imageFilePath, ImageFormat.Bmp);

            return Path.Combine(subFolder, fileName);
        }
    }
}
