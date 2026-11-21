// Author: Codex
// Description: Tracks fallback/rejection within one requested reload, including errors caught by loaders.
// Created date: 02/10/2026

namespace Services.Vds.Catalog;

/// <summary>Tracks fallback/rejection within one requested reload, including errors caught by loaders.</summary>
public sealed class CatalogRefreshAudit : IDisposable
{
    private static readonly AsyncLocal<CatalogRefreshAudit?> Current = new();
    private readonly CatalogRefreshAudit? _previous;
    private readonly HashSet<string> _issues = new(StringComparer.Ordinal);
    public CatalogRefreshAudit() { _previous = Current.Value; Current.Value = this; }
    public string[] Issues { get { lock (_issues) return _issues.OrderBy(x => x).ToArray(); } }
    public static void Failed(string source)
    {
        var audit = Current.Value;
        if (audit is null) return;
        lock (audit._issues) audit._issues.Add(source);
    }
    public void Dispose() { Current.Value = _previous; }
}
