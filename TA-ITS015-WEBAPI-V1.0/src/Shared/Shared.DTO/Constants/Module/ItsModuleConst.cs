using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTO.Constants.Module
{
    public static class ItsOpConfigConst
    {
        public const string ItsOpConfig = "its_operation_config";
    }

    /// <summary>
    /// Cấu hình hướng di chuyển trong hệ thống ITS
    /// </summary>
    public static class ItsDirectionConfig
    {
        /// <summary>
        /// Tất cả chiều
        /// </summary>
        public const string Both = "0";
        /// <summary>
        /// Chiều thuận. Vị trí tăng dần
        /// </summary>
        public const string Forward = "1";
        /// <summary>
        /// Chiều nghịch. Vị trí giảm dần
        /// </summary>
        public const string Backward = "2";

    }
}
