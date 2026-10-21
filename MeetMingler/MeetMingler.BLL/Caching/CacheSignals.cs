using System.Collections.Concurrent;
using Microsoft.Extensions.Primitives;

namespace MeetMingler.BLL.Caching;

public class CacheSignals
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _sources = new();

    public IChangeToken GetToken(string signal)
    {
        return new CancellationChangeToken(_sources.GetOrAdd(signal, _ => new CancellationTokenSource()).Token);
    }

    public void Signal(string signal)
    {
        if (_sources.TryRemove(signal, out var source))
            source.Cancel();
    }
}
