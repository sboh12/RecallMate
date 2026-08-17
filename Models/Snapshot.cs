namespace RecallMate.Models;

public class Snapshot
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string WindowTitle { get; set; } = string.Empty;
    public string ProcessPath { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? Summary { get; set; } // later filled by LLM
}
