namespace FileCompass.Core.Models;

public class ScanError
{
    public long Id { get; set; }
    public long LocationId { get; set; }
    public required string Path { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorType { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
