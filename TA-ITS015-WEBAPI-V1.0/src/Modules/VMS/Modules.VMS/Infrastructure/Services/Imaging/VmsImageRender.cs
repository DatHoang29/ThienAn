using Modules.VMS.Controllers.Template.Dto;
using Modules.VMS.Core.Exceptions;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Modules.VMS.Infrastructure.Services.Imaging
{

    public class VmsImageRender : IScoped
    {
        private const int MaxDimension = 8000; // chặn ảnh quá lớn

        /// <summary>
        /// Parse "384x64" -> (384,64) kèm validate
        /// Author: ChuongLP
        /// Date: 2026-06-24
        /// </summary>
        public (int width, int height) ParseSize(string? size)
        {
            var dims = size?.Split('x');
            if (dims is not { Length: 2 })
                throw Oops.Oh(BaseMsg.VmsTemplate.Exception.SizeIncorrectFormat);

            if (!int.TryParse(dims[0], out int w) || !int.TryParse(dims[1], out int h)
                || w <= 0 || h <= 0 || w > MaxDimension || h > MaxDimension)
                throw Oops.Oh(BaseMsg.VmsTemplate.Exception.SizeIncorrectValue);

            return (w, h);
        }

        /// <summary>Tạo canvas + cấu hình chất lượng cao + fill nền. Caller tự dispose cả 2
        /// Author: ChuongLP
        /// Date: 2026-06-24
        /// .</summary>
        public (Bitmap bitmap, Graphics graphics) CreateCanvas(int width, int height, string? backColor)
        {
            var bitmap = new Bitmap(width, height);
            var g = Graphics.FromImage(bitmap);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.Clear(ToColor(backColor, Color.Black));
            return (bitmap, g);
        }

        /// <summary>
        /// Vẽ 1 đoạn text canh theo style trong rect. autoFit=true sẽ co/giãn font vừa khung.
        /// Author: ChuongLP
        /// Date: 2026-06-24
        /// </summary>
        public void DrawText(Graphics g, string? text, RectangleF rect, TextStyle style, bool autoFit = true, bool allowGrow = true)
        {
            if (string.IsNullOrEmpty(text)) return;

            var family = string.IsNullOrWhiteSpace(style.Font) ? "Arial" : style.Font!;
            var baseSize = int.TryParse(style.FontSize, out int s) && s > 0 ? s : 13;
            var fontStyle = style.Bold ? FontStyle.Bold : FontStyle.Regular;

            using var baseFont = new Font(family, baseSize, fontStyle);
            using var font = autoFit ? FitFont(g, text, rect.Size, baseFont, allowGrow) : (Font)baseFont.Clone();
            using var brush = new SolidBrush(ToColor(style.Color, Color.Green));
            using var format = new StringFormat
            {
                Alignment = ParseAlign(style.Align),
                LineAlignment = StringAlignment.Center
            };
            g.DrawString(text, font, brush, rect, format);
        }

        /// <summary>
        /// Vẽ ảnh từ url tương đối (WebRoot) hoặc data:image base64.
        /// Author: ChuongLP
        /// Date: 2026-06-24
        /// </summary>
        public void DrawImage(Graphics g, string? source, Rectangle rect)
        {
            if (string.IsNullOrWhiteSpace(source)) return;
            using var image = LoadImage(source!);
            if (image != null) g.DrawImage(image, rect);
        }

        // ---------- private helpers ----------

        private static StringAlignment ParseAlign(string? align) => align?.ToLower() switch
        {
            "left" => StringAlignment.Near,
            "right" => StringAlignment.Far,
            _ => StringAlignment.Center
        };

        /// <summary>
        /// Tìm font vừa khung (thay FindFont cũ) - clamp + tránh chia 0
        /// Author: ChuongLP
        /// Date: 2026-06-24
        /// .</summary>
        private Font FitFont(Graphics g, string text, SizeF room, Font preferred, bool allGrow = true,
                             float minSize = 6f, float maxSize = 200f)
        {
            SizeF real = g.MeasureString(text, preferred);
            if (real.Width <= 0 || real.Height <= 0) return (Font)preferred.Clone();

            float ratio = Math.Min(room.Height / real.Height, room.Width / real.Width);

            if(!allGrow && ratio > 1f)
                ratio = 1f; // chỉ co lại khi tràn, KHÔNG phóng quá cỡ đã chọn

            float newSize = Math.Clamp(preferred.Size * ratio, minSize, maxSize);
            return new Font(preferred.FontFamily, newSize, preferred.Style);
        }

        /// <summary>
        /// Lấy hình ảnh từ đường dẫn url
        /// Author: ChuongLP
        /// Date: 2026-06-24
        /// </summary>
        private Image? LoadImage(string source)
        {
            if (source.StartsWith("data:image"))
            {
                var parts = source.Split(',');
                if (parts.Length < 2) return null;
                var bytes = Convert.FromBase64String(parts[1]);
                using var ms = new MemoryStream(bytes);
                // QUAN TRỌNG: copy sang Bitmap mới để không phụ thuộc stream đã dispose
                using var tmp = Image.FromStream(ms);
                return new Bitmap(tmp);
            }

            var path = Path.Combine(App.WebHostEnvironment.WebRootPath, source);
            if (!File.Exists(path)) return null;
            // copy ra bitmap để KHÔNG khoá file (Image.FromFile giữ lock)
            using var fromFile = Image.FromFile(path);
            return new Bitmap(fromFile);
        }


        /// <summary>
        /// Description: Xuất byte ảnh theo định dạng
        /// Author: ChuongLP
        /// Date: 2026-06-24
        /// </summary>
        public byte[] ToBytes(Bitmap bmp, ImageFormat format)
        {
            using (var ms = new MemoryStream())
            {
                bmp.Save(ms, format);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Description: Trả data URI để FE bind thẳng vào thẻ img
        /// Author: ChuongLP
        /// Date: 2026-06-24
        /// </summary>
        public string ToBase64DataUri(Bitmap bmp, ImageFormat format, string mime)
        {
            string base64 = Convert.ToBase64String(ToBytes(bmp, format));
            return "data:" + mime + ";base64," + base64;
        }


        /// <summary>
        /// Description: Điều chỉnh độ sáng ảnh (0..100). 100 = giữ nguyên, 0 = tối đen.
        /// Author: ChuongLP
        /// Date: 2026-06-24
        /// </summary>
        public Bitmap ApplyBrightness(Bitmap source, int brightness)
        {
            if (brightness < 0) brightness = 0;
            if (brightness > 100) brightness = 100;

            // Giữ nguyên nếu sáng tối đa
            if (brightness == 100) return source;

            float scale = brightness / 100f;

            float[][] matrixItems = new float[][]
            {
                new float[] { scale, 0, 0, 0, 0 },
                new float[] { 0, scale, 0, 0, 0 },
                new float[] { 0, 0, scale, 0, 0 },
                new float[] { 0, 0, 0, 1, 0 },
                new float[] { 0, 0, 0, 0, 1 }
            };
            ColorMatrix colorMatrix = new ColorMatrix(matrixItems);

            ImageAttributes attributes = new ImageAttributes();
            attributes.SetColorMatrix(colorMatrix);

            Bitmap result = new Bitmap(source.Width, source.Height);
            Graphics graphics = Graphics.FromImage(result);
            try
            {
                Rectangle rect = new Rectangle(0, 0, source.Width, source.Height);
                graphics.DrawImage(source, rect, 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
            }
            finally
            {
                graphics.Dispose();
                attributes.Dispose();
                source.Dispose(); // bản gốc không cần nữa
            }
            return result;
        }

        /// <summary>
        /// Description: Render ảnh text 2 dòng ra Bitmap (in-memory) cho preview
        /// Author: ChuongLP
        /// Date: 2026-06-24
        /// </summary>
        public Bitmap RenderTwoContent(TextStyle style, string content1, string content2, string size, int brightness)
        {
            var dimension = ParseSize(size);
            int width = dimension.width;
            int height = dimension.height;

            var canvas = CreateCanvas(width, height, style.BackColor);
            Bitmap bitmap = canvas.bitmap;
            Graphics graphics = canvas.graphics;
            try
            {
                RectangleF topRect = new RectangleF(0, 0, width, height / 2f);
                RectangleF bottomRect = new RectangleF(0, height / 2f, width, height / 2f);

                // Nếu người dùng có chọn cỡ chữ hợp lệ -> tôn trọng cỡ đã chọn (chỉ co lại khi tràn khung, KHÔNG phóng to lấp đầy).
                // Nếu không có cỡ chữ -> giữ hành vi auto-fill cũ (co/giãn vừa khung) để không làm hồi quy dữ liệu cũ.
                bool hasExplicitFontSize = int.TryParse(style.FontSize, out int explicitSize) && explicitSize > 0;
                bool allowGrow = !hasExplicitFontSize;
                DrawText(graphics, content1, topRect, style, autoFit: true, allowGrow: allowGrow);
                DrawText(graphics, content2, bottomRect, style, autoFit: true, allowGrow: allowGrow);
            }
            finally
            {
                graphics.Dispose();
            }
            return ApplyBrightness(bitmap, brightness);
        }

        /// <summary>
        /// Description: Render template JSON ra Bitmap (in-memory) cho preview
        /// Author: ChuongLP
        /// Date: 2026-06-24
        /// </summary>
        public Bitmap RenderTemplate(string jsonContent, string size, int brightness)
        {
            var dimension = ParseSize(size);
            int width = dimension.width;
            int height = dimension.height;

            TemplateDesign template = JsonConvert.DeserializeObject<TemplateDesign>(jsonContent);
            if (template == null || template.Elements == null)
            {
                throw Oops.Oh(BaseMsg.VmsTemplate.Exception.JsonIncorrect);
            }

            var canvas = CreateCanvas(width, height, string.IsNullOrWhiteSpace(template.BackColor) ? "White" : template.BackColor);
            Bitmap bitmap = canvas.bitmap;
            Graphics graphics = canvas.graphics;
            try
            {
                foreach (TemplateElement element in template.Elements)
                {
                    Rectangle rect = new Rectangle(element.X, element.Y, element.Width, element.Height);
                    string type = element.Type == null ? "" : element.Type.ToLower();

                    switch (type)
                    {
                        case "text":
                            string text = element.Content == null ? "" : element.Content.ToString();
                            DrawText(graphics, text, rect, TextStyle.From(element), autoFit: true, allowGrow: false);
                            break;
                        case "image":
                            string elementContent = element.Content == null ? "" : element.Content.ToString();
                            TemplateContent content = JsonConvert.DeserializeObject<TemplateContent>(elementContent);
                            if (content != null)
                            {
                                DrawImage(graphics, content.Url, rect);
                            }
                            break;
                    }
                }
            }
            finally
            {
                graphics.Dispose();
            }
            return ApplyBrightness(bitmap, brightness);
        }

        /// <summary>
        /// Map tên màu / mã hex (#RRGGBB) sang Color, có fallback. (gộp từ VmsColor cũ)
        /// Author : ChuongLP
        /// Date: 2026-06-29
        /// </summary>
        private static Color ToColor(string? color, Color fallback)
        {
            if (string.IsNullOrWhiteSpace(color)) return fallback;

            switch (color)
            {
                case "Blue": return Color.Blue;
                case "Black": return Color.Black;
                case "White": return Color.White;
                case "Yellow": return Color.Yellow;
                case "LightGreen": return Color.LightGreen;
                case "Green": return Color.Green;
                case "Red": return Color.Red;
                case "OrangeRed": return Color.OrangeRed;
            }

            try { return ColorTranslator.FromHtml(color); }
            catch { return fallback; }
        }

    }
}
