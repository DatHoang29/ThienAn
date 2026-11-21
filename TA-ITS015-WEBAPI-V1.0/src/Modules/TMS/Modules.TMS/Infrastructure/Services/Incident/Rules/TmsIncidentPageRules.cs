using Furion.FriendlyException;
using Modules.TMS.Core.Dto.Incident;
using Modules.TMS.Core.Exceptions;

namespace Modules.TMS.Infrastructure.Services.Incident.Rules
{
    /// <summary>
    /// Author: ThangNN
    /// Description: Chặn tham số phân trang/sắp xếp không hợp lệ của danh sách sự cố, và quy tên trường
    /// sắp xếp về đúng bộ được phép.
    /// Tham số đầu vào được chuẩn hóa tại chỗ để handler dùng cùng bộ lọc khi truy vấn.
    /// Created date: 09/08/2024
    /// </summary>
    public static class TmsIncidentPageRules
    {
        /// <summary>Số ngày tối đa của cửa sổ hiển thị hồ sơ đang mở / đã đóng.</summary>
        public const int MaxDaysLimit = 120;

        /// <summary>Số bản ghi tối đa một trang.</summary>
        public const int MaxPageSize = 1000;

        /// <summary>
        /// Chặn tham số phân trang/sắp xếp và quy tên trường sắp xếp về bộ được phép.
        /// </summary>
        /// <param name="input">Tham số phân trang được chuẩn hóa tại chỗ.</param>
        /// <param name="allowUnboundedPageSize">
        /// Chỉ hai đường NỘI BỘ vẽ biểu đồ / đếm sự cố truyền <c>true</c> (chúng cần trọn tập lọc).
        /// Trước đây điều kiện này đọc <c>input.IsRequestForChart</c> — một property công khai trên body —
        /// nên client tự đặt cờ là bỏ được trần. Nay quyền đó nằm ở phía gọi, không nằm trong request.
        /// </param>
        public static void Normalize(PageTmsIncidentInput input, bool allowUnboundedPageSize = false)
        {
            if (input.Page < 1 || input.PageSize < 1 || (!allowUnboundedPageSize && input.PageSize > MaxPageSize))
                throw Oops.Oh(BaseMsg.TmsIncident.Exception.PagingInvalid);

            if (input.OpenIncidentDaysLimit is < 0 or > MaxDaysLimit
                || input.ClosedIncidentDaysLimit is < 0 or > MaxDaysLimit)
                throw Oops.Oh(BaseMsg.TmsIncident.Exception.DisplayWindowInvalid, MaxDaysLimit);

            input.Field = input.Field?.Trim().ToLowerInvariant() switch
            {
                null or "" => null,
                "name" => "Name",
                "code" => "Code",
                "eventtypeid" or "eventtypename" => "EventTypeId",
                "startdate" => "StartDate",
                "createdby" => "CreatedBy",
                "createddate" or "createtime" => "CreateTime",
                "location" => "Location",
                "kmnumber" => "KmNumber",
                "metnumber" => "MetNumber",
                "state" or "statename" => "State",
                "source" => "Source",
                "zoneid" => "ZoneId",
                "injurednumber" => "InjuredNumber",
                "vehiclenumber" => "VehicleNumber",
                "id" => "ID",
                _ => throw Oops.Oh(BaseMsg.TmsIncident.Exception.SortFieldInvalid)
            };

            input.Order = string.IsNullOrWhiteSpace(input.Order) ? "desc" : input.Order.Trim().ToLowerInvariant();
            if (input.Order is not ("asc" or "desc"))
                throw Oops.Oh(BaseMsg.TmsIncident.Exception.SortDirectionInvalid);

            if (input.FromDate > input.ToDate)
                throw Oops.Oh(BaseMsg.TmsIncident.Exception.DateRangeInvalid);
        }
    }
}
