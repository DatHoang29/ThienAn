using System.Collections.Generic;

namespace Module.VideoWall.WPF.ViewModels.Isapi;

/// <summary>
/// Description: Định dạng nội dung Body gửi trong API ISAPI (XML hoặc JSON)
/// Created date: 13/09/2026
/// </summary>
public enum VwIsapiBodyFormat
{
    Xml,
    Json
}

/// <summary>
/// Description: Định nghĩa Schema cho một API ISAPI (Section, phần tử Root, các trường tham số và định dạng)
/// Created date: 13/09/2026
/// </summary>
public sealed record VwIsapiApiSchema(
    string Section,
    string? RootElement,
    IReadOnlyList<VwIsapiFieldDef> Fields,
    IReadOnlyList<string>? RawBlockElements = null,
    VwIsapiBodyFormat BodyFormat = VwIsapiBodyFormat.Xml,
    string? ListWrapperElement = null
);
