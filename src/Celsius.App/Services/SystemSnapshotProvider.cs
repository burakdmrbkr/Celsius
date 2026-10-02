using Celsius.Core;
using Celsius.Core.Models;

namespace Celsius.App.Services;

/// <summary>
/// Holds the most recent <see cref="SystemSnapshot"/> captured from the engine
/// and raises an event whenever it changes. Refreshing happens on a background
/// thread so the UI thread is never blocked by sensor reads.
/// </summary>
public sealed class SystemSnapshotProvider
{
    private readonly object _sync = new();
    private SystemSnapshot _current = SystemSnapshot.Empty;
    private bool _refreshing;

    /// <summary>Raised on a background thread after <see cref="Current"/> changes.</summary>
    public event EventHandler<SystemSnapshot>? SnapshotChanged;

    /// <summary>The most recent snapshot.</summary>
    public SystemSnapshot Current
    {
        get
        {
            lock (_sync)
            {
                return _current;
            }
        }
    }

    /// <summary>
    /// Captures a fresh snapshot without blocking the caller. Overlapping
    /// refreshes are coalesced.
    /// </summary>
    public void Refresh(CelsiusEngine engine)
    {
        lock (_sync)
        {
            if (_refreshing)
            {
                return;
            }

            _refreshing = true;
        }

        _ = Task.Run(() =>
        {
            try
            {
                var snapshot = engine.CaptureSnapshot();
                lock (_sync)
                {
                    _current = snapshot;
                }

                SnapshotChanged?.Invoke(this, snapshot);
            }
            catch
            {
                // Capture already swallows errors; nothing to do here.
            }
            finally
            {
                lock (_sync)
                {
                    _refreshing = false;
                }
            }
        });
    }
}
