using System.Collections.Generic;

namespace Module.VideoWall.WPF.ViewModels.Isapi;

/// <summary>
/// Description: Loại tham số trường ISAPI (tham số đường dẫn PathParam, trường BodyField hoặc khối XML thô)
/// Created date: 13/09/2026
/// </summary>
public enum VwIsapiFieldKind
{
    PathParam,
    BodyField,
    RawXmlBlock
}

/// <summary>
/// Description: Kiểu dữ liệu của trường tham số ISAPI (chuỗi, số nguyên, boolean, enum)
/// Created date: 13/09/2026
/// </summary>
public enum VwIsapiFieldType
{
    String,
    Int,
    Bool,
    Enum
}

/// <summary>
/// Description: Định nghĩa chi tiết một trường tham số trên form ISAPI tự động
/// Created date: 13/09/2026
/// </summary>
public sealed record VwIsapiFieldDef(
    string Key,
    string Label,
    VwIsapiFieldKind Kind,
    VwIsapiFieldType Type = VwIsapiFieldType.String,
    bool Required = false,
    string? DefaultValue = null,
    string? GroupPath = null,
    IReadOnlyList<string>? Options = null,
    int? Min = null,
    int? Max = null,
    bool Advanced = false,
    string? Tooltip = null,
    bool ReadOnly = false
);
