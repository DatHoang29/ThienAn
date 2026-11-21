using CommunityToolkit.Mvvm.ComponentModel;

namespace Module.VideoWall.WPF.ViewModels.Isapi;

/// <summary>
/// Description: ViewModel đại diện cho một trường nhập liệu tham số API ISAPI.
/// Created date: 08/09/2026
/// </summary>
public sealed partial class VwIsapiFormFieldViewModel : ObservableObject
{
    /// <summary>Định nghĩa trường dữ liệu (VwIsapiFieldDef).</summary>
    public VwIsapiFieldDef Definition { get; }

    [ObservableProperty]
    private string _value;

    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>Cho biết trường có lỗi xác thực dữ liệu hay không.</summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>Cho biết trường có danh sách lựa chọn dạng dropdown hay không.</summary>
    public bool HasOptions => Definition.Options != null && Definition.Options.Count > 0;

    /// <summary>Cho biết trường là ô nhập tự do (không có danh sách chọn).</summary>
    public bool HasNoOptions => !HasOptions;

    /// <summary>Cho biết trường có nhóm phân cấp XML (GroupPath) hay không.</summary>
    public bool HasGroupPath => !string.IsNullOrWhiteSpace(Definition.GroupPath);

    public VwIsapiFormFieldViewModel(VwIsapiFieldDef definition)
    {
        Definition = definition;
        _value = !string.IsNullOrEmpty(definition.DefaultValue)
            ? definition.DefaultValue
            : (definition.Options != null && definition.Options.Count > 0 ? definition.Options[0] : string.Empty);
    }

    partial void OnValueChanged(string value)
    {
        Validate();
    }

    public bool Validate()
    {
        ErrorMessage = null;

        if (Definition.Required && string.IsNullOrWhiteSpace(Value))
        {
            ErrorMessage = $"Trường '{Definition.Label}' là bắt buộc.";
            OnPropertyChanged(nameof(HasError));
            return false;
        }

        if (!string.IsNullOrWhiteSpace(Value) && Definition.Type == VwIsapiFieldType.Int)
        {
            if (!int.TryParse(Value.Trim(), out var intVal))
            {
                ErrorMessage = $"'{Definition.Label}' phải là số nguyên.";
                OnPropertyChanged(nameof(HasError));
                return false;
            }

            if (Definition.Min.HasValue && intVal < Definition.Min.Value)
            {
                ErrorMessage = $"'{Definition.Label}' phải >= {Definition.Min.Value}.";
                OnPropertyChanged(nameof(HasError));
                return false;
            }

            if (Definition.Max.HasValue && intVal > Definition.Max.Value)
            {
                ErrorMessage = $"'{Definition.Label}' phải <= {Definition.Max.Value}.";
                OnPropertyChanged(nameof(HasError));
                return false;
            }
        }

        OnPropertyChanged(nameof(HasError));
        return true;
    }
}
