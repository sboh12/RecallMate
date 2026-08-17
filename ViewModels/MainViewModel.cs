using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RecallMate.Models;
using RecallMate.Services;

namespace RecallMate.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IDataService _dataService;
    private readonly IWindowCaptureService? _captureService;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private bool _isCapturing;

    // Grouped by date for the timeline
    public ObservableCollection<DayGroup> Timeline { get; } = new();

    public MainViewModel(IDataService dataService, IWindowCaptureService? captureService = null)
    {
        _dataService = dataService;
        _captureService = captureService;

        if (_captureService is not null)
        {
            _captureService.SnapshotCaptured += OnSnapshotCaptured;
            _captureService.Start();
            IsCapturing = true;
        }

        // Load last 7 days on startup
        _ = LoadTimelineAsync(DateTime.Now.AddDays(-7), DateTime.Now);
    }

    private async void OnSnapshotCaptured(Snapshot snapshot)
    {
        // Comes in on the timer thread - hop back to the UI thread before touching
        // ObservableCollection, or WPF will throw a cross-thread exception.
        await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            await _dataService.AddSnapshotAsync(snapshot);
            AddToTimeline(snapshot);
            StatusText = $"Captured: {Truncate(snapshot.WindowTitle, 60)}";
        });
    }

    private void AddToTimeline(Snapshot snapshot)
    {
        var existingGroup = Timeline.FirstOrDefault(g => g.Date == snapshot.Timestamp.Date);
        if (existingGroup is not null)
        {
            existingGroup.Snapshots.Insert(0, snapshot);
        }
        else
        {
            var newGroup = new DayGroup(snapshot.Timestamp.Date, new List<Snapshot> { snapshot });
            Timeline.Insert(0, newGroup); // today's group goes at the top
        }
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength] + "...";

    [RelayCommand]
    private void ToggleCapture()
    {
        if (_captureService is null)
            return;

        if (IsCapturing)
        {
            _captureService.Stop();
            IsCapturing = false;
            StatusText = "Capture paused";
        }
        else
        {
            _captureService.Start();
            IsCapturing = true;
            StatusText = "Capture resumed";
        }
    }

    private async Task LoadTimelineAsync(DateTime from, DateTime to)
    {
        StatusText = "Loading...";
        var snapshots = await _dataService.GetSnapshotsAsync(from, to);
        var groups = snapshots
            .GroupBy(s => s.Timestamp.Date)
            .OrderByDescending(g => g.Key)
            .Select(g => new DayGroup(g.Key, g.OrderByDescending(s => s.Timestamp).ToList()));

        Timeline.Clear();
        foreach (var group in groups)
            Timeline.Add(group);

        StatusText = $"Showing {snapshots.Count} snapshots from {from:d} to {to:d}";
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            // reset to last 7 days
            await LoadTimelineAsync(DateTime.Now.AddDays(-7), DateTime.Now);
            return;
        }

        StatusText = $"Searching for \"{SearchQuery}\"...";
        // In real app: use ONNX embedding + vector search here
        var results = await _dataService.SearchAsync(SearchQuery);
        var groups = results
            .GroupBy(s => s.Timestamp.Date)
            .OrderByDescending(g => g.Key)
            .Select(g => new DayGroup(g.Key, g.OrderByDescending(s => s.Timestamp).ToList()));

        Timeline.Clear();
        foreach (var group in groups)
            Timeline.Add(group);
        StatusText = $"Found {results.Count} snapshots matching \"{SearchQuery}\"";
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadTimelineAsync(DateTime.Now.AddDays(-7), DateTime.Now);
    }
}

// Grouping helper for timeline display
public class DayGroup
{
    public DateTime Date { get; }

    // ObservableCollection, not List: WindowCaptureService inserts new snapshots
    // into an existing day's group at runtime, and the UI needs to see that.
    public ObservableCollection<Snapshot> Snapshots { get; }

    public string DateHeader => Date.ToString("dddd, MMM d yyyy");
    public DayGroup(DateTime date, IEnumerable<Snapshot> snapshots)
    {
        Date = date;
        Snapshots = new ObservableCollection<Snapshot>(snapshots);
    }
}
