using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Module.VideoWall.WPF.ViewModels;
using Module.VideoWall.WPF.ViewModels.Isapi;

namespace Module.VideoWall.WPF.Views;

/// <summary>
/// Description: View hiển thị tab nhóm API ISAPI và form tham số tương ứng
/// Created date: 13/09/2026
/// </summary>
public partial class IsapiGroupTabView : UserControl
{
    public static readonly DependencyProperty GroupProperty =
        DependencyProperty.Register(
            nameof(Group),
            typeof(VwIsapiGroup),
            typeof(IsapiGroupTabView),
            new PropertyMetadata(VwIsapiGroup.Board, OnGroupChanged));

    /// <summary>Nhóm API ISAPI hiển thị trên tab này.</summary>
    public VwIsapiGroup Group
    {
        get => (VwIsapiGroup)GetValue(GroupProperty);
        set => SetValue(GroupProperty, value);
    }

    private VwIsapiPresetItem? _lastSelectedPreset;

    public IsapiGroupTabView()
    {
        InitializeComponent();

        UpdateItemsSource();

        ApiListBox.SelectionChanged += (_, _) =>
        {
            if (ApiListBox.SelectedItem is VwIsapiPresetItem preset)
                _lastSelectedPreset = preset;

            SyncSelectionToConnection();
        };

        Loaded += (_, _) =>
        {
            if (_lastSelectedPreset != null)
                ApiListBox.SelectedItem = _lastSelectedPreset;
            else if (ApiListBox.SelectedItem == null && ApiListBox.Items.Count > 0)
                ApiListBox.SelectedIndex = 0;

            SyncSelectionToConnection();
        };
    }

    private void SyncSelectionToConnection()
    {
        if (DataContext is ConnectionViewModel vm)
        {
            vm.SelectedIsapiPreset = ApiListBox.SelectedItem as VwIsapiPresetItem;
        }
    }

    private static void OnGroupChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is IsapiGroupTabView view)
        {
            view.UpdateItemsSource();
        }
    }

    private static int GetMethodOrder(string method) => method.ToUpperInvariant() switch
    {
        "GET" => 1,
        "POST" => 2,
        "PUT" => 3,
        "DELETE" => 4,
        _ => 5
    };

    private static int CompareSection(string a, string b)
    {
        var aParts = a.Split('.');
        var bParts = b.Split('.');
        var maxLen = System.Math.Max(aParts.Length, bParts.Length);

        for (int i = 0; i < maxLen; i++)
        {
            var aNum = i < aParts.Length && int.TryParse(aParts[i], out var an) ? an : 0;
            var bNum = i < bParts.Length && int.TryParse(bParts[i], out var bn) ? bn : 0;
            if (aNum != bNum)
                return aNum.CompareTo(bNum);
        }

        return string.Compare(a, b, System.StringComparison.Ordinal);
    }

    private void UpdateItemsSource()
    {
        if (ApiListBox is null)
            return;

        var items = VwIsapiPresetList.Presets
            .Where(p => p.Group == Group)
            .OrderBy(p => GetMethodOrder(p.Method))
            .ThenBy(p => p.Section, System.Collections.Generic.Comparer<string>.Create(CompareSection))
            .ToList();

        ApiListBox.ItemsSource = items;

        if (items.Count > 0 && ApiListBox.SelectedItem == null)
        {
            ApiListBox.SelectedIndex = 0;
            SyncSelectionToConnection();
        }
    }
}
