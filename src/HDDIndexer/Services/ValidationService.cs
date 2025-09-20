using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HDDIndexer.Data;
using HDDIndexer.Models;
using Microsoft.EntityFrameworkCore;
namespace HDDIndexer.Services
{
    public interface IValidationService
    {
        Task<ValidationResult> ValidateDatabaseAsync();
        Task<ValidationResult> ValidateFileIntegrityAsync(int driveId);
        Task<ValidationResult> ValidateBackupAsync(string backupPath);
        Task<bool> RepairDatabaseAsync(ValidationIssue issue);
        Task<List<ValidationIssue>> FindOrphanedRecordsAsync();
        Task<List<ValidationIssue>> FindMissingFilesAsync(int driveId);
        Task<bool> CleanupOrphanedRecordsAsync();
    }

    public class ValidationService : IValidationService
    {
        private readonly CatalogDbContext _dbContext;
        private readonly IBackupService _backupService;

        public ValidationService(
            CatalogDbContext dbContext,
            IBackupService backupService)
        {
            _dbContext = dbContext;
            _backupService = backupService;
        }

        public async Task<ValidationResult> ValidateDatabaseAsync()
        {
            var result = new ValidationResult { Type = ValidationType.Database };
            var issues = new List<ValidationIssue>();

            try
            {
                // Check for orphaned files (files without valid drive)
                var orphanedFiles = await _dbContext.Files
                    .Where(f => !_dbContext.Drives.Any(d => d.DriveId == f.DriveId))
                    .CountAsync();

                if (orphanedFiles > 0)
                {
                    issues.Add(new ValidationIssue
                    {
                        Type = IssueType.OrphanedRecords,
                        Severity = IssueSeverity.Medium,
                        Description = $"{orphanedFiles} files reference non-existent drives",
                        Count = orphanedFiles,
                        CanAutoFix = true
                    });
                }

                // Check for invalid parent references
                var invalidParents = await _dbContext.Files
                    .Where(f => f.ParentFileId.HasValue &&
                               !_dbContext.Files.Any(pf => pf.FileId == f.ParentFileId))
                    .CountAsync();

                if (invalidParents > 0)
                {
                    issues.Add(new ValidationIssue
                    {
                        Type = IssueType.InvalidReferences,
                        Severity = IssueSeverity.Medium,
                        Description = $"{invalidParents} files have invalid parent references",
                        Count = invalidParents,
                        CanAutoFix = true
                    });
                }

                // Check for duplicate file paths within drives
                var duplicates = await _dbContext.Files
                    .GroupBy(f => new { f.DriveId, f.FullPath })
                    .Where(g => g.Count() > 1)
                    .CountAsync();

                if (duplicates > 0)
                {
                    issues.Add(new ValidationIssue
                    {
                        Type = IssueType.DuplicateRecords,
                        Severity = IssueSeverity.High,
                        Description = $"{duplicates} duplicate file paths found",
                        Count = duplicates,
                        CanAutoFix = true
                    });
                }

                // Check drive statistics consistency
                foreach (var drive in await _dbContext.Drives.ToListAsync())
                {
                    var actualFileCount = await _dbContext.Files
                        .CountAsync(f => f.DriveId == drive.DriveId && !f.IsDirectory);

                    var actualDirCount = await _dbContext.Files
                        .CountAsync(f => f.DriveId == drive.DriveId && f.IsDirectory);

                    if (drive.FileCount != actualFileCount)
                    {
                        issues.Add(new ValidationIssue
                        {
                            Type = IssueType.InconsistentData,
                            Severity = IssueSeverity.Low,
                            Description = $"Drive {drive.DriveName} file count mismatch: stored={drive.FileCount}, actual={actualFileCount}",
                            DriveId = drive.DriveId,
                            CanAutoFix = true
                        });
                    }

                    if (drive.DirectoryCount != actualDirCount)
                    {
                        issues.Add(new ValidationIssue
                        {
                            Type = IssueType.InconsistentData,
                            Severity = IssueSeverity.Low,
                            Description = $"Drive {drive.DriveName} directory count mismatch: stored={drive.DirectoryCount}, actual={actualDirCount}",
                            DriveId = drive.DriveId,
                            CanAutoFix = true
                        });
                    }
                }

                result.Issues = issues;
                result.IsValid = !issues.Any(i => i.Severity == IssueSeverity.High);
                result.ValidationDate = DateTime.Now;

                System.Diagnostics.Debug.WriteLine($"Database validation completed. Found {issues.Count} issues.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database validation failed: {ex.Message}");
                result.IsValid = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        public async Task<ValidationResult> ValidateFileIntegrityAsync(int driveId)
        {
            var result = new ValidationResult { Type = ValidationType.FileIntegrity };
            var issues = new List<ValidationIssue>();

            try
            {
                var drive = await _dbContext.Drives.FindAsync(driveId);
                if (drive == null)
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Drive not found";
                    return result;
                }

                // Check if drive is accessible
                if (!Directory.Exists(drive.DriveName))
                {
                    issues.Add(new ValidationIssue
                    {
                        Type = IssueType.DriveOffline,
                        Severity = IssueSeverity.High,
                        Description = $"Drive {drive.DriveName} is not accessible",
                        DriveId = driveId,
                        CanAutoFix = false
                    });
                }
                else
                {
                    // Sample validation - check a subset of files for existence
                    var sampleFiles = await _dbContext.Files
                        .Where(f => f.DriveId == driveId && !f.IsDirectory)
                        .OrderBy(f => Guid.NewGuid())
                        .Take(100)
                        .ToListAsync();

                    int missingFiles = 0;
                    foreach (var file in sampleFiles)
                    {
                        if (!File.Exists(file.FullPath))
                        {
                            missingFiles++;
                        }
                    }

                    if (missingFiles > 0)
                    {
                        issues.Add(new ValidationIssue
                        {
                            Type = IssueType.MissingFiles,
                            Severity = IssueSeverity.Medium,
                            Description = $"{missingFiles} of {sampleFiles.Count} sampled files are missing",
                            DriveId = driveId,
                            Count = missingFiles,
                            CanAutoFix = false
                        });
                    }
                }

                result.Issues = issues;
                result.IsValid = !issues.Any(i => i.Severity == IssueSeverity.High);
                result.ValidationDate = DateTime.Now;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"File integrity validation failed for drive {driveId}: {ex.Message}");
                result.IsValid = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        public async Task<ValidationResult> ValidateBackupAsync(string backupPath)
        {
            var result = new ValidationResult { Type = ValidationType.Backup };

            try
            {
                result.IsValid = await _backupService.ValidateBackupAsync(backupPath);
                result.ValidationDate = DateTime.Now;

                if (!result.IsValid)
                {
                    result.Issues = new List<ValidationIssue>
                    {
                        new ValidationIssue
                        {
                            Type = IssueType.CorruptedBackup,
                            Severity = IssueSeverity.High,
                            Description = "Backup file is corrupted or invalid",
                            CanAutoFix = false
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Backup validation failed for {backupPath}: {ex.Message}");
                result.IsValid = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        public async Task<bool> RepairDatabaseAsync(ValidationIssue issue)
        {
            try
            {
                switch (issue.Type)
                {
                    case IssueType.OrphanedRecords:
                        await CleanupOrphanedRecordsAsync();
                        return true;

                    case IssueType.InvalidReferences:
                        await FixInvalidReferencesAsync();
                        return true;

                    case IssueType.DuplicateRecords:
                        await RemoveDuplicateRecordsAsync();
                        return true;

                    case IssueType.InconsistentData:
                        if (issue.DriveId.HasValue)
                        {
                            await FixDriveStatisticsAsync(issue.DriveId.Value);
                            return true;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to repair issue: {issue.Description} - {ex.Message}");
            }

            return false;
        }

        public async Task<List<ValidationIssue>> FindOrphanedRecordsAsync()
        {
            var issues = new List<ValidationIssue>();

            var orphanedFiles = await _dbContext.Files
                .Where(f => !_dbContext.Drives.Any(d => d.DriveId == f.DriveId))
                .Select(f => new { f.FileId, f.FileName })
                .ToListAsync();

            foreach (var file in orphanedFiles)
            {
                issues.Add(new ValidationIssue
                {
                    Type = IssueType.OrphanedRecords,
                    Severity = IssueSeverity.Medium,
                    Description = $"Orphaned file: {file.FileName}",
                    FileId = file.FileId,
                    CanAutoFix = true
                });
            }

            return issues;
        }

        public async Task<List<ValidationIssue>> FindMissingFilesAsync(int driveId)
        {
            var issues = new List<ValidationIssue>();

            var files = await _dbContext.Files
                .Where(f => f.DriveId == driveId && !f.IsDirectory)
                .ToListAsync();

            foreach (var file in files)
            {
                if (!File.Exists(file.FullPath))
                {
                    issues.Add(new ValidationIssue
                    {
                        Type = IssueType.MissingFiles,
                        Severity = IssueSeverity.Medium,
                        Description = $"Missing file: {file.FullPath}",
                        FileId = file.FileId,
                        DriveId = driveId,
                        CanAutoFix = false
                    });
                }
            }

            return issues;
        }

        public async Task<bool> CleanupOrphanedRecordsAsync()
        {
            try
            {
                var orphanedFiles = await _dbContext.Files
                    .Where(f => !_dbContext.Drives.Any(d => d.DriveId == f.DriveId))
                    .ToListAsync();

                _dbContext.Files.RemoveRange(orphanedFiles);
                await _dbContext.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"Cleaned up {orphanedFiles.Count} orphaned file records");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to cleanup orphaned records: {ex.Message}");
                return false;
            }
        }

        private async Task FixInvalidReferencesAsync()
        {
            var invalidFiles = await _dbContext.Files
                .Where(f => f.ParentFileId.HasValue &&
                           !_dbContext.Files.Any(pf => pf.FileId == f.ParentFileId))
                .ToListAsync();

            foreach (var file in invalidFiles)
            {
                file.ParentFileId = null; // Reset to root level
            }

            await _dbContext.SaveChangesAsync();
        }

        private async Task RemoveDuplicateRecordsAsync()
        {
            var duplicateGroups = await _dbContext.Files
                .GroupBy(f => new { f.DriveId, f.FullPath })
                .Where(g => g.Count() > 1)
                .ToListAsync();

            foreach (var group in duplicateGroups)
            {
                var duplicates = group.OrderBy(f => f.FileId).Skip(1);
                _dbContext.Files.RemoveRange(duplicates);
            }

            await _dbContext.SaveChangesAsync();
        }

        private async Task FixDriveStatisticsAsync(int driveId)
        {
            var drive = await _dbContext.Drives.FindAsync(driveId);
            if (drive != null)
            {
                drive.FileCount = await _dbContext.Files
                    .CountAsync(f => f.DriveId == driveId && !f.IsDirectory);

                drive.DirectoryCount = await _dbContext.Files
                    .CountAsync(f => f.DriveId == driveId && f.IsDirectory);

                await _dbContext.SaveChangesAsync();
            }
        }
    }

    public class ValidationResult
    {
        public ValidationType Type { get; set; }
        public bool IsValid { get; set; }
        public DateTime ValidationDate { get; set; }
        public List<ValidationIssue> Issues { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    public class ValidationIssue
    {
        public IssueType Type { get; set; }
        public IssueSeverity Severity { get; set; }
        public string Description { get; set; } = string.Empty;
        public int? DriveId { get; set; }
        public int? FileId { get; set; }
        public int Count { get; set; } = 1;
        public bool CanAutoFix { get; set; }
    }

    public enum ValidationType
    {
        Database,
        FileIntegrity,
        Backup
    }

    public enum IssueType
    {
        OrphanedRecords,
        InvalidReferences,
        DuplicateRecords,
        InconsistentData,
        MissingFiles,
        DriveOffline,
        CorruptedBackup
    }

    public enum IssueSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }
}