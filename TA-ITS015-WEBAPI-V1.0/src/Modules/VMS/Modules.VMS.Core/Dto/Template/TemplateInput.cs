#region using reference

// For: Entity
using Modules.VMS.Core.Entities;
using Shared.Core.Utilities;
using Shared.DTO.Enums;

// For: BasePageInput

#endregion

namespace Modules.VMS.Controllers.Template.Dto;

/// <summary>
/// Author: Ngọc Thắng
/// Description: Đối tượng dto chứa thông tin phân trang truyền vào
/// Created date: 09/08/2024
/// </summary>
public class PageTemplateInput: BasePageInput
{
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? Size {  get; set; }
    public string? Lang { get; set; }
    public BaseEnums.StatusEnum? Status { get; set; }
}

/// <summary>
/// Author: Ngọc Thắng
/// Description: Đối tượng dto chứa thông tin tìm kiếm truyền vào
/// Created date: 09/08/2024
/// </summary>
public class TemplateInput
{
    public string? ID { get; set; }
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? Size { get; set; }
    public string? Lang { get; set; }
    public BaseEnums.StatusEnum? Status { get; set; }
}

/// <summary>
/// Author: Ngọc Thắng
/// Decription: Đối tượng dto của VmsTemplate chứa thông tin cần lưu truyền vào
/// Created date: 09/08/2024
/// </summary>
public class AddTemplateInput : Core.Entities.VmsTemplate
{
    public string JsonContent { get; set; }
}

/// <summary>
/// Author: Ngọc Thắng
/// Description: Đối tượng dto của VmsTemplate chứa thông tin cần cập nhật truyền vào
/// Created date: 09/08/2024
/// </summary>
public class UpdateTemplateInput : AddTemplateInput {}

/// <summary>
/// Author: Ngọc Thắng
/// Description: Đối tượng dto chứa thông tin cần xóa khỏi csdl truyền vào
/// Created date: 09/08/2024
/// </summary>
public class DeleteTemplateInput: BaseIdInput {}

public class TemplateDesign
{
    [JsonProperty("id")] public int Id { get; set; }
    [JsonProperty("name")] public string? Name { get; set; }
    [JsonProperty("backcolor")] public string? BackColor { get; set; }
    [JsonProperty("elements")] public TemplateElement[]? Elements { get; set; }
}

public class TemplateElement
{
    [JsonProperty("id")] public string? Id { get; set; }
    [JsonProperty("type")] public string? Type { get; set; }   // "text" | "image"
    [JsonProperty("content")] public object? Content { get; set; }
    [JsonProperty("x")] public int X { get; set; }
    [JsonProperty("y")] public int Y { get; set; }
    [JsonProperty("width")] public int Width { get; set; }
    [JsonProperty("height")] public int Height { get; set; }
    [JsonProperty("label")] public string? Label { get; set; }

    // === Field style MỚI (optional -> tương thích JSON cũ) ===
    [JsonProperty("color")] public string? Color { get; set; }
    [JsonProperty("backColor")] public string? BackColor { get; set; }
    [JsonProperty("font")] public string? Font { get; set; }
    [JsonProperty("fontSize")] public string? FontSize { get; set; }
    [JsonProperty("bold")] public bool Bold { get; set; }
    [JsonProperty("align")] public string? Align { get; set; } // "center"|"left"|"right"
}

public class TemplateContent
{
    [JsonProperty("id")] public string? Id { get; set; }
    [JsonProperty("name")] public string? Name { get; set; }
    [JsonProperty("url")] public string? Url { get; set; }
    [JsonProperty("size")] public string? Size { get; set; }
}

/// <summary>
/// Style chung cho việc vẽ text (dùng cho TwoContentToImage & GenerateImage)
/// </summary>
public class TextStyle
{
    public string? Color { get; set; }
    public string? BackColor { get; set; }
    public string? Font { get; set; }
    public string? FontSize { get; set; }
    public bool Bold { get; set; }
    public string? Align { get; set; } // center (default) | left | right

    // Map nhanh từ TemplateElement
    public static TextStyle From(TemplateElement e) => new()
    {
        Color = e.Color,
        BackColor = e.BackColor,
        Font = e.Font,
        FontSize = e.FontSize,
        Bold = e.Bold,
        Align = e.Align
    };
}