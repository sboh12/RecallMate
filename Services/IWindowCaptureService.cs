using RecallMate.Models;

namespace RecallMate.Services;

public interface IWindowCaptureService : IDisposable
{
    /// <summary>Raised whenever a new (distinct) foreground window is detected.</summary>
    event Action<Snapshot>? SnapshotCaptured;

    void Start();
    void Stop();
}
