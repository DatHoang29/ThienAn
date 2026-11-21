using CommunityToolkit.Mvvm.ComponentModel;
using Module.VideoWall.WPF.ViewModels.Isapi;

namespace Module.VideoWall.WPF.ViewModels;

/// <summary>
/// Description: ViewModel đại diện cho một bước kịch bản ISAPI.
/// Created date: 08/09/2026
/// </summary>
public sealed partial class VwScenarioStepViewModel : ObservableObject
{
    /// <summary>Cấu hình mẫu API (Preset) của bước.</summary>
    public VwIsapiPresetItem Preset { get; }

    /// <summary>Form nhập tham số tương ứng với bước kịch bản.</summary>
    public VwIsapiFormViewModel Form { get; }

    [ObservableProperty]
    private string _rawBody = string.Empty;

    /// <summary>Tên hiển thị kèm phương thức HTTP và nhóm chức năng.</summary>
    public string DisplayName => $"[{Preset.Method}] {Preset.Section} — {Preset.Name}";

    /// <summary>Cho biết bước có tham số đường dẫn (Path parameters) cần điền hay không.</summary>
    public bool HasPathFields => Form.HasPathFields;

    /// <summary>Cho biết bước có cần nội dung Body gửi kèm hay không.</summary>
    public bool NeedsBody => Preset.Method != "GET" && Preset.Method != "DELETE";

    public VwScenarioStepViewModel(VwIsapiPresetItem preset, string? initialBody = null)
    {
        Preset = preset;
        Form = new VwIsapiFormViewModel(preset);
        _rawBody = initialBody ?? preset.SampleBody ?? string.Empty;
    }
}
