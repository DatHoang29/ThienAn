using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Modules.DataTransporter.ITS.Infrastructure.Services;
using Modules.VMS.Controllers.Current.Dto;
using Modules.VMS.Core.Entities;
using Modules.VMS.Infrastructure.Services.Process;
using Shared.Core.Interfaces;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Modules.VMS.Infrastructure.Services.Current
{
    public class VmsCurrentService
    {
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly IUploadService _iUploadService;
        private readonly BaseRepository<VmsCurrent> _vmsCurrentRepository;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private VmsProcessService _VmsProcessService;


		public VmsCurrentService(IWebHostEnvironment hostEnvironment, IUploadService iUploadService, BaseRepository<VmsCurrent> vmsCurrentRepository,
									IHttpContextAccessor httpContextAccessor, VmsProcessService vmsProcessService)
        {
            _hostEnvironment = hostEnvironment;
            _iUploadService = iUploadService;
            _vmsCurrentRepository = vmsCurrentRepository;
			_httpContextAccessor = httpContextAccessor;
			_VmsProcessService = vmsProcessService;
		}
        ///// Author: KhoaND
        ///// Description:
        ///// Lưu trữ hình ảnh
        ///// Created date: 08/11/2024
        //public async Task<string> SaveImageAsync(string? base64Image, string imgName)
        //{
        //    if (string.IsNullOrEmpty(base64Image)) return string.Empty;

        //    var dataGroups = Regex.Match(base64Image, "data:image/(?<type>.+?);base64,(?<data>.+)").Groups;
        //    string imgExt = dataGroups["type"].Value;
        //    string imgData = dataGroups["data"].Value;

        //    if (string.IsNullOrEmpty(imgExt) || !new[] { "jpeg", "jpg", "png", "gif", "bmp", "svg", "webp" }.Contains(imgExt.ToLower()))
        //        return string.Empty;

        //    if (!Directory.Exists(_hostEnvironment.WebRootPath)) return string.Empty;

        //    try
        //    {
        //        byte[] imageBytes = Convert.FromBase64String(imgData);
        //        string imgFolder = Path.Combine("upload", "vms", "template", "preview");
        //        string uploadFolder = Path.Combine(_hostEnvironment.WebRootPath, imgFolder);
        //        Directory.CreateDirectory(uploadFolder);

        //        string imgPath = await _iUploadService.UploadAsync(new Shared.Core.Utilities.BaseUploadInput
        //        {
        //            Data = base64Image,
        //            FileName = imgName,
        //            Extension = "." + imgExt,
        //            FolderPath = imgFolder
        //        }, true);

        //        return imgPath;
        //    }
        //    catch
        //    {
        //        return string.Empty;
        //    }
        //}
        ///// Author: KhoaND
        ///// Description:
        ///// Xóa hình ảnh
        ///// Created date: 08/11/2024
        //public bool DeleteImage(string? imageUrl)
        //{
        //    try
        //    {
        //        if (string.IsNullOrEmpty(imageUrl)) return false;
        //        string imagePath = Path.Combine(_hostEnvironment.WebRootPath, imageUrl);

        //        if (File.Exists(imagePath))
        //        {
        //            File.Delete(imagePath);
        //            return !File.Exists(imagePath);
        //        }

        //        return false;
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}
        ///// Author: KhoaND
        ///// Description:
        ///// kiểm tra base64
        ///// Created date: 08/11/2024
        //public bool IsBase64String(string? data)
        //{
        //    try
        //    {
        //        if (string.IsNullOrEmpty(data)) return false;

        //        string base64Data = Regex.Replace(data, @"^data:image/[^;]+;base64,", string.Empty);
        //        if (base64Data.Length % 4 != 0 || !Regex.IsMatch(base64Data, @"^[a-zA-Z0-9\+/]*={0,2}$"))
        //            return false;

        //        byte[] imageBytes = Convert.FromBase64String(base64Data);
        //        return imageBytes.Length > 0;
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}
        /// Author: KhoaND
        /// Description:
        /// Render ra hình ảnh 
        /// Created date: 08/11/2024
        public string WriteTextToImage(AddVmsCurrentInput item, string backColor = "Black", int brightness = 100)
        {
            string imageBackgroundPath = Path.Combine("vms", "Background", "384x64.PNG");

            using var imgBackground = Image.FromFile(Path.Combine(_hostEnvironment.WebRootPath, imageBackgroundPath));
            using var bitmap = new Bitmap(imgBackground);
            using var graphics = Graphics.FromImage(bitmap);

            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

            var backgroundColor = GetColorFromString(backColor);
            graphics.Clear(backgroundColor);

            var date = DateTime.Now;
            string imageFileName = $"{item.Code}_{date:yyyyMMdd_HHmmss}.png";
            string subfolder = Path.Combine($"{date:yyyyMM}", $"{date:dd}");
            string imageDirectory = Path.Combine(_hostEnvironment.WebRootPath, "upload", "vms", "template", "preview", subfolder);
            Directory.CreateDirectory(imageDirectory);
            string imageFilePath = Path.Combine(imageDirectory, imageFileName);
            string url = Path.Combine("upload", "vms", "template", "preview", subfolder, imageFileName);
            var textColor = ConvertStringToBrush(item.Color);
            var rowData = JsonConvert.DeserializeObject<Dictionary<string, string>>(item.RowData);
            var fontSize = int.TryParse(item.FontSize, out int size) && size > 0 ? size : 13;
            var fontFamily = string.IsNullOrEmpty(item.Font) ? "Arial" : item.Font;
            var font = new Font(fontFamily, fontSize);

            string[] listString = { rowData.GetValueOrDefault("row1", ""), rowData.GetValueOrDefault("row2", "") };
            string longestString = listString.OrderByDescending(s => s.Length).FirstOrDefault();
            var adjustedFont = FindFont(graphics, longestString, font);

            StringFormat drawFormat = new() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

            Rectangle layoutRectangleRow1 = new Rectangle(0, 0, bitmap.Width, bitmap.Height / 2);
            Rectangle layoutRectangleRow2 = new Rectangle(0, bitmap.Height / 2, bitmap.Width, bitmap.Height / 2);

            graphics.DrawString(listString[0], adjustedFont, textColor, layoutRectangleRow1, drawFormat);
            graphics.DrawString(listString[1], adjustedFont, textColor, layoutRectangleRow2, drawFormat);

            bitmap.Save(imageFilePath);
            return url;
        }
        /// Author: KhoaND
        /// Description:
        /// Lấy font cho text
        /// Created date: 08/11/2024
        public static Font FindFont(Graphics g, string text, Font preferredFont)
        {
            return string.IsNullOrEmpty(text) ? preferredFont : new Font(preferredFont.FontFamily, preferredFont.Size);
        }
        /// Author: KhoaND
        /// Description:
        ///     Lấy màu cho background
        /// Created date: 08/11/2024
        public static Color GetColorFromString(string color)
        {
            return color switch
            {
                "Blue" => Color.Blue,
                "Black" => Color.Black,
                "White" => Color.White,
                "Yellow" => Color.Yellow,
                "LightGreen" => Color.LightGreen,
                "Green" => Color.Green,
                "Red" => Color.Red,
                "OrangeRed" => Color.OrangeRed,
            };
        }
        /// Author: KhoaND
        /// Description:
        ///     Lấy màu cho chữ 
        /// Created date: 08/11/2024
        public static Brush ConvertStringToBrush(string color)
        {
            return color switch
            {
                "Red" => Brushes.Red,
                "Yellow" => Brushes.Yellow,
                "White" => Brushes.White,
                "Green" => Brushes.Green,
                "LightGreen" => Brushes.LightGreen,
                "OrangeRed" => Brushes.OrangeRed,
                "Black" => Brushes.Black,
            };
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="vmsId"></param>
        /// <returns></returns>
        public async Task<VmsCurrent> GetvmsCurrentEquipmentIDAsync(string vmsId)
        {
            return await _vmsCurrentRepository.AsQueryable().Where(vmsCurrent => vmsCurrent.EquipmentId == vmsId).FirstAsync();

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vmsData"></param>
        /// <param name="incidentid"></param>
        /// <returns></returns>
        public async Task UpdateVmsCurrent(VmsCurrent vmsData, string incidentid = null)
        {
            if (!string.IsNullOrEmpty(incidentid) && vmsData == null)
            {
				var authorizationHeader = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString();
				string token = "";

				if (!string.IsNullOrEmpty(authorizationHeader) && authorizationHeader.StartsWith("Bearer "))
				{
					token = authorizationHeader.Substring("Bearer ".Length).Trim();
				}
				// Check if the input Incidentid is null or empty
				if (string.IsNullOrWhiteSpace(incidentid))
				{
					return;
				}

				// Fetch incident logs by ID
				var IncidentID = await _VmsProcessService.GetIncidentAsync(incidentid, token);

				// Check if the IncidentID result is null or empty
				if (IncidentID == null || !IncidentID.Any())
				{
					return;
				}

				foreach (var item in IncidentID)
				{
					var VmsCurrent = await this.GetvmsCurrentEquipmentIDAsync(item.EquipmentID);

					var currentInput = new Core.Entities.VmsCurrent
					{
						ID = VmsCurrent.ID,
						EquipmentId = item.EquipmentID,
						Code = VmsCurrent.Code,
						Name = VmsCurrent.Name,
						Url = VmsCurrent.DefaultUrl,
						DefaultUrl = VmsCurrent.DefaultUrl,
						RowData = VmsCurrent.DefaultRowData,
						DefaultRowData = VmsCurrent.DefaultRowData,
						Color = VmsCurrent.DefaultColor,
						DefaultColor = VmsCurrent.DefaultColor,
						DefaultBackColor = VmsCurrent.DefaultBackColor,
						BackColor = VmsCurrent.DefaultBackColor,
					};

					await _vmsCurrentRepository.UpdateAsync(vmsData);
					_VmsProcessService.logLastImageAsync(item.EquipmentID, VmsCurrent.DefaultUrl, VmsCurrent.RowData, token, "Reset"); // Pass the last image URL
					ItsDataTransporter.BroadcastData(ItsDataTransporterChannel.VmsUpdateImage, new { ActionType = "vms-success", Data = currentInput });
				}
			}
            else
            {
                //////////////////////////
				await _vmsCurrentRepository.UpdateAsync(vmsData);
				//////////////////////////
			}
		}

        public async Task InsertVmsCurrent(VmsCurrent vmsData)
        {
            await _vmsCurrentRepository.InsertAsync(vmsData);
        }

        /// <summary>
        /// Get list vms current
        /// </summary>
        /// <param name="vmsId"></param>
        /// <returns></returns>
        public async Task<List<VmsCurrent>> GetListVmsCurrent() 
        { 
            return await _vmsCurrentRepository.AsQueryable().ToListAsync();
		}
	}
}
