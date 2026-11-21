using System.Collections.Concurrent;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Modules.Mail.Core.Dto.Smtp;
using Modules.Mail.Infrastructure.Services.Mail.Templates;

namespace Modules.Mail.Infrastructure.Services.Mail.Delivery
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Ảnh nhúng trong thân thư theo cơ chế CID.
    ///
    ///              Hai nguồn ảnh:
    ///                - Ảnh người dùng chèn trong wangEditor: là link http(s) / đường dẫn tới file của CHÍNH server này
    ///                  (upload lên wwwroot). Lúc GỬI, ảnh tìm được file trên server thì đọc file, gửi kèm thư thành một
    ///                  phần riêng và đổi src sang cid:. Ảnh của máy chủ khác giữ nguyên link.
    ///                - &lt;img src="cid:logo"&gt;: tên "logo" tra ở Mail:InlineImages ra đường dẫn file tính từ wwwroot.
    ///
    ///              VÌ SAO CID, KHÔNG PHẢI BASE64 TRONG HTML: Gmail chặn ảnh src="data:...", Outlook hiện dấu X; còn link
    ///              http thì cần server công khai và Outlook mặc định chặn tải ảnh. CID đặt ảnh ở một phần riêng của thư
    ///              (multipart/related) — mọi hộp thư đều hiện, người nhận không phải bấm "tải ảnh".
    ///              Riêng bản XEM TRƯỚC trên web thì đổi cid: sang data: vì trình duyệt không hiểu cid:.
    ///
    ///              Nội dung file cache theo thời điểm sửa file. Thiếu file thì bỏ qua ảnh đó (thư vẫn gửi) và ghi cảnh báo.
    /// Created date: 30/09/2026
    /// </summary>
    public class MailInlineImageService : IScoped
    {
        private static readonly Regex CidPattern = new(@"cid:([A-Za-z0-9_.-]+)", RegexOptions.Compiled);

        /// <summary>Thuộc tính src của thẻ img: nhóm 1 = phần trước giá trị, nhóm 2 = dấu nháy, nhóm 3 = giá trị.</summary>
        private static readonly Regex ImgSrcPattern = new(
            @"(<img\b[^>]*?\bsrc\s*=\s*)([""'])(.*?)\2", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>Tiền tố cid của ảnh lấy từ nội dung thư — tránh trùng tên khai ở Mail:InlineImages.</summary>
        private const string ContentCidPrefix = "mail-img-";

        /// <summary>Đường dẫn bắt đầu bằng "share/" là file ở Upload:RootPath (xem FileServerUtil.GetPath).</summary>
        private const string SharePrefix = "share/";

        /// <summary>Cache nội dung ảnh: khoá = đường dẫn đầy đủ, kèm thời điểm sửa file để biết file đã đổi.</summary>
        private static readonly ConcurrentDictionary<string, (DateTime Modified, byte[] Bytes)> Cache = new(StringComparer.OrdinalIgnoreCase);

        private readonly MailSettings _settings;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MailInlineImageService> _logger;

        public MailInlineImageService(IOptions<MailSettings> settings, IWebHostEnvironment environment,
            IConfiguration configuration, ILogger<MailInlineImageService> logger)
        {
            _settings = settings.Value;
            _environment = environment;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Chuẩn bị ảnh cho thư GỬI ĐI: ảnh của server trong nội dung đổi sang cid: + các ảnh cid: khai ở
        /// Mail:InlineImages. Trả HTML đã đổi src và danh sách ảnh gửi kèm (MailAttachment có ContentId).
        /// </summary>
        public (string Html, List<MailAttachment> Attachments) Embed(string? html)
        {
            var attachments = new List<MailAttachment>();
            if (string.IsNullOrEmpty(html)) return (html ?? string.Empty, attachments);

            // Cùng một file dùng nhiều lần thì chỉ gửi kèm một lần
            var cidByPath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var result = ImgSrcPattern.Replace(html, m =>
            {
                var src = WebUtility.HtmlDecode(m.Groups[3].Value).Trim();
                if (src.StartsWith("cid:", StringComparison.OrdinalIgnoreCase) || src.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                    return m.Value;

                var fullPath = ResolveLocal(src);
                if (fullPath == null) return m.Value;   // ảnh máy chủ khác: giữ link

                if (!cidByPath.TryGetValue(fullPath, out var cid))
                {
                    var image = Read(fullPath);
                    if (image == null) return m.Value;

                    cid = ContentCidPrefix + (cidByPath.Count + 1);
                    cidByPath[fullPath] = cid;
                    attachments.Add(new MailAttachment(cid + image.Value.Ext, image.Value.Bytes)
                    {
                        ContentId = cid,
                        ContentType = image.Value.Mime,
                    });
                }

                return $"{m.Groups[1].Value}{m.Groups[2].Value}cid:{cid}{m.Groups[2].Value}";
            });

            // Ảnh cid: khai sẵn ở Mail:InlineImages (VD cid:logo)
            foreach (var id in FindIds(result).Where(x => !x.StartsWith(ContentCidPrefix, StringComparison.OrdinalIgnoreCase)))
            {
                var image = Load(id);
                if (image == null) continue;

                attachments.Add(new MailAttachment(id + image.Value.Ext, image.Value.Bytes)
                {
                    ContentId = id,
                    ContentType = image.Value.Mime,
                });
            }

            return (result, attachments);
        }

        /// <summary>
        /// Đổi cid:xxx thành data:...;base64 để trình duyệt hiển thị được — CHỈ dùng cho bản xem trước,
        /// không dùng cho thư gửi đi (Gmail / Outlook chặn ảnh data:).
        /// </summary>
        public string ToDataUri(string? html)
        {
            if (string.IsNullOrEmpty(html)) return html ?? string.Empty;

            return CidPattern.Replace(html, m =>
            {
                var image = Load(m.Groups[1].Value);
                return image == null ? m.Value : $"data:{image.Value.Mime};base64,{Convert.ToBase64String(image.Value.Bytes)}";
            });
        }

        private static IEnumerable<string> FindIds(string? html)
            => string.IsNullOrEmpty(html)
                ? Enumerable.Empty<string>()
                : CidPattern.Matches(html).Select(m => m.Groups[1].Value).Distinct(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// File ảnh trên server ứng với src (link http(s) hoặc đường dẫn tương đối). Tra theo phần ĐƯỜNG DẪN của link
        /// trong wwwroot, và trong Upload:RootPath nếu có cấu hình. Không phải file của server thì null.
        /// </summary>
        private string? ResolveLocal(string src)
        {
            string path;
            if (Uri.TryCreate(src, UriKind.Absolute, out var uri))
            {
                if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return null;
                path = uri.AbsolutePath;
            }
            else
            {
                path = src.Split('?', '#')[0];
            }

            path = Uri.UnescapeDataString(path).Replace('\\', '/').TrimStart('/');
            if (path.Length == 0) return null;

            var candidates = new List<(string Root, string Relative)> { (MailTemplateStore.WebRootOf(_environment), path) };

            var uploadRoot = _configuration["Upload:RootPath"];
            if (!string.IsNullOrWhiteSpace(uploadRoot))
            {
                candidates.Add((uploadRoot, path.StartsWith(SharePrefix, StringComparison.OrdinalIgnoreCase) ? path[SharePrefix.Length..] : path));
            }

            foreach (var (root, relative) in candidates)
            {
                var fullPath = InsideRoot(root, relative);
                if (fullPath != null && File.Exists(fullPath)) return fullPath;
            }

            return null;
        }

        /// <summary>Ảnh khai ở Mail:InlineImages theo tên cid.</summary>
        private (byte[] Bytes, string Mime, string Ext)? Load(string id)
        {
            if (!_settings.InlineImages.TryGetValue(id, out var relative) || string.IsNullOrWhiteSpace(relative))
            {
                _logger.LogWarning("Mẫu thư dùng ảnh cid:{Id} nhưng Mail:InlineImages chưa khai.", id);
                return null;
            }

            // Chặn cấu hình trỏ ra ngoài wwwroot (VD "../appsettings.json")
            var fullPath = InsideRoot(MailTemplateStore.WebRootOf(_environment), relative);
            if (fullPath == null)
            {
                _logger.LogWarning("Ảnh cid:{Id} trỏ ra ngoài wwwroot ({Path}), bỏ qua.", id, relative);
                return null;
            }

            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("Không tìm thấy ảnh cid:{Id} tại {Path}. Thư vẫn gửi nhưng thiếu ảnh.", id, fullPath);
                return null;
            }

            return Read(fullPath);
        }

        /// <summary>Đọc file ảnh (có cache). Không phải ảnh png / jpg / gif hoặc đọc lỗi thì null.</summary>
        private (byte[] Bytes, string Mime, string Ext)? Read(string fullPath)
        {
            try
            {
                var modified = File.GetLastWriteTimeUtc(fullPath);
                var bytes = Cache.TryGetValue(fullPath, out var cached) && cached.Modified == modified
                    ? cached.Bytes
                    : (Cache[fullPath] = (modified, File.ReadAllBytes(fullPath))).Bytes;

                var image = DetectImage(bytes);
                return image == null ? null : (bytes, image.Value.Mime, image.Value.Ext);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Đọc ảnh {Path} lỗi. Thư vẫn gửi nhưng thiếu ảnh.", fullPath);
                return null;
            }
        }

        /// <summary>Đường dẫn đầy đủ của relative nằm TRONG root; ra ngoài root thì null.</summary>
        private static string? InsideRoot(string root, string relative)
        {
            var rootFull = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var fullPath = Path.GetFullPath(Path.Combine(rootFull,
                relative.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar)));

            return fullPath.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase) ? fullPath : null;
        }

        /// <summary>
        /// Loại ảnh theo NỘI DUNG, không theo đuôi file: logo hiện tại tên .jpg nhưng thực chất là PNG
        /// trong suốt — khai sai kiểu thì một số hộp thư không hiện ảnh. Không phải ảnh thì null.
        /// </summary>
        private static (string Mime, string Ext)? DetectImage(byte[] b)
        {
            if (b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return ("image/png", ".png");
            if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return ("image/jpeg", ".jpg");
            if (b.Length >= 6 && b[0] == 0x47 && b[1] == 0x49 && b[2] == 0x46) return ("image/gif", ".gif");
            return null;
        }
    }
}
