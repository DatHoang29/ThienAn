using Shared.Utility.Utilities;
using System.ComponentModel.DataAnnotations;
using Furion.FriendlyException;
using Modules.VDS.Core.Exceptions;
using Shared.Core.Utilities;

namespace Modules.VDS.Core.Dto;

/// <summary>
/// Author: KienLT
/// Description: Tham số đọc dùng chung cho các API đọc của VDS: cửa sổ thời gian, phân trang và bộ lọc
/// cơ bản (thiết bị / vùng / hướng / biển số).
///
/// <para>Đặt ở <c>Modules.VDS.Core/Dto</c> vì KHÔNG thuộc riêng feature nào — có 4 nơi kế thừa hoặc nhận
/// trực tiếp: Journey, TrafficStatistic, TrafficSummary, VehicleFilterHistory.</para>
///
/// <para><b>Kế thừa <see cref="BasePageInput"/> từ 22/09/2026</b> thay vì tự khai <c>Page</c>/<c>PageSize</c>.
/// Trước đó module có hai bộ phân trang song song: <c>PageTrafficDataInput</c>/<c>PageVdsConfigInput</c> dùng
/// base Shared còn lớp này tự khai. <c>PageSize</c> được đặt lại 50 trong constructor vì mặc định của base
/// là 20 — giữ nguyên hành vi cũ cho client không truyền tham số.</para>
/// <para>Hệ quả: hai DTO không phân trang (<c>StatisticSnapshotInput</c>, <c>TrafficSummaryInput</c>)
/// mang theo trường phân trang không dùng. Đúng như trước đây — C# chỉ cho kế thừa
/// một lớp nên tách hai base sẽ phải nhân đôi phần cửa sổ/bộ lọc. Ghi lại để không ai coi đó là thiếu sót.</para>
/// Created date: 16/09/2026
/// </summary>
public class VdsReadInput : BasePageInput
{
    public VdsReadInput()
    {
        PageSize = 50;
    }

    [Required] public DateTime? FromDate { get; set; }
    [Required] public DateTime? ToDate { get; set; }
    public string? EquipmentId { get; set; }
    public string? ZoneId { get; set; }
    public string? Direction { get; set; }
    public string? LicensePlate { get; set; }

    /// <summary>Số bản ghi tối đa một trang.</summary>
    public const int MaxPageSize = 500;

    /// <summary>Độ dài tối đa của cửa sổ thời gian được phép đọc.</summary>
    public const int MaxWindowDays = 60;

    /// <summary>
    /// Cửa sổ thời gian đã quy về UTC, kèm chặn tham số không hợp lệ.
    ///
    /// <para>Giữ ở DTO thay vì đẩy sang FluentValidation vì các DTO kế thừa nó còn được dựng NỘI BỘ rồi
    /// dispatch qua message bus, không chỉ đến từ tham số action — FluentValidation chỉ tự chạy cho đường
    /// controller nên sẽ bỏ sót các đường đó.</para>
    /// <para>Đây cũng là nơi duy nhất còn chặn <see cref="MaxPageSize"/> cho các DTO kế thừa, vì
    /// <see cref="BasePageInput.PageSize"/> không mang attribute giới hạn.</para>
    /// </summary>
    public (DateTime From, DateTime To) Window()
    {
        if (FromDate is null || ToDate is null) throw Oops.Oh(BaseMsg.Vds.Exception.DateRangeRequired);
        var from = BaseUtil.TimeServer.ToUtc(FromDate.Value);
        var to = BaseUtil.TimeServer.ToUtc(ToDate.Value);
        if (to <= from || to - from > TimeSpan.FromDays(MaxWindowDays))
            throw Oops.Oh(BaseMsg.Vds.Exception.DateRangeInvalid);
        if (Page < 1 || PageSize < 1 || PageSize > MaxPageSize) throw Oops.Oh(BaseMsg.Vds.Exception.PagingInvalid);
        return (from, to);
    }
}
