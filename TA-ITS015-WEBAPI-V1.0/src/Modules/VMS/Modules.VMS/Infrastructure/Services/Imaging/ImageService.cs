using Microsoft.AspNetCore.Hosting;
using Shared.Core.Interfaces;
using System.Text.RegularExpressions;

namespace Modules.VMS.Infrastructure.Services.Imaging
{
    public class ImageService
    {
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly IUploadService _iUploadService;

        public ImageService(IWebHostEnvironment hostEnvironment, IUploadService iUploadService)
        {
            this._hostEnvironment = hostEnvironment;
            this._iUploadService = iUploadService;
        }

        /// <summary>
        /// Author: ThangNN , ChuongLP
        /// Description: Lưu file vào thư mục upload 
        /// ChuongLP : // Thêm tham số imgFolder, giữ default "upload\\vms\\image" để các nơi gọi cũ không phải đổi
        /// Created date: 01/12/2024 , 29/06/2026
        /// </summary>
        public async Task<string> SaveImageAsync(string? base64Image, string imgName, string imgFolder = "upload\\vms\\image")
        {
            if (string.IsNullOrEmpty(base64Image))
                return string.Empty;

            var dataGroups = Regex.Match(base64Image, "data:image/(?<type>.+?);base64,(?<data>.+)").Groups;
            string imgExt = dataGroups["type"].Value;
            string imgData = dataGroups["data"].Value;

            if (string.IsNullOrEmpty(imgExt) || !new string[] { "jpeg", "jpg", "png", "gif", "bmp", "svg", "webp" }.Contains(imgExt.ToLower()))
                return string.Empty;

            if (!Directory.Exists(this._hostEnvironment.WebRootPath))
                return string.Empty;

            try
            {
                // Try converting to a byte array to ensure this base64 data is valid
                byte[] imageBytes = Convert.FromBase64String(imgData);

                // BỎ dòng gán cứng cũ: string imgFolder = "upload\\vms\\image";
                string uploadFolder = Path.Combine(this._hostEnvironment.WebRootPath, imgFolder);
                if (!Directory.Exists(uploadFolder))
                    Directory.CreateDirectory(uploadFolder);

                string imgPath = await this._iUploadService.UploadAsync(new Shared.Core.Utilities.BaseUploadInput()
                {
                    Data = base64Image, // Not: imgData
                    FileName = imgName,
                    Extension = "." + imgExt,
                    FolderPath = imgFolder
                }, true);

                return imgPath;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Xóa file upload
        /// Created date: 01/12/2024
        /// </summary>
        public bool DeleteImage(string? imageUrl)
        {
            try
            {
                if(string.IsNullOrEmpty(imageUrl))
                    return false;

                string imagePath = Path.Combine(this._hostEnvironment.WebRootPath, imageUrl);

                if (File.Exists(imagePath))
                {
                    File.Delete(imagePath);

                    return !File.Exists(imagePath);
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Kiểm tra chuỗi có định dạng là base64
        /// Created date: 01/12/2024
        /// </summary>
        public bool IsBase64String(string? data)
        {
            try
            {
                if(string.IsNullOrEmpty(data))
                    return false;

                string base64Data = Regex.Replace(data, @"^data:image/[^;]+;base64,", string.Empty);
                if(base64Data.Length % 4 != 0 || !Regex.IsMatch(base64Data, @"^[a-zA-Z0-9\+/]*={0,2}$"))
                    return false;

                byte[] imageBytes = Convert.FromBase64String(base64Data);

                return imageBytes.Length > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
