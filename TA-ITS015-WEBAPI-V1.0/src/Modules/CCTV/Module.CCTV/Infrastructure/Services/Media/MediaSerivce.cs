using Microsoft.AspNetCore.Hosting;
using Shared.Core.Interfaces;
using Shared.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Module.CCTV.Infrastructure.Services.Media
{
    public class MediaSerivce
    {
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly IUploadService _iUploadService;

        public MediaSerivce(IWebHostEnvironment hostEnvironment,IUploadService iUploadService)
        {
            _hostEnvironment = hostEnvironment;
            _iUploadService = iUploadService;
        }

        /// <summary>
        /// Lưu snapshot mới nhất của thiết bị.
        /// Mỗi device chỉ giữ 1 ảnh duy nhất: wwwroot/CCTV/snapshot/{deviceId}.jpg
        /// Nếu file đã tồn tại thì ghi đè.
        /// </summary>
        public async Task<string> SaveSnapshotAsync(byte[]? imageBytes,string deviceId,CancellationToken cancellationToken = default)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return string.Empty;

            if (string.IsNullOrWhiteSpace(deviceId))
                return string.Empty;

            if (string.IsNullOrWhiteSpace(_hostEnvironment.WebRootPath))
                return string.Empty;

            try
            {
                string imgFolder = Path.Combine("CCTV", "snapshot");
                string uploadFolder = Path.Combine(_hostEnvironment.WebRootPath, imgFolder);

                if (!Directory.Exists(uploadFolder))
                    Directory.CreateDirectory(uploadFolder);

                string safeDeviceId = SafeFileName(deviceId);
                string imgName = safeDeviceId;
                string imgExt = "jpg";

                string base64Image = $"data:image/{imgExt};base64,{Convert.ToBase64String(imageBytes)}";

                string imgPath = await _iUploadService.UploadAsync(
                    new BaseUploadInput
                    {
                        Data = base64Image,
                        FileName = imgName,
                        Extension = "." + imgExt,
                        FolderPath = imgFolder
                    },
                    true
                );

                return imgPath;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Hàm xóa hình ảnh trong folder khi xóa thiết bị
        /// Author: ChuongLP
        /// Date: 2026-06-21
        /// </summary>
        public void DeleteSnapshot(string? snapshotUrl)
        {
            if (string.IsNullOrWhiteSpace(snapshotUrl))
                return;

            if (string.IsNullOrWhiteSpace(_hostEnvironment.WebRootPath))
                return;

            try
            {
                var relativePath = snapshotUrl
                    .Replace("/", Path.DirectorySeparatorChar.ToString())
                    .TrimStart(Path.DirectorySeparatorChar);

                var filePath = Path.Combine(_hostEnvironment.WebRootPath, relativePath);

                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
            catch
            {
                // Không throw để tránh lỗi xóa ảnh làm fail sync.
            }
        }

        private static string SafeFileName(string value)
        {
            return Regex.Replace(value, @"[^a-zA-Z0-9_\-]", "_");
        }
    }
}
