using System;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using Module.VideoWall.WPF.Api.Direct.Isapi;

namespace Module.VideoWall.WPF.ViewModels.Isapi;

/// <summary>
/// Description: ViewModel quản lý biểu mẫu nhập tham số cho một preset API ISAPI.
/// Created date: 08/09/2026
/// </summary>
public sealed partial class VwIsapiFormViewModel : ObservableObject
{
    /// <summary>Cấu hình mẫu API (Preset) tương ứng.</summary>
    public VwIsapiPresetItem Preset { get; }

    /// <summary>Lược đồ tham số API (Schema) của phần chức năng.</summary>
    public VwIsapiApiSchema? Schema { get; }

    /// <summary>Toàn bộ các trường tham số của biểu mẫu.</summary>
    public ObservableCollection<VwIsapiFormFieldViewModel> AllFields { get; } = [];

    /// <summary>Danh sách các trường tham số đường dẫn (Path Parameters).</summary>
    public ObservableCollection<VwIsapiFormFieldViewModel> PathFields { get; } = [];

    /// <summary>Cho biết biểu mẫu có tham số đường dẫn hay không.</summary>
    public bool HasPathFields => PathFields.Count > 0;

    [ObservableProperty]
    private string _rawBody = string.Empty;

    partial void OnRawBodyChanged(string value) => OnFieldChanged();

    public event EventHandler? FormChanged;

    public VwIsapiFormViewModel(VwIsapiPresetItem preset)
    {
        Preset = preset;
        Schema = VwIsapiSchemaCatalog.GetSchema(preset.Section);
        _rawBody = preset.SampleBody ?? string.Empty;

        if (Schema != null)
        {
            foreach (var fieldDef in Schema.Fields)
            {
                if (fieldDef.Kind != VwIsapiFieldKind.PathParam)
                    continue;

                var fieldVm = new VwIsapiFormFieldViewModel(fieldDef);
                fieldVm.PropertyChanged += (_, _) => OnFieldChanged();
                AllFields.Add(fieldVm);
                PathFields.Add(fieldVm);
            }
        }
    }

    public bool Validate(out string? firstError)
    {
        firstError = null;
        var isValid = true;
        foreach (var field in PathFields)
        {
            if (!field.Validate())
            {
                isValid = false;
                firstError ??= field.ErrorMessage;
            }
        }
        return isValid;
    }

    public string BuildPath()
    {
        var path = Preset.Url;
        foreach (var pathField in PathFields)
        {
            var placeholder = "{" + pathField.Definition.Key + "}";
            var replacement = string.IsNullOrWhiteSpace(pathField.Value)
                ? (pathField.Definition.DefaultValue ?? string.Empty)
                : pathField.Value.Trim();
            if (!string.IsNullOrEmpty(replacement))
            {
                path = path.Replace(placeholder, replacement, StringComparison.OrdinalIgnoreCase);
            }
        }
        return path;
    }

    /// <summary>
    /// Trích xuất các tham số từ chuỗi URL thực tế và cập nhật ngược lại vào PathFields (liên kết 2 chiều).
    /// </summary>
    public bool TryExtractPathParameters(string actualPath)
    {
        if (string.IsNullOrWhiteSpace(actualPath) || PathFields.Count == 0 || string.IsNullOrWhiteSpace(Preset.Url))
            return false;

        var template = Preset.Url.Trim().TrimStart('/');
        var actual = actualPath.Trim().TrimStart('/');

        // Chuẩn hoá tiền tố "ISAPI/"
        if (!template.StartsWith("ISAPI/", StringComparison.OrdinalIgnoreCase) && actual.StartsWith("ISAPI/", StringComparison.OrdinalIgnoreCase))
        {
            actual = actual[6..].TrimStart('/');
        }
        else if (template.StartsWith("ISAPI/", StringComparison.OrdinalIgnoreCase) && !actual.StartsWith("ISAPI/", StringComparison.OrdinalIgnoreCase))
        {
            actual = "ISAPI/" + actual;
        }

        // Tạo Regex từ Template: thay thế {placeholder} bằng Named Capture Group (?<name>[^/?#]+)
        var escaped = Regex.Escape(template)
            .Replace("\\{", "{")
            .Replace("\\}", "}");

        var pattern = "^" + Regex.Replace(escaped, @"\{([a-zA-Z0-9_]+)\}", m => $"(?<{m.Groups[1].Value}>[^/?#]+)") + "$";

        var match = Regex.Match(actual, pattern, RegexOptions.IgnoreCase);
        if (!match.Success)
            return false;

        var anyChanged = false;
        foreach (var field in PathFields)
        {
            Group? targetGroup = null;
            if (match.Groups[field.Definition.Key].Success)
            {
                targetGroup = match.Groups[field.Definition.Key];
            }
            else
            {
                foreach (Group g in match.Groups)
                {
                    if (g.Success && string.Equals(g.Name, field.Definition.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        targetGroup = g;
                        break;
                    }
                }
            }

            if (targetGroup is { Success: true })
            {
                var extracted = targetGroup.Value;
                if (!string.Equals(field.Value, extracted, StringComparison.Ordinal))
                {
                    field.Value = extracted;
                    anyChanged = true;
                }
            }
        }

        return anyChanged;
    }

    private void OnFieldChanged()
    {
        FormChanged?.Invoke(this, EventArgs.Empty);
    }
}
