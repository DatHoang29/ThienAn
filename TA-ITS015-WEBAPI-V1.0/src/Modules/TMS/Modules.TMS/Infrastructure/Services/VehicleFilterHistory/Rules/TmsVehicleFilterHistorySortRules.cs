using Modules.TMS.Controllers.VehicleFilterHistory.Dto;
using Modules.VDS.Core.Exceptions;

namespace Modules.TMS.Infrastructure.Services.VehicleFilterHistory.Rules;

/// <summary>
/// Author: KienLT
/// Description: Quy mệnh đề ORDER BY của lịch sử đối chiếu biển số về bộ cột được phép.
///
/// <para><b>Đây là lớp chặn injection, không phải tiện lợi:</b> chuỗi trả về đi THẲNG vào câu SQL
/// (<c>.OrderBy(sort)</c>), nên chỉ tên cột hằng và hướng hằng được phép đi qua; chữ của người gọi không bao
/// giờ được nối vào SQL.</para>
///
/// <para><b>Vì sao là static class chứ không phải FluentValidation:</b> hàm này KHÔNG chỉ kiểm tra mà còn
/// SINH RA mệnh đề SQL, nên handler phải gọi được nó trực tiếp. Thêm nữa FluentValidation chỉ tự chạy cho
/// tham số action, còn input này còn được dựng nội bộ rồi dispatch qua bus.</para>
/// <para>Trước 22/09/2026 hàm này nằm ngay trong DTO dưới tên <c>VehicleFilterHistoryWindowInput.Sort()</c> —
/// DTO mang logic sinh SQL.</para>
/// Created date: 22/09/2026
/// </summary>
public static class TmsVehicleFilterHistorySortRules
{
    /// <summary>Cột sắp xếp mặc định khi người gọi không chỉ định.</summary>
    public const string DefaultColumn = "DetectTime";

    /// <summary>Mệnh đề ORDER BY đã được quy về bộ cột cho phép.</summary>
    public static string Sort(VehicleFilterHistoryWindowInput input)
    {
        var column = input.Field?.ToLowerInvariant() switch
        {
            "licenseplate" => "LicensePlate",
            "type" => "Type",
            "direction" => "Direction",
            "createtime" => "CreateTime",
            "detecttime" or null or "" => DefaultColumn,
            _ => throw Oops.Oh(BaseMsg.Vds.Exception.SortColumnInvalid)
        };
        var order = input.Order?.ToLowerInvariant() switch
        {
            "asc" => "ASC",
            "desc" or null or "" => "DESC",
            _ => throw Oops.Oh(BaseMsg.Vds.Exception.SortDirectionInvalid)
        };
        return column + " " + order;
    }
}
