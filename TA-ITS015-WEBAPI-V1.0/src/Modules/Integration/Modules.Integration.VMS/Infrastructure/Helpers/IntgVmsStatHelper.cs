using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Modules.Integration.VMS.Base.Core.Enums;
using Modules.Integration.VMS.Base.Core.Exceptions;
using static Modules.Integration.VMS.Base.Core.Enums.IntgVmsEnums;

namespace Modules.Integration.VMS.Infrastructure.Helpers
{
    /// <summary>
    /// Hàm hỗ trợ chuyển đổi thông số hoạt động chung cho các thiết bị khác nhau
    /// </summary>
    internal static class IntgVmsStatHelper
    {

        public class Chainzone
        {
            /// <summary>
            /// Chuyển đổi thông số chung sang thông số Chainzone
            /// Author: SonTH
            /// Create Date: 20/2/2025
            /// </summary>
            /// <param name="stat"></param>
            /// <returns></returns>
            /// <exception cref="ArgumentException"></exception>
            public static IntgVmsEnums.IntgChainzoneStatType ToChainzoneStatType(string stat) => stat switch
            {
                IntgVmsStatType.All => IntgChainzoneStatType.All,
                IntgVmsStatType.TemperatureInside => IntgChainzoneStatType.TemperatureInside,
                IntgVmsStatType.TemperatureOutside => IntgChainzoneStatType.TemperatureOutside,
                IntgVmsStatType.HumidityInside => IntgChainzoneStatType.HumidityInside,
                IntgVmsStatType.HumidityOutside => IntgChainzoneStatType.HumidityOutside,
                IntgVmsStatType.Gradient => IntgChainzoneStatType.Gradient,
                IntgVmsStatType.WindDirection => IntgChainzoneStatType.WindDirection,
                IntgVmsStatType.WindSpeed => IntgChainzoneStatType.WindSpeed,
                IntgVmsStatType.Brightness => IntgChainzoneStatType.Brightness,
                IntgVmsStatType.DoorOpen => IntgChainzoneStatType.DoorOpen,
                IntgVmsStatType.PowerStatus => IntgChainzoneStatType.PowerStatus,
                IntgVmsStatType.FirmwareVersion => IntgChainzoneStatType.FirmwareVersion,
                IntgVmsStatType.PixelErrorCount => IntgChainzoneStatType.PixelErrorCount,

                _ => throw new ArgumentException(BaseMsg.IntgVms.Exception.InvalidStatMapping)
            };
            /// <summary>
            /// Chuyển đổi thông số Chainzone sang thông số chung
            /// Author: SonTH
            /// Create Date: 20/2/2025
            /// </summary>
            /// <param name="type"></param>
            /// <returns></returns>
            /// <exception cref="ArgumentException"></exception>
            public static string ToVmsStatType(int type) => type switch
            {
                (int)IntgChainzoneStatType.All => IntgVmsStatType.All,
                (int)IntgChainzoneStatType.TemperatureInside => IntgVmsStatType.TemperatureInside,
                (int)IntgChainzoneStatType.TemperatureOutside => IntgVmsStatType.TemperatureOutside,
                (int)IntgChainzoneStatType.HumidityInside => IntgVmsStatType.HumidityInside,
                (int)IntgChainzoneStatType.HumidityOutside => IntgVmsStatType.HumidityOutside,
                (int)IntgChainzoneStatType.Gradient => IntgVmsStatType.Gradient,
                (int)IntgChainzoneStatType.WindDirection => IntgVmsStatType.WindDirection,
                (int)IntgChainzoneStatType.WindSpeed => IntgVmsStatType.WindSpeed,
                (int)IntgChainzoneStatType.Brightness => IntgVmsStatType.Brightness,
                (int)IntgChainzoneStatType.DoorOpen => IntgVmsStatType.DoorOpen,
                (int)IntgChainzoneStatType.PowerStatus => IntgVmsStatType.PowerStatus,
                (int)IntgChainzoneStatType.FirmwareVersion => IntgVmsStatType.FirmwareVersion,
                (int)IntgChainzoneStatType.PixelErrorCount => IntgVmsStatType.PixelErrorCount,

                _ => throw new ArgumentException(BaseMsg.IntgVms.Exception.InvalidStatMapping)
            };
        }

    }
}
