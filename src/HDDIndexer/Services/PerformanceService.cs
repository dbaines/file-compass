using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HDDIndexer.Data;
using Microsoft.EntityFrameworkCore;

namespace HDDIndexer.Services
{
    public interface IPerformanceService
    {
        Task OptimizeDatabaseAsync();
        Task<PerformanceMetrics> GetPerformanceMetricsAsync();
        Task CleanupTempFilesAsync();
        Task<DatabaseAnalysis> AnalyzeDatabaseAsync();
        void EnablePerformanceLogging(bool enabled);
        Task<List<PerformanceRecommendation>> GetRecommendationsAsync();
        void SetMemoryLimit(long maxMemoryMB);
        Task CompactDatabaseAsync();
    }

    public class PerformanceService : IPerformanceService
    {
        private readonly CatalogDbContext _dbContext;
        private readonly ISettingsService _settingsService;
        private readonly PerformanceCounter? _memoryCounter;
        private readonly PerformanceCounter? _cpuCounter;
        private bool _performanceLoggingEnabled;
        private long _maxMemoryBytes = 1024 * 1024 * 1024; // 1GB default

        public PerformanceService(CatalogDbContext dbContext, ISettingsService settingsService)
        {
            _dbContext = dbContext;
            _settingsService = settingsService;

            try
            {
                _memoryCounter = new PerformanceCounter("Process", "Working Set", Process.GetCurrentProcess().ProcessName);
                _cpuCounter = new PerformanceCounter("Process", "% Processor Time", Process.GetCurrentProcess().ProcessName);
            }
            catch
            {
                // Performance counters may not be available
            }
        }

        public async Task OptimizeDatabaseAsync()
        {
            try
            {
                // Vacuum the database to reclaim space
                await _dbContext.Database.ExecuteSqlRawAsync("VACUUM");

                // Analyze tables to update statistics
                await _dbContext.Database.ExecuteSqlRawAsync("ANALYZE");

                // Rebuild indexes for better performance
                await RebuildIndexesAsync();

                System.Diagnostics.Debug.WriteLine("Database optimization completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database optimization failed: {ex.Message}");
            }
        }

        public async Task<PerformanceMetrics> GetPerformanceMetricsAsync()
        {
            var metrics = new PerformanceMetrics
            {
                Timestamp = DateTime.Now
            };

            try
            {
                // Memory usage
                var currentProcess = Process.GetCurrentProcess();
                metrics.MemoryUsageMB = currentProcess.WorkingSet64 / (1024 * 1024);
                metrics.MemoryLimitMB = _maxMemoryBytes / (1024 * 1024);

                // CPU usage (approximate)
                if (_cpuCounter != null)
                {
                    metrics.CpuUsagePercent = _cpuCounter.NextValue();
                }

                // Database metrics
                var dbMetrics = await GetDatabaseMetricsAsync();
                metrics.DatabaseSizeMB = dbMetrics.SizeMB;
                metrics.TotalFiles = dbMetrics.FileCount;
                metrics.TotalDrives = dbMetrics.DriveCount;

                // Query performance
                var stopwatch = Stopwatch.StartNew();
                await _dbContext.Files.CountAsync();
                stopwatch.Stop();
                metrics.AverageQueryTimeMs = stopwatch.ElapsedMilliseconds;

                // Disk space
                var tempPath = Path.GetTempPath();
                var drive = new DriveInfo(Path.GetPathRoot(tempPath) ?? "C:");
                metrics.AvailableDiskSpaceGB = drive.AvailableFreeSpace / (1024L * 1024L * 1024L);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get performance metrics: {ex.Message}");
            }

            return metrics;
        }

        public async Task CleanupTempFilesAsync()
        {
            try
            {
                var tempPath = Path.GetTempPath();
                var hddIndexerTempFiles = Directory.GetFiles(tempPath, "HDDIndexer*");

                foreach (var file in hddIndexerTempFiles)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.CreationTime < DateTime.Now.AddDays(-1))
                        {
                            File.Delete(file);
                        }
                    }
                    catch
                    {
                        // Ignore files that can't be deleted
                    }
                }

                // Clean up old backup files if configured
                var backupPath = _settingsService.BackupDirectory;
                if (!string.IsNullOrEmpty(backupPath) && Directory.Exists(backupPath))
                {
                    await CleanupOldBackupsAsync(backupPath);
                }

                System.Diagnostics.Debug.WriteLine("Temporary file cleanup completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Cleanup failed: {ex.Message}");
            }
        }

        public async Task<DatabaseAnalysis> AnalyzeDatabaseAsync()
        {
            var analysis = new DatabaseAnalysis();

            try
            {
                // Get database file size
                var dbPath = _dbContext.Database.GetConnectionString();
                if (dbPath != null && dbPath.Contains("Data Source="))
                {
                    var actualPath = dbPath.Split("Data Source=")[1].Split(';')[0];
                    if (File.Exists(actualPath))
                    {
                        analysis.DatabaseFileSizeMB = new FileInfo(actualPath).Length / (1024 * 1024);
                    }
                }

                // Table statistics
                analysis.FileTableRowCount = await _dbContext.Files.CountAsync();
                analysis.DriveTableRowCount = await _dbContext.Drives.CountAsync();

                // Index usage analysis
                var indexAnalysis = await AnalyzeIndexUsageAsync();
                analysis.IndexEfficiency = indexAnalysis;

                // Query performance analysis
                analysis.SlowQueries = await FindSlowQueriesAsync();

                // Fragmentation analysis
                analysis.FragmentationLevel = await CalculateFragmentationAsync();

                analysis.LastAnalyzed = DateTime.Now;
            }
            catch (Exception ex)
            {
                analysis.Error = ex.Message;
            }

            return analysis;
        }

        public void EnablePerformanceLogging(bool enabled)
        {
            _performanceLoggingEnabled = enabled;

            if (enabled)
            {
                System.Diagnostics.Debug.WriteLine("Performance logging enabled");
            }
        }

        public async Task<List<PerformanceRecommendation>> GetRecommendationsAsync()
        {
            var recommendations = new List<PerformanceRecommendation>();

            try
            {
                var metrics = await GetPerformanceMetricsAsync();
                var analysis = await AnalyzeDatabaseAsync();

                // Memory recommendations
                if (metrics.MemoryUsageMB > metrics.MemoryLimitMB * 0.8)
                {
                    recommendations.Add(new PerformanceRecommendation
                    {
                        Type = RecommendationType.Memory,
                        Severity = RecommendationSeverity.Medium,
                        Title = "High Memory Usage",
                        Description = "The application is using more than 80% of its memory limit.",
                        Action = "Consider increasing the memory limit or closing other applications."
                    });
                }

                // Database size recommendations
                if (analysis.DatabaseFileSizeMB > 1000) // 1GB
                {
                    recommendations.Add(new PerformanceRecommendation
                    {
                        Type = RecommendationType.Database,
                        Severity = RecommendationSeverity.Low,
                        Title = "Large Database",
                        Description = "Your catalog database is quite large.",
                        Action = "Consider archiving old scan data or optimizing the database."
                    });
                }

                // Query performance recommendations
                if (metrics.AverageQueryTimeMs > 1000) // 1 second
                {
                    recommendations.Add(new PerformanceRecommendation
                    {
                        Type = RecommendationType.Query,
                        Severity = RecommendationSeverity.High,
                        Title = "Slow Query Performance",
                        Description = "Database queries are taking longer than expected.",
                        Action = "Run database optimization or rebuild indexes."
                    });
                }

                // Disk space recommendations
                if (metrics.AvailableDiskSpaceGB < 5) // Less than 5GB
                {
                    recommendations.Add(new PerformanceRecommendation
                    {
                        Type = RecommendationType.DiskSpace,
                        Severity = RecommendationSeverity.High,
                        Title = "Low Disk Space",
                        Description = "Available disk space is running low.",
                        Action = "Free up disk space or move the database to a different location."
                    });
                }

                // Fragmentation recommendations
                if (analysis.FragmentationLevel > 30)
                {
                    recommendations.Add(new PerformanceRecommendation
                    {
                        Type = RecommendationType.Maintenance,
                        Severity = RecommendationSeverity.Medium,
                        Title = "Database Fragmentation",
                        Description = "The database has significant fragmentation.",
                        Action = "Run database compaction to improve performance."
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to generate recommendations: {ex.Message}");
            }

            return recommendations;
        }

        public void SetMemoryLimit(long maxMemoryMB)
        {
            _maxMemoryBytes = maxMemoryMB * 1024 * 1024;
            System.Diagnostics.Debug.WriteLine($"Memory limit set to {maxMemoryMB} MB");
        }

        public async Task CompactDatabaseAsync()
        {
            try
            {
                // SQLite-specific compaction
                await _dbContext.Database.ExecuteSqlRawAsync("VACUUM");
                await _dbContext.Database.ExecuteSqlRawAsync("PRAGMA optimize");

                System.Diagnostics.Debug.WriteLine("Database compaction completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database compaction failed: {ex.Message}");
            }
        }

        private async Task RebuildIndexesAsync()
        {
            try
            {
                // Recreate indexes for better performance
                await _dbContext.Database.ExecuteSqlRawAsync("REINDEX");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Index rebuild failed: {ex.Message}");
            }
        }

        private async Task<DatabaseMetrics> GetDatabaseMetricsAsync()
        {
            return new DatabaseMetrics
            {
                FileCount = await _dbContext.Files.CountAsync(),
                DriveCount = await _dbContext.Drives.CountAsync(),
                SizeMB = 0 // Would calculate actual database size
            };
        }

        private async Task CleanupOldBackupsAsync(string backupPath)
        {
            try
            {
                var retentionDays = _settingsService.BackupRetentionDays;
                var cutoffDate = DateTime.Now.AddDays(-retentionDays);

                var backupFiles = Directory.GetFiles(backupPath, "*.backup");
                foreach (var file in backupFiles)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Backup cleanup failed: {ex.Message}");
            }
        }

        private async Task<double> AnalyzeIndexUsageAsync()
        {
            // Simplified index analysis
            return 85.0; // Placeholder for actual analysis
        }

        private async Task<List<string>> FindSlowQueriesAsync()
        {
            // Would analyze query performance
            return new List<string>();
        }

        private async Task<double> CalculateFragmentationAsync()
        {
            // Simplified fragmentation calculation
            return 15.0; // Placeholder
        }
    }

    public class PerformanceMetrics
    {
        public DateTime Timestamp { get; set; }
        public long MemoryUsageMB { get; set; }
        public long MemoryLimitMB { get; set; }
        public double CpuUsagePercent { get; set; }
        public long DatabaseSizeMB { get; set; }
        public int TotalFiles { get; set; }
        public int TotalDrives { get; set; }
        public long AverageQueryTimeMs { get; set; }
        public long AvailableDiskSpaceGB { get; set; }
    }

    public class DatabaseAnalysis
    {
        public long DatabaseFileSizeMB { get; set; }
        public int FileTableRowCount { get; set; }
        public int DriveTableRowCount { get; set; }
        public double IndexEfficiency { get; set; }
        public List<string> SlowQueries { get; set; } = new();
        public double FragmentationLevel { get; set; }
        public DateTime LastAnalyzed { get; set; }
        public string? Error { get; set; }
    }

    public class DatabaseMetrics
    {
        public int FileCount { get; set; }
        public int DriveCount { get; set; }
        public long SizeMB { get; set; }
    }

    public class PerformanceRecommendation
    {
        public RecommendationType Type { get; set; }
        public RecommendationSeverity Severity { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Action { get; set; } = "";
    }

    public enum RecommendationType
    {
        Memory,
        Database,
        Query,
        DiskSpace,
        Maintenance
    }

    public enum RecommendationSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }
}