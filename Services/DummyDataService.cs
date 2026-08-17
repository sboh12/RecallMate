using RecallMate.Models;

namespace RecallMate.Services;

public class DummyDataService : IDataService
{
    // Pretend data - replace with SQLite + ONNX embedding later
    private readonly List<Snapshot> _snapshots = new()
    {
        new() { Id = 1, Timestamp = DateTime.Now.AddHours(-1), WindowTitle = "Visual Studio Code - RecallMate.csproj", ProcessPath = "code.exe", Summary = "Editing project file" },
        new() { Id = 2, Timestamp = DateTime.Now.AddHours(-2), WindowTitle = "Firefox - How to use ONNX Runtime with C#", ProcessPath = "firefox.exe", Url = "https://onnxruntime.ai", Summary = "Research on ONNX" },
        new() { Id = 3, Timestamp = DateTime.Now.AddDays(-1).AddHours(-3), WindowTitle = "Slack - #dev-team", ProcessPath = "slack.exe", Summary = "Discussion about sprint planning" },
        new() { Id = 4, Timestamp = DateTime.Now.AddDays(-1).AddHours(-4), WindowTitle = "Microsoft Word - Sprint Planning Notes.docx", ProcessPath = "winword.exe", Summary = "Sprint planning document" },
        new() { Id = 5, Timestamp = DateTime.Now.AddDays(-2), WindowTitle = "Terminal - git log", ProcessPath = "cmd.exe", Summary = "Checking commit history" },
    };

    public Task<List<Snapshot>> GetSnapshotsAsync(DateTime from, DateTime to)
        => Task.FromResult(_snapshots.Where(s => s.Timestamp >= from && s.Timestamp <= to).ToList());

    public Task<List<Snapshot>> SearchAsync(string query)
    {
        // Simple string matching for demo - replace with semantic search
        var results = _snapshots.Where(s =>
            s.WindowTitle.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            (s.Summary?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
        return Task.FromResult(results);
    }

    public Task<Snapshot> AddSnapshotAsync(Snapshot snapshot)
    {
        // Still in-memory - swap this class for a SQLite-backed one later
        // and this method becomes an INSERT.
        snapshot.Id = _snapshots.Count == 0 ? 1 : _snapshots.Max(s => s.Id) + 1;
        _snapshots.Add(snapshot);
        return Task.FromResult(snapshot);
    }
}
