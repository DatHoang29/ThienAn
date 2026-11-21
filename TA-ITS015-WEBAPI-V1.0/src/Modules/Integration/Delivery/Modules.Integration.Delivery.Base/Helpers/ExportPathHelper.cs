namespace Modules.Integration.Delivery.Base.Helpers
{
    /// <summary>
    /// Description: Các hàm tiện ích thuần BCL phát sinh và chuẩn hóa đường dẫn tập tin xuất bản dữ liệu.
    /// Created date: 15/10/2026
    /// </summary>
    public static class ExportPathHelper
    {
        public const string DefaultJsonExtension = ".json";
        public const string UnknownConstant = "Unknown";
        public const string DefaultBasePath = "sharedata/send";
        public const string BasePathConfigKey = "FileStorage:BasePath";

        /// <summary>
        /// Description: Phát sinh đường dẫn tương đối lưu tập tin kết xuất: Out/[PartnerCode]/[yyyy]/[MM]/[dd]/[DatatypeId]/[FileName].[ext]
        /// </summary>
        public static string GenerateExportRelativePath(
            string partnerCode,
            string datatypeId,
            DateTime time,
            string? subDiscriminator = null,
            string extension = DefaultJsonExtension)
        {
            var ext = extension.TrimStart('.').ToLowerInvariant();
            var yyyy = time.ToString("yyyy");
            var MM = time.ToString("MM");
            var dd = time.ToString("dd");

            var invalidChars = Path.GetInvalidFileNameChars();
            var safePartnerCode = string.IsNullOrWhiteSpace(partnerCode)
                ? UnknownConstant
                : new string(partnerCode.Trim().Where(c => !invalidChars.Contains(c)).ToArray()).Replace("..", "");

            if (string.IsNullOrWhiteSpace(safePartnerCode))
                safePartnerCode = UnknownConstant;

            var discriminatorSuffix = string.IsNullOrWhiteSpace(subDiscriminator) ? "" : $"_{subDiscriminator}";
            var fileName = $"{datatypeId}_{time:yyyyMMddHHmmss}{discriminatorSuffix}";

            return $"Out/{safePartnerCode}/{yyyy}/{MM}/{dd}/{datatypeId}/{fileName}.{ext}";
        }

        /// <summary>
        /// Description: Lấy phần phân biệt gắn vào tên tập tin kết xuất (SerialNbr hoặc ID).
        /// </summary>
        public static string ResolveFileDiscriminator(long? serialNbr, string? id)
        {
            var subIdPrefix = !string.IsNullOrEmpty(id)
                ? id[..Math.Min(8, id.Length)]
                : "sub";

            if (serialNbr.HasValue)
                return $"{serialNbr.Value}_{subIdPrefix}";

            return id ?? Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// Description: Chọn tên thư mục gói tin cho đường dẫn kết xuất.
        /// </summary>
        public static string ResolveDatatypeFolderName(string? datatypeId)
        {
            if (string.IsNullOrWhiteSpace(datatypeId))
                return UnknownConstant;

            var invalidChars = Path.GetInvalidFileNameChars();
            var safeName = new string(datatypeId.Trim().Where(c => !invalidChars.Contains(c)).ToArray()).Replace("..", "");

            return string.IsNullOrWhiteSpace(safeName) ? UnknownConstant : safeName;
        }

        /// <summary>
        /// Description: Phân giải đường dẫn gốc cho thư mục xuất bản dữ liệu.
        ///              Đường dẫn tuyệt đối giữ nguyên; đường dẫn tương đối ghép với baseDirectory;
        ///              đường dẫn rỗng dùng mặc định <see cref="DefaultBasePath"/>.
        /// </summary>
        public static string ResolveRootPath(string? path, string? baseDirectory = null)
        {
            var targetPath = string.IsNullOrWhiteSpace(path) ? DefaultBasePath : path.Trim();
            if (Path.IsPathRooted(targetPath))
                return Path.GetFullPath(targetPath);

            string baseDir;
            if (!string.IsNullOrWhiteSpace(baseDirectory))
            {
                baseDir = baseDirectory;
            }
            else
            {
                var currentDir = Directory.GetCurrentDirectory();
                var sep = Path.DirectorySeparatorChar;
                var altSep = Path.AltDirectorySeparatorChar;

                // 1. Khi chạy dev tại thư mục mã nguồn (có .csproj hoặc appsettings ngoài thư mục bin), ưu tiên ghi tại thư mục source
                var isUnderBin = currentDir.Contains($"{sep}bin{sep}") || currentDir.Contains($"{altSep}bin{altSep}");
                if (!string.IsNullOrWhiteSpace(currentDir) &&
                    (File.Exists(Path.Combine(currentDir, "ShareDataWorker.csproj")) ||
                     (File.Exists(Path.Combine(currentDir, "appsettings.json")) && !isUnderBin)))
                {
                    baseDir = currentDir;
                }
                // 2. Khi chạy debug mà CWD là thư mục project, tránh ghi sâu vào bin/Debug hoặc bin/Release
                else if ((AppContext.BaseDirectory.Contains($"{sep}bin{sep}") || AppContext.BaseDirectory.Contains($"{altSep}bin{altSep}")) &&
                         !string.IsNullOrWhiteSpace(currentDir) &&
                         !currentDir.Equals(Environment.SystemDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    baseDir = currentDir;
                }
                else
                {
                    // 3. Khi chạy dưới dạng Windows Service (CWD = System32), trỏ về thư mục cài đặt chứa file thực thi của service
                    baseDir = AppContext.BaseDirectory;
                }
            }

            return Path.GetFullPath(Path.Combine(baseDir, targetPath));
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Lưu tập tin kết xuất vào đường dẫn tuyệt đối hoặc tương đối dưới thư mục gốc chỉ định.
        /// Created date: 16/10/2026
        /// </summary>
        public static async Task<string> SaveFile(
            string basePath,
            string relativeOrFullPath,
            byte[] contentBytes,
            CancellationToken cancellationToken = default)
        {
            var root = ResolveRootPath(basePath);
            var fullPath = Path.IsPathRooted(relativeOrFullPath)
                ? Path.GetFullPath(relativeOrFullPath)
                : Path.GetFullPath(Path.Combine(root, relativeOrFullPath));

            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
            }

            await File.WriteAllBytesAsync(fullPath, contentBytes, cancellationToken);
            return fullPath;
        }
    }
}
