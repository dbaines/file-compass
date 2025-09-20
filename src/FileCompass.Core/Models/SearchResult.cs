namespace FileCompass.Core.Models;

public class SearchResult
{
    public required IReadOnlyList<FileEntry> Files { get; init; }
    public int TotalCount { get; init; }
    public TimeSpan SearchDuration { get; init; }
    public string? Query { get; init; }

    public bool HasMore => Files.Count < TotalCount;
}
