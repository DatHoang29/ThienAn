using Modules.VMS.Controllers.Control.Dtos;
using Modules.VMS.Controllers.Template.Dto;
using Modules.VMS.Infrastructure.Services.Imaging;
using System.Drawing;
using System.Drawing.Imaging;

namespace Modules.VMS.Controllers.Control.Queries
{
    public class ControlQueryHandler : IScoped
    {
        private readonly VmsImageRender _renderer;

        public ControlQueryHandler(VmsImageRender renderer)
        {
            _renderer = renderer;
        }

        /// <summary>
        /// Lâìy hiÌnh aÒnh xem trýõìc dýòa trên thông tin truyêÌn vaÌo
        /// Author: ChuongLP
        /// Date: 2026-06-24
        /// </summary>
        public Task<PreviewOutput> HandleAsync(PreviewInput input)
        {
            string controlType = input.ControlType == null ? "" : input.ControlType.Trim().ToLower();
            string size = string.IsNullOrEmpty(input.Size) ? "384x64" : input.Size;
            int brightness = input.Brightness == null ? 100 : input.Brightness.Value;

            Bitmap bitmap;

            if (controlType == "template")
            {
                string json = input.JsonContent == null ? "" : input.JsonContent;
                bitmap = _renderer.RenderTemplate(json, size, brightness);
            }
            else
            {
                TextStyle style = new TextStyle();
                style.Color = string.IsNullOrEmpty(input.Color) ? "Green" : input.Color;
                style.BackColor = string.IsNullOrEmpty(input.BackColor) ? "Black" : input.BackColor;
                style.Font = input.Font;
                style.FontSize = input.FontSize;
                bitmap = _renderer.RenderTwoContent(style, input.Content1, input.Content2, size, brightness);
            }

            PreviewOutput output = new PreviewOutput();
            try
            {
                output.Base64 = _renderer.ToBase64DataUri(bitmap, ImageFormat.Png, "image/png");
                output.ContentType = "image/png";
            }
            finally
            {
                bitmap.Dispose();
            }
            return Task.FromResult(output);
        }
    }
}
