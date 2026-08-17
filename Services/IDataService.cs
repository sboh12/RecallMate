using RecallMate.Models;

namespace RecallMate.Services;

public interface IDataService
{
    Task<List<Snapshot>> GetSnapshotsAsync(DateTime from, DateTime to);
    Task<List<Snapshot>> SearchAsync(string query);
    Task<Snapshot> AddSnapshotAsync(Snapshot snapshot);
}
