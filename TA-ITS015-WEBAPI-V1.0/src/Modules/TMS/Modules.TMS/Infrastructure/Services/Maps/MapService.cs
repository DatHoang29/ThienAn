using Microsoft.AspNetCore.Hosting;
using Modules.TMS.Controllers.Map.Dto;
using Modules.TMS.Controllers.MapLocations.Dto;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Modules.TMS.Infrastructure.Services.Maps
{
    public class MapService : IWolverineHandler
    {
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IMessageBus _messageBus;

        public MapService(IWebHostEnvironment hostingEnvironment, IMessageBus messageBus)
        {
            _hostingEnvironment = hostingEnvironment;
            _messageBus = messageBus;
        }

        public async Task<string> SaveImageAsync(string base64Image)
        {
            // Kiểm tra xem base64Image có phải là base64 hợp lệ không
            if (IsBase64String(base64Image))
            {
                // Xóa tiền tố "data:image/jpeg;base64," hoặc "data:image/png;base64," nếu có
                string base64Data = Regex.Replace(base64Image, @"^data:image/[^;]+;base64,", "");

                // Chuyển đổi base64 thành byte[]
                byte[] imageBytes = Convert.FromBase64String(base64Data);

                // Lấy đường dẫn thực tế đến thư mục wwwroot/Upload/Map/ImageMap
                string uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "Upload", "Map", "ImageMap");

                // Tạo thư mục nếu chưa tồn tại
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Tạo tên tệp hình ảnh duy nhất
                string fileName = Guid.NewGuid().ToString() + ".jpg"; // Hoặc .png tùy theo định dạng hình ảnh
                string filePath = Path.Combine(uploadsFolder, fileName);

                // Lưu hình ảnh vào đĩa
                await File.WriteAllBytesAsync(filePath, imageBytes);

                // Trả về đường dẫn đầy đủ của hình ảnh (nếu cần)
                // return filePath;

                // Trả về đường dẫn tương đối từ gốc wwwroot
                string relativePath = Path.Combine("Upload", "Map", "ImageMap", fileName).Replace("\\", "/");
                return relativePath;
            }
            else
            {
                // Nếu không phải base64 hợp lệ, trả về chính chuỗi đầu vào
                return base64Image;
            }
        }

        // Phương thức kiểm tra chuỗi có phải là base64 hợp lệ không
        private bool IsBase64String(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            // Xóa tiền tố nếu có
            string base64Data = Regex.Replace(input, @"^data:image/[^;]+;base64,", "").Trim();

            // Kiểm tra độ dài và các ký tự hợp lệ cho base64
            if (base64Data.Length % 4 != 0 || !Regex.IsMatch(base64Data, @"^[a-zA-Z0-9\+/]*={0,2}$"))
            {
                return false;
            }

            try
            {
                // Kiểm tra xem có phải là base64 hợp lệ không bằng cách thử chuyển đổi
                Convert.FromBase64String(base64Data);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Copy file cấu hình vị trí của bản đồ cũ sang bản đồ mới
        /// Author: TuanHA
        /// Date created: 03/04/2025
        /// </summary>
        /// <param name="command">Dữ liệu truyền vào lúc tạo bản đồ</param>
        /// <param name="newMapId">ID của bản đồ mới</param>
        /// <returns></returns>
        public async Task CopyFileLocationConfig(AddMapInput command, string newMapId)
        {
            // Get tmsMaplocation based on MapId
            MapLocationBasedOnMapIdInput mapLocationBasedOnMapIdInput = new MapLocationBasedOnMapIdInput();
            mapLocationBasedOnMapIdInput.MapId = command.OldMapId;
            var tmsMaplocation = await _messageBus.InvokeAsync<MapLocationsOutput>(mapLocationBasedOnMapIdInput);
            if (tmsMaplocation != null)
            {
                var sourceFilePath = Path.Combine(_hostingEnvironment.WebRootPath, tmsMaplocation.GeoJson);

                // Check file exists then copy file and create new TmsMapLocation
                if (File.Exists(sourceFilePath))
                {
                    try
                    {
                        // Get the original filename and extension
                        string fileExtension = Path.GetExtension(sourceFilePath);

                        // Create a new filename with a timestamp
                        string newFileName = newMapId + fileExtension;

                        // Check destinationDirectory
                        var destinationDirectory = Path.Combine(_hostingEnvironment.WebRootPath, "MapLocation");

                        // Combine the destination directory and the new filename
                        string destinationFilePath = Path.Combine(destinationDirectory, newFileName);

                        // Copy the file
                        File.Copy(sourceFilePath, destinationFilePath);

                        // Add new TmsMapLocation
                        AddTmsMapLocationInput addTmsMapLocationInput = new AddTmsMapLocationInput();
                        addTmsMapLocationInput.MapId = newMapId;
                        addTmsMapLocationInput.Name = newMapId;
                        addTmsMapLocationInput.Config = tmsMaplocation.Config;
                        addTmsMapLocationInput.Status = tmsMaplocation.Status;
                        addTmsMapLocationInput.GeoJson = "MapLocation" + "\\" + newFileName;
                        await _messageBus.InvokeAsync(addTmsMapLocationInput);

                        Debug.WriteLine($"File '{sourceFilePath}' exists. Copied to '{destinationFilePath}'");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error copying file: {ex.Message}");
                    }
                }
            }
        }
    }

}
