using Modules.TMS.Core.Entities;
using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Modules.TMS.Controllers.Zone.Dto;

//public class PageZoneInput : BasePageInput
//{

//    public string Name { get; set; }
//    public string FromKmNumber { get; set; }
//    public string FromMetNumber { get; set; }
//    public string ToKmNumber { get; set; }
//    public string ToMetNumber { get; set; }
//}

public class TreeZoneInput
{

    public string Name { get; set; }
    public string Code { get; set; }
    public string Type { get; set; }
    public string FromKmNumber { get; set; }
    public string FromMetNumber { get; set; }
    public string ToKmNumber { get; set; }
    public string ToMetNumber { get; set; }
    public BaseEnums.StatusEnum? Status { get; set; }

}

public class ZoneInput
{
    public string? ID { get; set; }

    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? Type { get; set; }
    public string? FromKmNumber { get; set; }
    public string? FromMetNumber { get; set; }
    public string? ToKmNumber { get; set; }
    public string? ToMetNumber { get; set; }
    public BaseEnums.StatusEnum? Status { get; set; }

}

public class ZoneWithChildInput
{
    public string? ID { get; set; }
    public string? Code { get; set; }
    public string? Type { get; set; }
    public bool IsExcludeParent { get; set; } = false;

}


public class ZoneStatusInfoInput : ZoneInput
{

}

public class AddZoneInput : TmsZone
{
}

public class UpdateZoneInput : AddZoneInput
{

}

public class DeleteZoneInput : BaseIdInput
{

}

/// <summary>
/// Author: Ngọc Thắng
/// Description: Lớp DTO chứa danh sách zoneId
/// </summary>
public class TmsZoneIdListInput
{
    public string? ZoneIdList { get; set; }
}

/// <summary>
/// Author: Ngọc Thắng
/// Description: Lớp DTO chứa thông tin vị trí
/// </summary>
public class TmsZonePositionInput
{
    public string? ZoneId { get; set; }
    public string? LaneId { get; set; }
    public string? DirectionId { get; set; }
    public int? KmNumber { get; set; }
    public int? MetNumber { get; set; }
    public bool IsExcludeParent { get; set; } = true;
    public string? Location { get; set; }
}

public sealed class SpeedConfigListInput;

public sealed class BatchUpdateZoneSpeedInput
{
    public List<ZoneSpeedUpdateItemInput>? Items { get; set; }
}

/// <summary>
/// Author: KienLT
/// Description: Cập nhật ngưỡng tốc độ vùng; nhận số thực để validator trả lỗi nghiệp vụ khi có phần lẻ.
/// Created date: 09/10/2026
/// </summary>
public sealed class ZoneSpeedUpdateItemInput
{
    public string? Id { get; set; }
    public double? MinSpeed { get; set; }
    public double? MaxSpeed { get; set; }
}
