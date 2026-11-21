namespace Shared.Utility.Helpers
{
    public class FileHelper
    {
        /// <summary>
        /// Helper lấy  kiểu mime của file tương ứng
        /// </summary>
        public class FileMimeHelper
        {
            /// <summary>
            /// Lấy mime file theo extension
            /// </summary>
            /// <param name="format"></param>
            /// <returns></returns>
            public static string GetByExtension(string format)
            {
                // Update 09/10/2026: chuẩn hoá đuôi (bỏ dấu chấm, chữ thường) — trước đây truyền ".pdf" luôn ra
                // application/octet-stream; bổ sung svg/webp/ico cho ảnh icon.
                switch ((format ?? string.Empty).Trim().TrimStart('.').ToLowerInvariant())
                {
                    case "pdf": return "application/pdf";
                    case "doc": return "application/msword";
                    case "dot": return "application/msword";
                    case "docx": return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                    case "dotx": return "application/vnd.openxmlformats-officedocument.wordprocessingml.template";
                    case "docm": return "application/vnd.ms-word.document.macroEnabled.12";
                    case "dotm": return "application/vnd.ms-word.template.macroEnabled.12";
                    case "xls": return "application/vnd.ms-excel";
                    case "xlt": return "application/vnd.ms-excel";
                    case "xla": return "application/vnd.ms-excel";
                    case "xlsx": return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    case "xltx": return "application/vnd.openxmlformats-officedocument.spreadsheetml.template";
                    case "xlsm": return "application/vnd.ms-excel.sheet.macroEnabled.12";
                    case "xltm": return "application/vnd.ms-excel.template.macroEnabled.12";
                    case "xlam": return "application/vnd.ms-excel.addin.macroEnabled.12";
                    case "xlsb": return "application/vnd.ms-excel.sheet.binary.macroEnabled.12";
                    case "ppt": return "application/vnd.ms-powerpoint";
                    case "pot": return "application/vnd.ms-powerpoint";
                    case "pps": return "application/vnd.ms-powerpoint";
                    case "ppa": return "application/vnd.ms-powerpoint";
                    case "pptx": return "application/vnd.openxmlformats-officedocument.presentationml.presentation";
                    case "potx": return "application/vnd.openxmlformats-officedocument.presentationml.template";
                    case "ppsx": return "application/vnd.openxmlformats-officedocument.presentationml.slideshow";
                    case "ppam": return "application/vnd.ms-powerpoint.addin.macroEnabled.12";
                    case "pptm": return "application/vnd.ms-powerpoint.presentation.macroEnabled.12";
                    case "potm": return "application/vnd.ms-powerpoint.template.macroEnabled.12";
                    case "ppsm": return "application/vnd.ms-powerpoint.slideshow.macroEnabled.12";
                    case "mdb": return "application/vnd.ms-access";
                    case "png": return "image/png";
                    case "jpg": return "image/jpeg";
                    case "jpeg": return "image/jpeg";
                    case "gif": return "image/gif";
                    case "bmp": return "image/bmp";
                    case "tif": return "image/tiff";
                    case "tiff": return "image/tiff";
                    case "svg": return "image/svg+xml";
                    case "webp": return "image/webp";
                    case "ico": return "image/x-icon";
                    default: return "application/octet-stream"; // Default MIME type for unknown files
                }
            }

            /// <summary>
            /// Đuôi file có phải ảnh không (dựa trên <see cref="GetByExtension"/>).
            /// Created date: 09/10/2026
            /// </summary>
            /// <param name="format">Đuôi file, có hoặc không có dấu chấm.</param>
            public static bool IsImage(string format)
            {
                return GetByExtension(format).StartsWith("image/", StringComparison.Ordinal);
            }

            /// <summary>
            /// Ghép đường dẫn tương đối (vd lưu trong DB) với thư mục gốc; false nếu đường dẫn thoát ra ngoài gốc
            /// (chứa "..", ".", ổ đĩa ":") — tránh đọc được file ngoài thư mục cho phép.
            /// Created date: 09/10/2026
            /// </summary>
            public static bool TryGetPathInRoot(string rootDirectory, string? relativePath, out string fullPath)
            {
                fullPath = string.Empty;
                if (string.IsNullOrWhiteSpace(rootDirectory) || string.IsNullOrWhiteSpace(relativePath))
                    return false;

                var path = relativePath.Trim().Replace('\\', '/').TrimStart('/');
                if (path.Length == 0 || path.Contains(':'))
                    return false;

                var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length == 0 || segments.Any(x => x == ".." || x == "."))
                    return false;

                var root = Path.GetFullPath(rootDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                           + Path.DirectorySeparatorChar;
                var combined = Path.GetFullPath(Path.Combine(root, Path.Combine(segments)));
                if (!combined.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                    return false;

                fullPath = combined;
                return true;
            }


            public static bool IsExist(string filePath)
            {
                return File.Exists(filePath);
            }

            public static string GetBase64(string filePath)
            {
                // Read the image file as a byte array
                byte[] imageBytes = File.ReadAllBytes(filePath);

                // Convert the byte array to a Base64 string
                return Convert.ToBase64String(imageBytes);
            }

            public static byte[] GetFileBytes(string filePath)
            {
                return File.ReadAllBytes(filePath);
            }

          
            /// <summary>
            /// Hàm hỗ trợ định dạng lại đường dẫn file theo đường dẫn lưu trữ
            /// vd /folder1/folder2/file
            /// </summary>
            /// <param name="path"></param>
            /// <returns></returns>
            public static string FormatUrlForStorage(string path)
            {
                if (string.IsNullOrWhiteSpace(path))
                    return "";

                // Normalize slashes to the system's directory separator
                string normalizedPath = path.Replace('/', Path.DirectorySeparatorChar)
                    .Replace('\\', Path.DirectorySeparatorChar);

                // If it's a UNC path, ensure it retains its leading double backslashes
                if (path.StartsWith(@"\\"))
                    normalizedPath = @"\\" + normalizedPath.TrimStart(Path.DirectorySeparatorChar);

                return Path.GetFullPath(normalizedPath);
            }

            /// <summary>
            /// Hàm hỗ trợ định dạng lại đường dẫn file theo đường dẫn web hiển thị
            /// vd \folder\folder2\file
            /// </summary>
            /// <param name="path"></param>
            /// <param name="baseUrl"></param>
            /// <returns></returns>
            public static string FormatUrlForWeb(string path, string? baseUrl = "")
            {
                if (string.IsNullOrWhiteSpace(path))
                    return "";

               
                // Ensure base URL ends with a slash
                if (!baseUrl.EndsWith("/"))
                    baseUrl += "/";

                // Convert path separators to web format
                string webPath = path.Replace("\\", "/");

                // Ensure no leading slashes to prevent double slashes in the final URL
                webPath = webPath.TrimStart('/');

                return $"{baseUrl}{webPath}";
            }

        }



        public static FileStream OpenReadWithRetry(string path, int attempts = 8, int baseDelayMs = 25)
        {
            for (int i = 0; ; i++)
            {
                try
                {
                    // Cho phép tiến trình khác vẫn đọc (chủ yếu AV)
                    return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                }
                catch (IOException) when (i < attempts - 1)
                {
                    Thread.Sleep(baseDelayMs * (1 << i)); // backoff: 25, 50, 100, 200, ...
                }
            }
        }

        public static FileStream CreateWriteWithRetry(string path, int attempts = 8, int baseDelayMs = 25)
        {
            for (int i = 0; ; i++)
            {
                try
                {
                    return new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                }
                catch (IOException) when (i < attempts - 1)
                {
                    Thread.Sleep(baseDelayMs * (1 << i));
                }
            }
        }

        public static void DeleteWithRetry(string path, int attempts = 8, int baseDelayMs = 25)
        {
            for (int i = 0; ; i++)
            {
                try
                {
                    if (File.Exists(path)) File.Delete(path);
                    return;
                }
                catch (IOException) when (i < attempts - 1)
                {
                    Thread.Sleep(baseDelayMs * (1 << i));
                }
            }
        }

        public static void MoveReplaceWithRetry(string srcTmp, string dst, int attempts = 8, int baseDelayMs = 25)
        {
            for (int i = 0; ; i++)
            {
                try
                {
                    if (File.Exists(dst)) DeleteWithRetry(dst, attempts, baseDelayMs);
                    File.Move(srcTmp, dst);
                    return;
                }
                catch (IOException) when (i < attempts - 1)
                {
                    Thread.Sleep(baseDelayMs * (1 << i));
                }
            }
        }

    }
}
