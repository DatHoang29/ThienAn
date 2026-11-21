using Furion.FriendlyException;
using Modules.TMS.Core.Dto.Incident;
using Modules.TMS.Core.Exceptions;
using Shared.Utility.Utilities;

namespace Modules.TMS.Infrastructure.Services.Incident.Rules
{
    /// <summary>
    /// Author: KienLT
    /// Description: Chặn tham số tra cứu bản tin đã ghi nhận và quy cửa sổ thời gian về giờ server.
    ///
    /// <para><b>Theo đúng mẫu <see cref="TmsIncidentPageRules"/></b>: static, gọi từ handler, ném bằng khóa
    /// localization. Không dùng FluentValidation vì cùng lý do đã ghi ở lớp đó — DTO còn có thể được dựng nội
    /// bộ rồi dispatch qua message bus, mà FluentValidation chỉ tự chạy cho tham số action.</para>
    ///
    /// <para><b>Trước 22/09/2026 toàn bộ phần này nằm trong controller</b> dưới dạng một câu <c>if</c> ném
    /// chuỗi tiếng Việt cứng, và cửa sổ thời gian KHÔNG được quy đổi múi giờ.</para>
    ///
    /// <para><b>Hàm này SỬA input</b> (<c>From</c>, <c>To</c>) — giống <see cref="TmsIncidentPageRules"/>, để
    /// handler dùng luôn giá trị đã chuẩn hoá.</para>
    /// Created date: 22/09/2026
    /// </summary>
    public static class TmsRecordedEventRules
    {
        /// <summary>Độ dài tối đa của cửa sổ tra cứu, tính bằng ngày.</summary>
        public const int MaxDaysLimit = 90;

        /// <summary>Số bản ghi tối đa một trang.</summary>
        public const int MaxPageSize = 200;

        /// <summary>
        /// Chặn tham số và quy cửa sổ thời gian từ UTC (FE gửi) về giờ server để so được với cột
        /// <c>StartDate</c> — cột đó lưu GIỜ SERVER.
        /// </summary>
        /// <param name="input">Tham số tra cứu, BỊ SỬA tại chỗ.</param>
        public static void Normalize(PageRecordedEventInput input)
        {
            if (input.Page < 1 || input.PageSize < 1 || input.PageSize > MaxPageSize)
                throw Oops.Oh(BaseMsg.TmsIncident.Exception.PagingInvalid);

            // FE gửi UTC+0. Cột StartDate lưu giờ server, nên phải quy đổi TRƯỚC khi so, nếu không cửa sổ
            // lệch đúng bằng offset múi giờ (7 tiếng ở Việt Nam) và trả về sai tập bản ghi.
            var now = BaseUtil.TimeServer.GetTimeServer();
            input.From = BaseUtil.TimeServer.GetTimeServer(input.From) ?? now.Date;
            input.To = BaseUtil.TimeServer.GetTimeServer(input.To) ?? now;

            if (input.From > input.To)
                throw Oops.Oh(BaseMsg.TmsIncident.Exception.DateRangeInvalid);

            if ((input.To.Value - input.From.Value).TotalDays > MaxDaysLimit)
                throw Oops.Oh(BaseMsg.TmsIncident.Exception.RecordedWindowTooLarge, MaxDaysLimit);
        }
    }
}
