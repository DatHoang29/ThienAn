using System.Text.Json;
using Services.Vds.Configuration;
using Services.Vds.Shared.State;
namespace Services.Vds.Features.Journey;
public sealed record JourneyProgress(long Revision, DateTime? RewindFrom, Dictionary<string, DateTime> Through);
public sealed class JourneyProgressStore
{
    private readonly object _sync = new();
    private readonly string _path;
    private JourneyProgress _current;
    public JourneyProgressStore(IVdsConfigManager config)
    {
        _path = StatePath.Resolve(config.Current.StateDirectory, "journey-progress-utc.json");
        _current = File.Exists(_path) ? JsonSerializer.Deserialize<JourneyProgress>(File.ReadAllText(_path))
            ?? throw new InvalidDataException("Invalid journey progress") : new(0, null, new());
    }
    public (long Revision, DateTime From) Read(string segment, DateTime initial)
    {
        lock (_sync) return (_current.Revision, _current.Through.TryGetValue(segment, out var at) ? at : _current.RewindFrom ?? initial);
    }
    public bool Commit(string segment, DateTime through, long revision)
    {
        lock (_sync)
        {
            if (_current.Revision != revision) return false;
            var next = new Dictionary<string, DateTime>(_current.Through) { [segment] = through };
            Save(_current with { Through = next });
            return true;
        }
    }
    public void Rewind(DateTime from)
    {
        lock (_sync)
        {
            var next = _current.Through.ToDictionary(x => x.Key, x => x.Value < from ? x.Value : from);
            Save(new(_current.Revision + 1, _current.RewindFrom is {} old && old < from ? old : from, next));
        }
    }
    private void Save(JourneyProgress next)
    { StatePath.WriteAtomic(_path, JsonSerializer.Serialize(next)); _current = next; }
}
