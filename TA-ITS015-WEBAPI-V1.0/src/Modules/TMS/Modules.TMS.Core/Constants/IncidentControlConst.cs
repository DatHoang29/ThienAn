using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.TMS.Core.Constants
{
    public static class IncidentControlConst
    {
        public static class State
        {
            public const string Override = "override";  // Đè hình — nội dung thiết bị đã bị ghi đè (set khi Update)
            public const string Appeend = "append";      // Lặp/nối hình
        }

        /// <summary>
        /// Độ ưu tiên mặc định của sự cố khi giữ thiết bị (sự cố tự động, bản ghi cũ chưa có Priority).
        /// Số nhỏ = ưu tiên cao.
        /// </summary>
        public const int DefaultPriority = 5;

        /// <summary>
        /// Trạng thái sự cố KHÔNG còn giữ thiết bị (Bỏ qua / Đã xử lý xong / Đã hủy).
        /// Bản ghi TmsIncidentControl của các sự cố này là dữ liệu sót, không được tính là đang chiếm thiết bị.
        /// Dùng chung cho mọi truy vấn "thiết bị đang bị sự cố giữ" để chỉ có một định nghĩa.
        /// </summary>
        public static readonly List<string> InactiveIncidentStates = new()
        {
            ((int)Shared.DTO.Enums.BaseEnums.TmsIncidentStateEnum.Skipped).ToString(),
            ((int)Shared.DTO.Enums.BaseEnums.TmsIncidentStateEnum.Finished).ToString(),
            ((int)Shared.DTO.Enums.BaseEnums.TmsIncidentStateEnum.Canceled).ToString(),
        };
    }
}
