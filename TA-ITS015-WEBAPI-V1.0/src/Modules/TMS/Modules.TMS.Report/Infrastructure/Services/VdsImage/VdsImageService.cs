using Furion.DependencyInjection;
using Furion.FriendlyException;
using Microsoft.Extensions.Configuration;
using Shared.DTO.Dtos.System;
using Modules.TMS.Report.Core.Exceptions;
using Shared.Utility.Apis.System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Modules.TMS.Report.Infrastructure.Services.VdsImage
{
    /// <summary>Nhánh thư mục của media — phải khớp <c>VdsMediaStorage.DataFolder</c>/<c>EventFolder</c>.</summary>
    public static class VdsMediaBranch
    {
        /// <summary>Ảnh của lượt xe bình thường (loại bản tin <c>Normal</c>).</summary>
        public const string Data = "data";

        /// <summary>Ảnh của 14 loại sự cố.</summary>
        public const string Event = "event";
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Đọc và giảm chất lượng ảnh VDS từ ổ đĩa.
    /// Created date: 08/08/2026
    ///
    /// <para><b>ĐÂY LÀ BÊN ĐỌC CỦA MỘT HỢP ĐỒNG HAI CHIỀU.</b> Không có bảng media: worker
    /// (<c>Services.Vds/Features/Media/VdsMediaStorage.BuildRelativePath</c>) ghi file theo đường dẫn TẤT
    /// ĐỊNH, bên đọc dựng lại đúng đường dẫn đó từ dữ liệu bản ghi:</para>
    /// <code>
    /// {gốc}/{data|event}/{yyyyMM}/{ddHH}/{cameraIp}/{yyyyMMddHHmmssfff}_{suffix}{ext}
    /// </code>
    ///
    /// <para><b>Sửa 04/09/2026 — trước đó hợp đồng bị VỠ ở bốn điểm, ảnh không bao giờ tìm thấy:</b></para>
    /// <list type="number">
    /// <item><b>Lệch 7 giờ</b>: bên đọc convert <c>detectTime</c> sang giờ local rồi mới dựng
    /// <c>yyyyMM/ddHH</c>, còn worker ghi theo <c>detectTime</c> là <b>UTC</b>.</item>
    /// <item><b>Thiếu nhánh <c>data|event</c></b>: bên đọc dùng hai thư mục gốc riêng
    /// (<c>Vds:DataImageDirectory</c> / <c>Vds:EventImageDirectory</c>) trong khi worker dùng MỘT gốc rồi
    /// tách nhánh bên dưới.</item>
    /// <item><b>Thiếu tầng <c>{cameraIp}</c></b> (worker thêm 28/08/2026).</item>
    /// <item><b>Suffix sai</b>: <c>pictureType = "p"</c> (ảnh biển số) bị đổi thành suffix <c>v</c>
    /// (ảnh phương tiện) ⇒ đọc sang file khác.</item>
    /// </list>
    ///
    /// <para><b>Thư mục gốc nằm trong CẤU HÌNH VẬN HÀNH</b> (<c>SysOpConfig</c>, khoá
    /// <c>vds_media_storage_directory</c>) — cùng khoá mà worker đọc, nên hai bên không thể trôi lệch. Không
    /// khai thì lùi về <c>IConfiguration</c> rồi tới giá trị mặc định của bên gọi.</para>
    ///
    /// <para><b>Camera: bên đọc KHÔNG biết <c>cameraIp</c></b> (bản ghi FE gửi lên chỉ có ID + thời gian),
    /// nên nó DÒ các thư mục camera trong tầng giờ. Tầng giờ chỉ có cỡ mười thư mục nên phép dò rẻ, và cách
    /// này giữ được API hiện tại — không buộc FE gửi thêm tham số. Có kèm đường lùi về bố cục CŨ (không có
    /// tầng camera) để ảnh đã lưu trước 28/08/2026 vẫn đọc được.</para>
    /// </summary>
    public sealed class VdsImageService : IScoped
    {
        /// <summary>Khoá <c>SysOpConfig</c> chứa thư mục gốc lưu media — phải khớp worker.</summary>
        public const string StorageDirectoryOpConfigCode = "vds_media_storage_directory";

        /// <summary>Thư mục dùng khi bản tin chưa tra được camera — khớp <c>VdsMediaStorage.UnknownCameraFolder</c>.</summary>
        private const string UnknownCameraFolder = "unknown";

        private readonly IConfiguration _configuration;
        private readonly BaseRepository<Modules.VDS.Core.Entities.VdsConfig> _vdsConfigRep;

        public VdsImageService(IConfiguration configuration, BaseRepository<Modules.VDS.Core.Entities.VdsConfig> vdsConfigRep)
        {
            _configuration = configuration;
            _vdsConfigRep = vdsConfigRep;
        }

        /// <summary>
        /// Đọc ảnh VDS theo thời gian, ID và loại ảnh; sau đó mã hóa lại JPEG theo chất lượng yêu cầu.
        /// </summary>
        /// <param name="mediaBranch">
        /// Nhánh media: <see cref="VdsMediaBranch.Data"/> cho lượt xe thường, <see cref="VdsMediaBranch.Event"/>
        /// cho sự cố. Bên gọi biết mình đang đọc bảng nào nên truyền vào, thay vì suy từ thư mục gốc như bản cũ.
        /// </param>
        /// <param name="configurationKey">Khoá <c>IConfiguration</c> dùng khi <c>SysOpConfig</c> chưa khai.</param>
        /// <param name="defaultImageDirectory">Giá trị cuối cùng khi cả hai nguồn trên đều trống.</param>
        /// <param name="detectTime">Thời điểm phát hiện, <b>UTC</b> — đúng cột <c>detectTime</c> worker ghi.</param>
        public async Task<(string Base64, string PictureType, string Url)> ReadAsync(
            string mediaBranch,
            string configurationKey,
            string defaultImageDirectory,
            string id,
            DateTime? detectTime,
            string pictureType,
            int quality)
        {
            var normalizedPictureType = pictureType?.Trim().ToLowerInvariant() ?? string.Empty;
            Validate(id, detectTime, normalizedPictureType, quality);

            // detectTime trên QuestDB là UTC và worker dựng đường dẫn TỪ CHÍNH giá trị đó. Bên đọc phải giữ
            // nguyên UTC; convert sang giờ local là lệch 7 tiếng ⇒ sai cả thư mục ngày/giờ lẫn tên file.
            var utcDetectTime = DateTime.SpecifyKind(detectTime!.Value, DateTimeKind.Utc);

            // 'f' = full (toàn cảnh), 'p' = plate (biển số) — GIỮ NGUYÊN, đây chính là suffix worker dùng.
            var outputPictureType = normalizedPictureType;

            var root = await ResolveStorageRootAsync(configurationKey, defaultImageDirectory);
            var hourDirectory = Path.Combine(
                root,
                NormalizeBranch(mediaBranch),
                utcDetectTime.ToString("yyyyMM"),
                utcDetectTime.ToString("ddHH"));

            var fileName = $"{utcDetectTime:yyyyMMddHHmmssfff}_{outputPictureType}.jpeg";
            var filePath = FindFile(hourDirectory, fileName);

            if (filePath is null)
            {
                // Trả về đường dẫn KỲ VỌNG (kể cả khi không thấy file) để bên gọi log/đối chiếu được — giữ
                // đúng hành vi cũ, chỉ khác là đường dẫn nay đã đúng quy ước.
                var expected = Path.Combine(hourDirectory, UnknownCameraFolder, fileName);
                return (string.Empty, outputPictureType, expected);
            }

            var originalBytes = await File.ReadAllBytesAsync(filePath);
            var outputBytes = quality == 100
                ? originalBytes
                : EncodeJpeg(originalBytes, quality);

            return (Convert.ToBase64String(outputBytes), outputPictureType, filePath);
        }

        /// <summary>
        /// Tìm file trong tầng giờ. Thứ tự dò có chủ ý:
        /// <list type="number">
        /// <item>Bố cục HIỆN HÀNH: <c>{giờ}/{cameraIp}/{tên file}</c> — dò từng thư mục camera con.</item>
        /// <item>Bố cục CŨ (trước 28/08/2026): <c>{giờ}/{tên file}</c> — để ảnh lịch sử vẫn đọc được.</item>
        /// </list>
        /// Trả <c>null</c> khi không có file nào khớp.
        /// </summary>
        private static string? FindFile(string hourDirectory, string fileName)
        {
            if (!Directory.Exists(hourDirectory)) return null;

            // Bố cục hiện hành: tầng camera. EnumerateDirectories chỉ đọc một cấp nên rẻ.
            string? found = null;
            foreach (var cameraDirectory in Directory.EnumerateDirectories(hourDirectory))
            {
                var candidate = Path.Combine(cameraDirectory, fileName);
                if (!File.Exists(candidate)) continue;
                if (found != null) throw Oops.Oh(BaseMsg.RptVdsImage.Exception.MultipleCameraMatch);
                found = candidate;
            }

            if (found != null) return found;
            // Bố cục cũ: file nằm thẳng trong tầng giờ.
            var legacy = Path.Combine(hourDirectory, fileName);
            return File.Exists(legacy) ? legacy : null;
        }

        /// <summary>Nhánh media đã chuẩn hoá; giá trị lạ ⇒ nhánh sự cố (khớp <c>VdsMediaStorage.FolderOf</c>).</summary>
        private static string NormalizeBranch(string? mediaBranch)
            => string.Equals(mediaBranch?.Trim(), VdsMediaBranch.Data, StringComparison.OrdinalIgnoreCase)
                ? VdsMediaBranch.Data
                : VdsMediaBranch.Event;

        private static void Validate(
            string id,
            DateTime? detectTime,
            string pictureType,
            int quality)
        {
            if (string.IsNullOrWhiteSpace(id)
                || id.Any(character => !char.IsLetterOrDigit(character) && character != '_' && character != '-'))
            {
                throw Oops.Oh(BaseMsg.RptVdsImage.Exception.InvalidId);
            }

            if (detectTime == null)
                throw Oops.Oh(BaseMsg.RptVdsImage.Exception.DetectTimeRequired);

            if (pictureType is not ("f" or "p" or "v" or "vi" or "sn"))
                throw Oops.Oh(BaseMsg.RptVdsImage.Exception.PictureTypeInvalid);

            if (quality is < 1 or > 100)
                throw Oops.Oh(BaseMsg.RptVdsImage.Exception.QualityRange, 1, 100);
        }

        /// <summary>
        /// Thư mục gốc lưu media, theo thứ tự ưu tiên:
        /// <list type="number">
        /// <item><c>SysOpConfig</c> khoá <see cref="StorageDirectoryOpConfigCode"/> — <b>nguồn chính</b>, cùng
        /// khoá worker đọc.</item>
        /// <item><c>IConfiguration[configurationKey]</c> — fallback khi chưa khai trên web.</item>
        /// <item><paramref name="defaultImageDirectory"/> — giá trị cuối.</item>
        /// </list>
        /// Lỗi gọi API cấu hình KHÔNG làm chết việc đọc ảnh: bắt và lùi về fallback.
        /// </summary>
        private async Task<string> ResolveStorageRootAsync(string configurationKey, string defaultImageDirectory)
        {
            var fromOpConfig = await TryReadOpConfigDirectoryAsync();

            var configured = !string.IsNullOrWhiteSpace(fromOpConfig)
                ? fromOpConfig!
                : _configuration[configurationKey];

            var directory = string.IsNullOrWhiteSpace(configured)
                ? defaultImageDirectory
                : configured.Trim();

            return Path.IsPathRooted(directory)
                ? Path.GetFullPath(directory)
                : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, directory));
        }

        /// <summary>Đọc thư mục gốc từ <c>SysOpConfig</c>; lỗi hoặc chưa khai ⇒ null.</summary>
        private async Task<string?> TryReadOpConfigDirectoryAsync()
        {
            try
            {
                var setting = await _vdsConfigRep.AsQueryable().Where(x => x.IsDelete == null
                    && x.Status == Shared.DTO.Enums.BaseEnums.StatusEnum.Enable
                    && x.GroupCode == "vds_config" && x.Code == StorageDirectoryOpConfigCode).FirstAsync();
                return setting?.Value?.Trim();
            }
            catch
            {
                // Cấu hình vận hành không đọc được thì vẫn phải đọc được ảnh theo fallback.
                return null;
            }
        }

        private static byte[] EncodeJpeg(byte[] imageBytes, int quality)
        {
            using var inputStream = new MemoryStream(imageBytes);
            using var image = Image.FromStream(inputStream);
            using var outputStream = new MemoryStream();
            using var encoderParameters = new EncoderParameters(1);
            using var qualityParameter = new EncoderParameter(
                global::System.Drawing.Imaging.Encoder.Quality,
                (long)quality);

            var jpegEncoder = ImageCodecInfo.GetImageEncoders()
                .FirstOrDefault(codec => codec.FormatID == ImageFormat.Jpeg.Guid)
                ?? throw Oops.Oh(BaseMsg.RptVdsImage.Exception.JpegEncoderNotFound);

            encoderParameters.Param[0] = qualityParameter;
            image.Save(outputStream, jpegEncoder, encoderParameters);
            return outputStream.ToArray();
        }
    }
}
