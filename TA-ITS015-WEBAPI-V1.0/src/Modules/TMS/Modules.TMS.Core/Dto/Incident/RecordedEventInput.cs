using Shared.Core.Utilities;

namespace Modules.TMS.Core.Dto.Incident
{
    /// <summary>
    /// Author: KienLT
    /// Description: DTO tham số phân trang lịch sử bản tin đã ghi nhận (nhóm <c>event</c>).
    ///
    /// <para><b>Trước 22/09/2026 lớp này tên <c>RecordedEventQuery</c> và được khai NGAY TRONG file
    /// controller</b>, tự khai <c>Page</c>/<c>PageSize</c> thay vì kế thừa <see cref="BasePageInput"/> —
    /// trong khi <see cref="PageTmsIncidentInput"/> của cùng feature đã kế thừa base đó. Nay theo đúng một
    /// mẫu và nằm đúng chỗ.</para>
    ///
    /// <para><b>Vì sao đặt ở <c>Modules.TMS.Core/Dto/Incident</c> chứ không phải
    /// <c>Controllers/Incident/Dto</c>:</b> thư mục thứ hai bị <c>Compile Remove</c> trong
    /// <c>Modules.TMS.csproj</c> — file đặt ở đó KHÔNG được biên dịch. Mọi DTO sống của feature Incident đều
    /// ở đây.</para>
    ///
    /// <para><b>Giữ tên trường <c>From</c>/<c>To</c></b> (không đổi thành <c>FromDate</c>/<c>ToDate</c> như
    /// <see cref="PageTmsIncidentInput"/>) để không đổi hợp đồng HTTP đang có. Đây là lệch tên CÓ CHỦ Ý.</para>
    /// Created date: 22/09/2026
    /// </summary>
    public class PageRecordedEventInput : BasePageInput
    {
        /// <summary>Mốc đầu cửa sổ tra cứu; FE gửi theo UTC+0.</summary>
        public DateTime? From { get; set; }

        /// <summary>Mốc cuối cửa sổ tra cứu; FE gửi theo UTC+0.</summary>
        public DateTime? To { get; set; }

        public string? EventTypeId { get; set; }

        public string? ZoneId { get; set; }
    }
}
