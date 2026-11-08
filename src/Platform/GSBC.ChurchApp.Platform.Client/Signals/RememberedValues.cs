using System.Collections.Concurrent;

namespace GSBC.ChurchApp.Platform.Client.Signals;

public sealed class RememberedValues : IDisposable
{
    private const ChangeKind ForgetOn = ChangeKind.Session | ChangeKind.Access;

    private readonly ConcurrentDictionary<string, object> _values = new();
    private readonly ClientSignals _signals;

    public RememberedValues(ClientSignals signals)
    {
        _signals = signals;
        _signals.Changed += OnChanged;
    }

    public bool TryGet<T>(string key, out T value)
    {
        if (_values.TryGetValue(key, out object? found) && found is T typed)
        {
            value = typed;
            return true;
        }
        value = default!;
        return false;
    }

    public void Keep(string key, object value) => _values[key] = value;

    public void Dispose() => _signals.Changed -= OnChanged;

    private void OnChanged(ClientChange change)
    {
        if ((change.Kind & ForgetOn) != ChangeKind.None)
            _values.Clear();
    }
}
