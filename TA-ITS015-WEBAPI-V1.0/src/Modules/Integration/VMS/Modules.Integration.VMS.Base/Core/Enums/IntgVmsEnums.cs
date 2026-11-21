using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modules.Integration.VMS.Base.Core.Enums
{
    public class IntgVmsEnums
    {
        public enum IntgVmsIntegrationType{

            Sdk,
            Api
        }

        public class IntgVmsIntegration
        {
            public const string Sdk = "sdk";
            public const string Api = "api";
        }

        public class IntgVmsStatType
        {
            /// <summary>
            /// Tất cả
            /// </summary>
            public const string All = "all";
            /// <summary>
            /// Nhiệt độ bên trong
            /// </summary>
            public const string TemperatureInside = "temperatureIn";
            /// <summary>
            /// Nhiet độ bên ngoài
            /// </summary>
            public const string TemperatureOutside = "temperatureOut";
            /// <summary>
            /// Độ ẩm bên trong
            /// </summary>
            public const string HumidityInside = "humidityIn";
            /// <summary>
            /// Độ ẩm bên ngoài
            /// </summary>
            public const string HumidityOutside = "humidityOut";
            /// <summary>
            /// Độ nghiêng của thiết bị
            /// </summary>
            public const string Gradient = "gradient";
            /// <summary>
            /// Hướng gió
            /// </summary>
            public const string WindDirection = "windDirection";
            /// <summary>
            /// Tốc độ gió
            /// </summary>
            public const string WindSpeed = "windSpeed";
            /// <summary>
            /// Độ sáng
            /// </summary>
            public const string Brightness = "brightness";
            /// <summary>
            /// Cửa mở hay không
            /// </summary>
            public const string DoorOpen = "doorOpen";
            /// <summary>
            /// Trạng thái nguồn
            /// </summary>
            public const string PowerStatus = "powerStatus";
            /// <summary>
            /// Phiên bản firmware
            /// </summary>
            public const string FirmwareVersion = "firmwareVersion";
            /// <summary>
            /// Số lỗi pixel
            /// </summary>
            public const string PixelErrorCount = "pixelErrorCount";

        }
      
        public enum IntgChainzoneStatType
        {
            /// <summary>
            /// Tất cả
            /// </summary>
            All = 0,
            /// <summary>
            /// nhiệt độ bên trong
            /// </summary>
            TemperatureInside = 1,
            /// <summary>
            /// Nhiệt độ bên ngoài
            /// </summary>
            TemperatureOutside = 2,
            /// <summary>
            /// Độ ẩm bên trong
            /// </summary>
            HumidityInside = 3,
            /// <summary>
            /// Độ ẩm bên ngoài
            /// </summary>
            HumidityOutside = 4,
            /// <summary>
            /// Độ nghiêng của thiết bị
            /// </summary>
            Gradient = 5,
            /// <summary>
            /// Hướng gió
            /// </summary>
            WindDirection = 6,
            /// <summary>
            /// Tốc độ gió
            /// </summary>
            WindSpeed = 7,
            /// <summary>
            /// Độ sáng
            /// </summary>
            Brightness = 8,
            /// <summary>
            /// Cửa mở hay không
            /// </summary>
            DoorOpen = 9,
            /// <summary>
            /// Trạng thái nguồn
            /// </summary>
            PowerStatus = 10,
            /// <summary>
            /// Phiên bản firmware
            /// </summary>
            FirmwareVersion = 11,
            /// <summary>
            /// Số lỗi pixel
            /// </summary>
            PixelErrorCount = 12
        }
    }
}
