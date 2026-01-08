namespace FileCompass.Core.Models;

public class SearchQuery
{
    public string? SearchTerm { get; set; }
    public long? LocationId { get; set; }
    public string? Extension { get; set; }
    public string? FileCategory { get; set; }
    public long? MinSize { get; set; }
    public long? MaxSize { get; set; }
    public DateTime? ModifiedAfter { get; set; }
    public DateTime? ModifiedBefore { get; set; }
    public bool IncludeDirectories { get; set; }
    public int MaxResults { get; set; } = -1; // -1 means no limit
    public int Offset { get; set; }

    public bool HasFilters =>
        !string.IsNullOrWhiteSpace(SearchTerm) ||
        LocationId.HasValue ||
        !string.IsNullOrWhiteSpace(Extension) ||
        !string.IsNullOrWhiteSpace(FileCategory) ||
        MinSize.HasValue ||
        MaxSize.HasValue ||
        ModifiedAfter.HasValue ||
        ModifiedBefore.HasValue;
}
