using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Module.VideoWall.WPF.ViewModels;

/// <summary>
/// Description: Kịch bản kiểm thử / vận hành dựng sẵn tích hợp trong giao diện VideoWall.
/// Created date: 08/09/2026
/// </summary>
public sealed partial class VwBuiltInScenario : ObservableObject
{
    /// <summary>Tên kịch bản dựng sẵn.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Mô tả chi tiết kịch bản dựng sẵn.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Danh sách các bước API trong kịch bản.</summary>
    public IReadOnlyList<string> ApiSteps { get; init; } = [];

    /// <summary>Hành động ủy quyền thực thi kịch bản.</summary>
    public Func<CancellationToken, Task>? ExecuteAction { get; init; }

    public Task Run(CancellationToken ct) =>
        ExecuteAction != null ? ExecuteAction(ct) : Task.CompletedTask;
}
