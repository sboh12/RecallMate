using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;
using RecallMate.Models;
using Timer = System.Timers.Timer;

namespace RecallMate.Services;

/// <summary>
/// Polls the current foreground window on an interval and raises a Snapshot
/// whenever it changes. Deliberately simple (polling, not hooks) so it's
/// easy to reason about and doesn't need admin rights or a global hook DLL.
/// </summary>
public class WindowCaptureService : IWindowCaptureService
{
    private readonly Timer _timer;
    private string? _lastWindowTitle;

    public event Action<Snapshot>? SnapshotCaptured;

    public WindowCaptureService(TimeSpan? pollInterval = null)
    {
        _timer = new Timer((pollInterval ?? TimeSpan.FromSeconds(5)).TotalMilliseconds);
        _timer.Elapsed += OnTick;
        _timer.AutoReset = true;
    }

    public void Start() => _timer.Start();
    public void Stop() => _timer.Stop();

    private void OnTick(object? sender, ElapsedEventArgs e)
    {
        var snapshot = TryCaptureForegroundWindow();
        if (snapshot is null)
            return;

        // Skip firing again if the user is still looking at the same window.
        if (string.Equals(snapshot.WindowTitle, _lastWindowTitle, StringComparison.Ordinal))
            return;

        _lastWindowTitle = snapshot.WindowTitle;
        SnapshotCaptured?.Invoke(snapshot);
    }

    private static readonly int CurrentProcessId = Environment.ProcessId;

    private static Snapshot? TryCaptureForegroundWindow()
    {
        var hWnd = GetForegroundWindow();
        if (hWnd == IntPtr.Zero)
            return null;

        var title = GetWindowTitle(hWnd);
        if (string.IsNullOrWhiteSpace(title))
            return null; // ignore windows with no title (desktop, some system windows)

        GetWindowThreadProcessId(hWnd, out var processId);

        // Never capture RecallMate's own window - otherwise switching to search
        // or scrolling the timeline pollutes the very history you're browsing.
        if (processId == CurrentProcessId)
            return null;

        string processPath = string.Empty;
        string processName = string.Empty;
        try
        {
            using var process = Process.GetProcessById((int)processId);
            processName = process.ProcessName;
            // MainModule.FileName can throw for elevated/system processes we don't have access to.
            processPath = process.MainModule?.FileName ?? processName;
        }
        catch
        {
            // Access denied or process already exited - fall back to nothing rather than crash.
        }

        string? url = null;
        if (BrowserUrlHelper.IsBrowserProcess(processName))
            url = BrowserUrlHelper.TryGetAddressBarText(hWnd);

        return new Snapshot
        {
            Timestamp = DateTime.Now,
            WindowTitle = title,
            ProcessPath = processPath,
            Url = url,
        };
    }

    private static string GetWindowTitle(IntPtr hWnd)
    {
        var length = GetWindowTextLength(hWnd);
        if (length == 0)
            return string.Empty;

        var builder = new StringBuilder(length + 1);
        GetWindowText(hWnd, builder, builder.Capacity);
        return builder.ToString();
    }

    public void Dispose()
    {
        _timer.Elapsed -= OnTick;
        _timer.Dispose();
    }

    // --- Win32 interop ---

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
}
