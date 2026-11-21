namespace Module.VideoWall.WPF.Api.Dto;

/// <summary>
/// Author: Đạt
/// Description: Bản sao phía client của Shared.DTO.Enums.BaseEnums dùng trong module Video Wall.
/// Backend cấu hình EnumToNumber nên các giá trị này truyền/nhận dưới dạng SỐ — giữ nguyên
/// con số, không đổi thứ tự.
/// Created date: 25/08/2026
/// </summary>
public enum VwStatus
{
    Disable = 0,
    Enable = 1,
}

/// <summary>
/// Author: Đạt
/// Description: Cờ hiển thị của một cửa sổ trong kịch bản (BaseEnums.SceneWindowVisible).
/// Created date: 25/08/2026
/// </summary>
public enum VwWindowVisible
{
    InVisible = 0,
    Visible = 1,
}

/// <summary>
/// Author: Đạt
/// Description: Trạng thái kích hoạt của kịch bản (BaseEnums.ActiveScene).
/// Created date: 25/08/2026
/// </summary>
public enum VwActiveScene
{
    DeActivate = 0,
    Activate = 1,
}

/// <summary>
/// Author: Đạt
/// Description: Cờ đánh dấu bản ghi mặc định (BaseEnums.DefaultEnum).
/// Created date: 25/08/2026
/// </summary>
public enum VwDefault
{
    None = 0,
    Default = 1,
}

/// <summary>
/// Author: Đạt
/// Description: Hành động đổi lớp Z-Order của cửa sổ (VwWindowLayerAction).
/// Created date: 25/08/2026
/// </summary>
public enum VwWindowLayerAction
{
    Top = 1,
    Bottom = 2,
}
