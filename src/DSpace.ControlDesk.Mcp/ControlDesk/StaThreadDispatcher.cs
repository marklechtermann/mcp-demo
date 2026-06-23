using System.Collections.Concurrent;

namespace DSpace.ControlDesk.Mcp.ControlDesk;

/// <summary>
/// Runs delegates on a single, dedicated STA thread.
/// ControlDesk's COM automation server is apartment-threaded, so every COM call
/// must be marshalled onto the same STA thread. This dispatcher owns that thread
/// and serializes all access to the COM object.
/// </summary>
internal sealed class StaThreadDispatcher : IDisposable
{
    private readonly BlockingCollection<Action> _queue = new();
    private readonly Thread _thread;

    public StaThreadDispatcher()
    {
        _thread = new Thread(Run)
        {
            IsBackground = true,
            Name = "ControlDesk-STA",
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
    }

    private void Run()
    {
        foreach (Action action in _queue.GetConsumingEnumerable())
        {
            action();
        }
    }

    /// <summary>Schedules <paramref name="func"/> on the STA thread and awaits its result.</summary>
    public Task<T> InvokeAsync<T>(Func<T> func, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            _queue.Add(
                () =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        tcs.TrySetCanceled(cancellationToken);
                        return;
                    }

                    try
                    {
                        tcs.TrySetResult(func());
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            tcs.TrySetException(ex);
        }

        return tcs.Task;
    }

    public void Dispose()
    {
        _queue.CompleteAdding();

        if (_thread.IsAlive)
        {
            _thread.Join(TimeSpan.FromSeconds(5));
        }

        _queue.Dispose();
    }
}
