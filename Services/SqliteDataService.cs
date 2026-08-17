using System.IO;
using Microsoft.Data.Sqlite;
using RecallMate.Models;

namespace RecallMate.Services;

/// <summary>
/// SQLite-backed IDataService. Stores snapshots in
/// %LocalAppData%\RecallMate\recallmate.db so history survives app restarts.
/// On first run (empty DB) it seeds the same sample rows DummyDataService
/// used to, just so the timeline isn't blank before capture kicks in.
/// </summary>
public class SqliteDataService : IDataService
{
    private readonly string _connectionString;

    public SqliteDataService()
    {
        var dbDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RecallMate");
        Directory.CreateDirectory(dbDirectory);

        var dbPath = Path.Combine(dbDirectory, "recallmate.db");
        _connectionString = $"Data Source={dbPath}";

        Initialize();
    }

    private void Initialize()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using (var createCmd = connection.CreateCommand())
        {
            createCmd.CommandText = """
                CREATE TABLE IF NOT EXISTS Snapshots (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TimestampTicks INTEGER NOT NULL,
                    WindowTitle TEXT NOT NULL,
                    ProcessPath TEXT NOT NULL,
                    Url TEXT NULL,
                    Summary TEXT NULL
                );
                CREATE INDEX IF NOT EXISTS IX_Snapshots_TimestampTicks ON Snapshots(TimestampTicks);
                """;
            createCmd.ExecuteNonQuery();
        }

        using var countCmd = connection.CreateCommand();
        countCmd.CommandText = "SELECT COUNT(*) FROM Snapshots;";
        var count = (long)(countCmd.ExecuteScalar() ?? 0L);
        if (count == 0)
            SeedSampleData(connection);
    }

    private static void SeedSampleData(SqliteConnection connection)
    {
        var samples = new[]
        {
            new Snapshot { Timestamp = DateTime.Now.AddHours(-1), WindowTitle = "Visual Studio Code - RecallMate.csproj", ProcessPath = "code.exe", Summary = "Editing project file" },
            new Snapshot { Timestamp = DateTime.Now.AddHours(-2), WindowTitle = "Firefox - How to use ONNX Runtime with C#", ProcessPath = "firefox.exe", Url = "https://onnxruntime.ai", Summary = "Research on ONNX" },
            new Snapshot { Timestamp = DateTime.Now.AddDays(-1).AddHours(-3), WindowTitle = "Slack - #dev-team", ProcessPath = "slack.exe", Summary = "Discussion about sprint planning" },
            new Snapshot { Timestamp = DateTime.Now.AddDays(-1).AddHours(-4), WindowTitle = "Microsoft Word - Sprint Planning Notes.docx", ProcessPath = "winword.exe", Summary = "Sprint planning document" },
            new Snapshot { Timestamp = DateTime.Now.AddDays(-2), WindowTitle = "Terminal - git log", ProcessPath = "cmd.exe", Summary = "Checking commit history" },
        };

        using var transaction = connection.BeginTransaction();
        foreach (var s in samples)
            InsertSnapshot(connection, s);
        transaction.Commit();
    }

    public Task<List<Snapshot>> GetSnapshotsAsync(DateTime from, DateTime to)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT Id, TimestampTicks, WindowTitle, ProcessPath, Url, Summary
            FROM Snapshots
            WHERE TimestampTicks BETWEEN $from AND $to
            ORDER BY TimestampTicks DESC;
            """;
        cmd.Parameters.AddWithValue("$from", from.Ticks);
        cmd.Parameters.AddWithValue("$to", to.Ticks);

        return Task.FromResult(ReadAll(cmd));
    }

    public Task<List<Snapshot>> SearchAsync(string query)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT Id, TimestampTicks, WindowTitle, ProcessPath, Url, Summary
            FROM Snapshots
            WHERE WindowTitle LIKE $q OR Summary LIKE $q OR Url LIKE $q
            ORDER BY TimestampTicks DESC;
            """;
        cmd.Parameters.AddWithValue("$q", $"%{query}%");

        return Task.FromResult(ReadAll(cmd));
    }

    public Task<Snapshot> AddSnapshotAsync(Snapshot snapshot)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        InsertSnapshot(connection, snapshot);
        return Task.FromResult(snapshot);
    }

    private static void InsertSnapshot(SqliteConnection connection, Snapshot snapshot)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Snapshots (TimestampTicks, WindowTitle, ProcessPath, Url, Summary)
            VALUES ($ticks, $title, $path, $url, $summary);
            SELECT last_insert_rowid();
            """;
        cmd.Parameters.AddWithValue("$ticks", snapshot.Timestamp.Ticks);
        cmd.Parameters.AddWithValue("$title", snapshot.WindowTitle);
        cmd.Parameters.AddWithValue("$path", snapshot.ProcessPath);
        cmd.Parameters.AddWithValue("$url", (object?)snapshot.Url ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$summary", (object?)snapshot.Summary ?? DBNull.Value);

        snapshot.Id = Convert.ToInt32((long)(cmd.ExecuteScalar() ?? 0L));
    }

    private static List<Snapshot> ReadAll(SqliteCommand cmd)
    {
        var results = new List<Snapshot>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new Snapshot
            {
                Id = reader.GetInt32(0),
                Timestamp = new DateTime(reader.GetInt64(1)),
                WindowTitle = reader.GetString(2),
                ProcessPath = reader.GetString(3),
                Url = reader.IsDBNull(4) ? null : reader.GetString(4),
                Summary = reader.IsDBNull(5) ? null : reader.GetString(5),
            });
        }
        return results;
    }
}
