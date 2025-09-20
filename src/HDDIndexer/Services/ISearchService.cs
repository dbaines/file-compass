using System.Collections.Generic;
using System.Threading.Tasks;
using HDDIndexer.Models;
using System;

namespace HDDIndexer.Services
{
    public interface ISearchService
    {
        Task<IEnumerable<FileEntry>> QuickSearchAsync(string query);
        Task<IEnumerable<FileEntry>> AdvancedSearchAsync(SearchCriteria criteria);
        Task<IEnumerable<FileEntry>> SearchByExtensionAsync(string extension);
        Task<IEnumerable<FileEntry>> SearchBySizeRangeAsync(long minSize, long maxSize);
        Task<IEnumerable<FileEntry>> SearchByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<string>> GetSearchSuggestionsAsync(string query);
        Task SaveSearchAsync(string name, SearchCriteria criteria);
        Task<IEnumerable<SavedSearch>> GetSavedSearchesAsync();
    }

    public class SearchCriteria
    {
        public string? FileName { get; set; }
        public SearchMatchType MatchType { get; set; }
        public List<string>? Extensions { get; set; }
        public long? MinSize { get; set; }
        public long? MaxSize { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<int>? DriveIds { get; set; }
        public bool IncludeSubfolders { get; set; } = true;
        public string? Path { get; set; }
        public FileType? FileType { get; set; }
        public bool CaseSensitive { get; set; }
    }

    public enum SearchMatchType
    {
        Contains,
        StartsWith,
        EndsWith,
        Exact,
        Regex
    }

    public enum FileType
    {
        All,
        Documents,
        Images,
        Videos,
        Audio,
        Archives,
        Executables,
        SourceCode
    }

    public class SavedSearch
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public SearchCriteria Criteria { get; set; } = new();
        public DateTime CreatedDate { get; set; }
        public DateTime? LastUsedDate { get; set; }
    }
}