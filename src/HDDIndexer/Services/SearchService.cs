using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HDDIndexer.Data;
using HDDIndexer.Models;
using Microsoft.EntityFrameworkCore;

namespace HDDIndexer.Services
{
    public class SearchService : ISearchService
    {
        private readonly CatalogDbContext _dbContext;
        private readonly List<SavedSearch> _savedSearches = new();

        public SearchService(CatalogDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<FileEntry>> QuickSearchAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Enumerable.Empty<FileEntry>();

            var lowerQuery = query.ToLower();

            return await _dbContext.Files
                .Include(f => f.Drive)
                .Where(f => f.FileName.ToLower().Contains(lowerQuery))
                .Take(100)
                .ToListAsync();
        }

        public async Task<IEnumerable<FileEntry>> AdvancedSearchAsync(SearchCriteria criteria)
        {
            var query = _dbContext.Files.Include(f => f.Drive).AsQueryable();

            // Apply filename filter
            if (!string.IsNullOrEmpty(criteria.FileName))
            {
                switch (criteria.MatchType)
                {
                    case SearchMatchType.Exact:
                        query = criteria.CaseSensitive
                            ? query.Where(f => f.FileName == criteria.FileName)
                            : query.Where(f => f.FileName.ToLower() == criteria.FileName.ToLower());
                        break;

                    case SearchMatchType.StartsWith:
                        query = criteria.CaseSensitive
                            ? query.Where(f => f.FileName.StartsWith(criteria.FileName))
                            : query.Where(f => f.FileName.ToLower().StartsWith(criteria.FileName.ToLower()));
                        break;

                    case SearchMatchType.EndsWith:
                        query = criteria.CaseSensitive
                            ? query.Where(f => f.FileName.EndsWith(criteria.FileName))
                            : query.Where(f => f.FileName.ToLower().EndsWith(criteria.FileName.ToLower()));
                        break;

                    case SearchMatchType.Contains:
                        query = criteria.CaseSensitive
                            ? query.Where(f => f.FileName.Contains(criteria.FileName))
                            : query.Where(f => f.FileName.ToLower().Contains(criteria.FileName.ToLower()));
                        break;

                    case SearchMatchType.Regex:
                        // Note: This is simplified - in production, would use SQL regex or client evaluation
                        var files = await query.ToListAsync();
                        var regex = new Regex(criteria.FileName,
                            criteria.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
                        return files.Where(f => regex.IsMatch(f.FileName));
                }
            }

            // Apply extension filter
            if (criteria.Extensions?.Any() == true)
            {
                var lowerExtensions = criteria.Extensions.Select(e => e.ToLower()).ToList();
                query = query.Where(f => f.FileExtension != null && lowerExtensions.Contains(f.FileExtension));
            }

            // Apply size filter
            if (criteria.MinSize.HasValue)
            {
                query = query.Where(f => f.FileSize >= criteria.MinSize.Value);
            }
            if (criteria.MaxSize.HasValue)
            {
                query = query.Where(f => f.FileSize <= criteria.MaxSize.Value);
            }

            // Apply date filter
            if (criteria.StartDate.HasValue)
            {
                query = query.Where(f => f.DateModified >= criteria.StartDate.Value);
            }
            if (criteria.EndDate.HasValue)
            {
                query = query.Where(f => f.DateModified <= criteria.EndDate.Value);
            }

            // Apply drive filter
            if (criteria.DriveIds?.Any() == true)
            {
                query = query.Where(f => criteria.DriveIds.Contains(f.DriveId));
            }

            // Apply path filter
            if (!string.IsNullOrEmpty(criteria.Path))
            {
                query = query.Where(f => f.FilePath.StartsWith(criteria.Path));
            }

            // Apply file type filter
            if (criteria.FileType.HasValue && criteria.FileType != FileType.All)
            {
                var extensions = GetExtensionsForFileType(criteria.FileType.Value);
                query = query.Where(f => f.FileExtension != null && extensions.Contains(f.FileExtension));
            }

            return await query.Take(1000).ToListAsync();
        }

        public async Task<IEnumerable<FileEntry>> SearchByExtensionAsync(string extension)
        {
            var lowerExtension = extension.ToLower();
            if (!lowerExtension.StartsWith("."))
                lowerExtension = "." + lowerExtension;

            return await _dbContext.Files
                .Include(f => f.Drive)
                .Where(f => f.FileExtension == lowerExtension)
                .Take(1000)
                .ToListAsync();
        }

        public async Task<IEnumerable<FileEntry>> SearchBySizeRangeAsync(long minSize, long maxSize)
        {
            return await _dbContext.Files
                .Include(f => f.Drive)
                .Where(f => f.FileSize >= minSize && f.FileSize <= maxSize && !f.IsDirectory)
                .OrderByDescending(f => f.FileSize)
                .Take(1000)
                .ToListAsync();
        }

        public async Task<IEnumerable<FileEntry>> SearchByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbContext.Files
                .Include(f => f.Drive)
                .Where(f => f.DateModified >= startDate && f.DateModified <= endDate)
                .OrderByDescending(f => f.DateModified)
                .Take(1000)
                .ToListAsync();
        }

        public async Task<IEnumerable<string>> GetSearchSuggestionsAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return Enumerable.Empty<string>();

            var lowerQuery = query.ToLower();

            var suggestions = await _dbContext.Files
                .Where(f => f.FileName.ToLower().StartsWith(lowerQuery))
                .Select(f => f.FileName)
                .Distinct()
                .Take(10)
                .ToListAsync();

            return suggestions;
        }

        public async Task SaveSearchAsync(string name, SearchCriteria criteria)
        {
            var savedSearch = new SavedSearch
            {
                Id = _savedSearches.Count + 1,
                Name = name,
                Criteria = criteria,
                CreatedDate = DateTime.Now
            };

            _savedSearches.Add(savedSearch);
            await Task.CompletedTask;
        }

        public async Task<IEnumerable<SavedSearch>> GetSavedSearchesAsync()
        {
            return await Task.FromResult(_savedSearches);
        }

        private List<string> GetExtensionsForFileType(FileType fileType)
        {
            return fileType switch
            {
                FileType.Documents => new() { ".doc", ".docx", ".pdf", ".txt", ".rtf", ".odt", ".xls", ".xlsx", ".ppt", ".pptx" },
                FileType.Images => new() { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".svg", ".ico", ".tiff", ".webp" },
                FileType.Videos => new() { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".mpg", ".mpeg" },
                FileType.Audio => new() { ".mp3", ".wav", ".flac", ".aac", ".ogg", ".wma", ".m4a", ".opus", ".ape" },
                FileType.Archives => new() { ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz", ".iso", ".cab" },
                FileType.Executables => new() { ".exe", ".msi", ".app", ".deb", ".rpm", ".dmg", ".pkg", ".appx" },
                FileType.SourceCode => new() { ".cs", ".cpp", ".c", ".h", ".py", ".js", ".ts", ".java", ".go", ".rs", ".swift", ".kt", ".rb", ".php", ".html", ".css", ".sql", ".json", ".xml", ".yaml" },
                _ => new()
            };
        }
    }
}