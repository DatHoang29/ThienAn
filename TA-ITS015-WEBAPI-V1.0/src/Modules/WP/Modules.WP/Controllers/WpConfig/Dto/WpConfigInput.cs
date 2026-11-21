using Microsoft.AspNetCore.Http;
using Modules.WP.Core.Entities;
using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Modules.WP.Controllers.Config.Dto;

/// <summary>
/// Author: HieuNV
/// Description: Các Input DTO cho tính năng cấu hình hệ thống WP (Config)
/// Created date: 03/07/2026
/// </summary>
public class WpConfigInfoInput { }
/// <summary>
/// Author: HieuNV
/// Description: Tham số đầu vào của Config Update
/// Created date: 14/08/2026
/// </summary>
public class WpConfigUpdateInput
 {
        public string? sysWebTitle { get; set; }
        public string? sysWebAddress { get; set; }
        public string? sysWebDescription { get; set; }
        public string? sysWebTelephone { get; set; }
        public string? sysWebCopyright { get; set; }
        public string? sysWebLogo { get; set; }
        public string? sysWebRepresent { get; set; }
        public string? sysWebEmail { get; set; }
        public string? sysWebImg1 { get; set; }
        public string? sysWebImg2 { get; set; }
        public string? sysWebImg3 { get; set; }
        public string? sysWebImgTitle1 { get; set; }
        public string? sysWebImgTitle2 { get; set; }
        public string? sysWebImgTitle3 { get; set; }
        public string? sysWebOffice { get; set; }
        public string? sysOfficeAddress { get; set; }
        public string? sysOfficeTelephone { get; set; }
        public string? sysBusinessLicense { get; set; }
        public string? webCamSum { get; set;  }
        public string? webTotalLength { get; set; }
        public string? webTotalToll { get; set; }
        public string? webTotalWork { get; set; }
        /* Ô truy cập nhanh (quick tiles) trang chủ WP: 4 ô cố định, mỗi ô gồm tên, đường dẫn, icon */
        public string? webTile1Label { get; set; }
        public string? webTile1Path { get; set; }
        public string? webTile1Icon { get; set; }
        public string? webTile2Label { get; set; }
        public string? webTile2Path { get; set; }
        public string? webTile2Icon { get; set; }
        public string? webTile3Label { get; set; }
        public string? webTile3Path { get; set; }
        public string? webTile3Icon { get; set; }
        public string? webTile4Label { get; set; }
        public string? webTile4Path { get; set; }
        public string? webTile4Icon { get; set; }
        /* Nút ở phần hero trang chủ WP: nút 1 (chính), nút 2 (phụ), mỗi nút gồm tên, đường dẫn */
        public string? webHeroBtn1Label { get; set; }
        public string? webHeroBtn1Path { get; set; }
        public string? webHeroBtn2Label { get; set; }
        public string? webHeroBtn2Path { get; set; }
        /* Màu giao diện web portal (mã hex, vd #2E3191): màu chính (bắt buộc), màu nhấn (tùy chọn) */
        public string? webThemePrimary { get; set; }
        public string? webThemeAccent { get; set; }
}
/// <summary>
/// Author: HieuNV
/// Description: Tham số đầu vào của Config Upload Img
/// Created date: 14/08/2026
/// </summary>

public class WPConfigUploadImgInput
{
    public IFormFile? File { get; set; }
}
