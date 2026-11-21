using Microsoft.AspNetCore.Http;
using Modules.VMS.Controllers.Template.Dto;
using Modules.VMS.Core.Exceptions;
using Modules.VMS.Infrastructure.Services.Imaging;
using System.Drawing;
using System.Drawing.Imaging;

namespace Modules.VMS.Infrastructure.Services.Template;

public class TemplateService
{
    private readonly VmsImageRender _renderer;
    public TemplateService(VmsImageRender renderer)
    {
        _renderer = renderer;
    }

    /// <summary>
    /// Generate image from template JSON
    /// </summary>
    /// Author: ChuongLP
    /// Date: 2026-06-24
    /// <returns></returns>
    public Task<IFormFile> GenerateImage(string jsonContent, string templateSize)
    {
        // Validate NGOÀI try để không bị che thành GenerateError
        var (width, height) = _renderer.ParseSize(templateSize);

        var template = JsonConvert.DeserializeObject<TemplateDesign>(jsonContent);
        if (template?.Elements == null)
            throw Oops.Oh(BaseMsg.VmsTemplate.Exception.JsonIncorrect);

        try
        {
            var (bitmap, graphics) = _renderer.CreateCanvas(width, height, "White");
            using (graphics)
            using (bitmap)
            {
                foreach (var el in template.Elements)
                {
                    var rect = new Rectangle(el.X, el.Y, el.Width, el.Height);
                    switch (el.Type?.ToLower())
                    {
                        case "text":
                            // dùng TextStyle -> render được màu/font theo element
                            _renderer.DrawText(graphics, el.Content?.ToString(), rect, TextStyle.From(el));
                            break;
                        case "image":
                            var content = JsonConvert.DeserializeObject<TemplateContent>(el.Content?.ToString() ?? "");
                            _renderer.DrawImage(graphics, content?.Url, rect);
                            break;
                    }
                }

                // === GIỮ NGUYÊN output: IFormFile ===
                var ms = new MemoryStream();
                bitmap.Save(ms, ImageFormat.Bmp);
                ms.Position = 0;
                string fileName = $"template_{DateTime.Now:yyyyMMddHHmmss}.bmp";
                return Task.FromResult<IFormFile>(new FormFile(ms, 0, ms.Length, "image", fileName)
                {
                    Headers = new HeaderDictionary(),
                    ContentType = "image/bmp"
                });
            }
        }
        catch (Exception ex)
        {
            throw Oops.Oh(BaseMsg.VmsTemplate.Exception.GenerateError, ex.Message);
        }
    }
}

