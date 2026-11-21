using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTO.Upload
{
    public class BaseUploadConfig
    {

        public class Folder
        {
            public const string EquipmentType = "Upload\\EquipmentType";
            public const string EventType = "Upload\\EventType";


        }

        public class Vms
        {
            public const string Template = "Upload\\Vms\\Template";
            public const string SendToDevice = "Upload\\Vms\\SendToDevice";

        }

        public class WP
        {
            public const string Thumbnail = "Upload\\WP\\Post";
            public const string Image = "Upload\\WP\\Image";
            public const string Pdf = "Upload\\WP\\TrafficLaw";
            public const string Logo = "Upload\\WP\\Logo";


        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Nội dung HTML của email (soạn bằng wangEditor) — DB chỉ lưu đường dẫn file.
        ///              Template: mẫu mail ({ID mẫu}.html). ScheduleTemplate: nội dung riêng của từng lịch gửi,
        ///              chia thư mục theo ngày tạo lịch (yyyy\MM\dd\{ID lịch}.html).
        /// Created date: 12/10/2026
        /// </summary>
        public class Mail
        {
            public const string Template = "Upload\\Mail\\Template";
            public const string ScheduleTemplate = "Upload\\Mail\\ScheduleTemplate";
            public const string Image = "Upload\\Mail\\Image";
        }


    }
    
}
