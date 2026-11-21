using System.Collections.Concurrent;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Shared.DTO.Upload;

namespace Modules.Mail.Infrastructure.Services.Mail.Templates
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Đọc / ghi FILE nội dung email (HTML soạn bằng wangEditor). DB chỉ lưu đường dẫn tính từ wwwroot
    ///              Đường dẫn chỉ được nằm trong wwwroot/Upload/Mail và là file .html — chặn đường dẫn lạ kiểu "../"
    ///              lọt từ DB ra ổ đĩa. Nội dung cache theo thời điểm sửa file.
    /// Created date: 29/09/2026
    /// </summary>
    public class MailTemplateStore : IScoped
    {
        private const string Ext = ".html";

        /// <summary>Thư mục gốc của mọi file nội dung email, tính từ wwwroot.</summary>
        private const string MailRoot = "Upload\\Mail";

        /// <summary>Cache nội dung: khoá = đường dẫn đầy đủ, kèm thời điểm sửa file để biết file đã đổi.</summary>
        private static readonly ConcurrentDictionary<string, (DateTime Modified, string Html)> Cache = new(StringComparer.OrdinalIgnoreCase);

        private readonly IWebHostEnvironment _environment;

        public MailTemplateStore(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        /// <summary>wwwroot của API; chưa có (lúc khởi động) thì lấy thư mục wwwroot cạnh DLL. Dùng chung với ảnh nội tuyến.</summary>
        public static string WebRootOf(IWebHostEnvironment? environment)
            => string.IsNullOrWhiteSpace(environment?.WebRootPath)
                ? Path.Combine(AppContext.BaseDirectory, "wwwroot")
                : environment!.WebRootPath;

        /// <summary>Nội dung HTML theo đường dẫn lưu ở DB. Đường dẫn rỗng / sai / thiếu file thì trả null.</summary>
        public string? Read(string? relativePath)
        {
            var full = FullPathOf(relativePath, MailRoot);
            if (full == null) return null;

            try
            {
                var file = new FileInfo(full);
                if (!file.Exists) return null;

                var modified = file.LastWriteTimeUtc;
                if (Cache.TryGetValue(full, out var cached) && cached.Modified == modified)
                    return cached.Html;

                var html = File.ReadAllText(full, Encoding.UTF8);
                Cache[full] = (modified, html);
                return html;
            }
            catch (IOException)
            {
                // File đang bị ghi đè — dùng bản cache cũ nếu có
                return Cache.TryGetValue(full, out var old) ? old.Html : null;
            }
        }

        /// <summary>Ghi nội dung mẫu ra Upload/Mail/Template/{id}.html. Trả đường dẫn để lưu DB.</summary>
        public Task<string> WriteTemplateAsync(string id, string html)
            => WriteAsync(Join(BaseUploadConfig.Mail.Template, id + Ext), html);

        /// <summary>
        /// Ghi nội dung của lịch. Lịch đã có file (currentPath hợp lệ) thì ghi đè đúng file đó; chưa có thì tạo
        /// Upload/Mail/ScheduleTemplate/yyyy/MM/dd/{id}.html theo ngày tạo lịch. Trả đường dẫn để lưu DB.
        /// </summary>
        public Task<string> WriteScheduleAsync(string id, DateTime createdAt, string html, string? currentPath)
        {
            var relative = FullPathOf(currentPath, BaseUploadConfig.Mail.ScheduleTemplate) != null
                ? Normalize(currentPath!)
                : Join(BaseUploadConfig.Mail.ScheduleTemplate,
                    createdAt.ToString("yyyy"), createdAt.ToString("MM"), createdAt.ToString("dd"), id + Ext);

            return WriteAsync(relative, html);
        }

        private async Task<string> WriteAsync(string relative, string html)
        {
            var full = FullPathOf(relative, MailRoot)
                ?? throw new InvalidOperationException($"Đường dẫn file nội dung email không hợp lệ: {relative}");

            Directory.CreateDirectory(Path.GetDirectoryName(full)!);

            // Ghi ra file tạm rồi thay: job gửi đang đọc không bao giờ gặp file ghi dở
            var temp = full + ".tmp";
            await File.WriteAllTextAsync(temp, html, new UTF8Encoding(false));
            File.Move(temp, full, overwrite: true);

            Cache.TryRemove(full, out _);
            return relative;
        }

        /// <summary>Đường dẫn lưu DB: ngăn bằng "/", không có "/" ở đầu.</summary>
        private static string Join(params string[] parts)
            => string.Join('/', parts.Select(Normalize).Where(x => x.Length > 0));

        private static string Normalize(string path) => path.Replace('\\', '/').Trim('/');

        /// <summary>Đường dẫn đầy đủ của file .html nằm TRONG wwwroot/{root}; ngoài phạm vi đó thì null.</summary>
        private string? FullPathOf(string? relativePath, string root)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return null;

            var webRoot = Path.GetFullPath(WebRootOf(_environment));
            var allowed = Path.GetFullPath(Path.Combine(webRoot, ToOsPath(root))).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var full = Path.GetFullPath(Path.Combine(webRoot, ToOsPath(Normalize(relativePath))));

            return full.StartsWith(allowed, StringComparison.OrdinalIgnoreCase)
                && full.EndsWith(Ext, StringComparison.OrdinalIgnoreCase)
                ? full
                : null;
        }

        private static string ToOsPath(string path)
            => path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
    }
}
