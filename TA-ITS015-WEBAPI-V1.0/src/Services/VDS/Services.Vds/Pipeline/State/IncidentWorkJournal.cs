using System.Security.Cryptography;
using System.Text;
using Services.Vds.Configuration;
using Services.Vds.Shared.State;
namespace Services.Vds.Pipeline.State;

/// <summary>Accepted live frames awaiting raw persistence and incident evaluation. No automatic expiry.</summary>
public sealed class IncidentWorkJournal
{
    private readonly string _root;
    public IncidentWorkJournal(IVdsConfigManager config) => _root = StatePath.Resolve(config.Current.StateDirectory, "incident-work");
    private string PathFor(string json) => Path.Combine(_root, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))) + ".json");
    public bool Contains(string json) => File.Exists(PathFor(json));
    public void Begin(string json) => StatePath.WriteAtomic(PathFor(json), json);
    public void Defer(string json) => File.SetLastWriteTimeUtc(PathFor(json), DateTime.UtcNow);
    public void Complete(string json) => File.Delete(PathFor(json));
    public IEnumerable<string> ReadPending() => Directory.Exists(_root)
        ? Directory.EnumerateFiles(_root, "*.json").OrderBy(File.GetLastWriteTimeUtc).Select(File.ReadAllText)
        : Enumerable.Empty<string>();
}
