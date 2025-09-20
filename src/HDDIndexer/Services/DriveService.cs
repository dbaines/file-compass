using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using HDDIndexer.Data;
using HDDIndexer.Models;
using Microsoft.EntityFrameworkCore;

namespace HDDIndexer.Services
{
    public class DriveService : IDriveService
    {
        private readonly CatalogDbContext _dbContext;

        public DriveService(CatalogDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<DriveInfo>> GetAvailableDrivesAsync()
        {
            return await Task.Run(() =>
            {
                return DriveInfo.GetDrives()
                    .Where(d => d.IsReady &&
                           (d.DriveType == DriveType.Fixed ||
                            d.DriveType == DriveType.Removable ||
                            d.DriveType == DriveType.Network))
                    .ToList();
            });
        }

        public async Task<IEnumerable<Drive>> GetCatalogedDrivesAsync()
        {
            return await _dbContext.Drives
                .OrderBy(d => d.DriveName)
                .ToListAsync();
        }

        public async Task<Drive?> GetDriveAsync(int driveId)
        {
            return await _dbContext.Drives
                .FirstOrDefaultAsync(d => d.DriveId == driveId);
        }

        public async Task<Drive> AddDriveAsync(string driveName, DriveInfo driveInfo)
        {
            var drive = new Drive
            {
                DriveName = driveName,
                VolumeLabel = driveInfo.VolumeLabel,
                SerialNumber = GetDriveSerialNumber(driveInfo.Name),
                TotalSize = driveInfo.TotalSize,
                FileSystem = driveInfo.DriveFormat,
                ScanDate = DateTime.MinValue,
                Status = DriveStatus.NeverScanned
            };

            _dbContext.Drives.Add(drive);
            await _dbContext.SaveChangesAsync();
            return drive;
        }

        public async Task UpdateDriveAsync(Drive drive)
        {
            _dbContext.Drives.Update(drive);
            await _dbContext.SaveChangesAsync();
        }

        public async Task RemoveDriveAsync(int driveId)
        {
            var drive = await _dbContext.Drives.FindAsync(driveId);
            if (drive != null)
            {
                _dbContext.Drives.Remove(drive);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<bool> IsDriveCataloged(string serialNumber)
        {
            return await _dbContext.Drives
                .AnyAsync(d => d.SerialNumber == serialNumber);
        }

        public async Task<DriveStatus> GetDriveStatusAsync(int driveId)
        {
            var drive = await GetDriveAsync(driveId);
            if (drive == null) return DriveStatus.Error;

            // Check if drive is currently connected
            var availableDrives = await GetAvailableDrivesAsync();
            var isOnline = availableDrives.Any(d =>
                GetDriveSerialNumber(d.Name) == drive.SerialNumber);

            if (!isOnline) return DriveStatus.Offline;

            if (drive.ScanDate == DateTime.MinValue)
                return DriveStatus.NeverScanned;

            var daysSinceLastScan = (DateTime.Now - drive.ScanDate).Days;
            if (daysSinceLastScan > 30)
                return DriveStatus.Outdated;

            return DriveStatus.UpToDate;
        }

        public async Task RefreshDriveStatusAsync()
        {
            var drives = await GetCatalogedDrivesAsync();
            var availableDrives = await GetAvailableDrivesAsync();

            foreach (var drive in drives)
            {
                drive.IsOnline = availableDrives.Any(d =>
                    GetDriveSerialNumber(d.Name) == drive.SerialNumber);
                drive.Status = await GetDriveStatusAsync(drive.DriveId);
            }

            await _dbContext.SaveChangesAsync();
        }

        private string GetDriveSerialNumber(string drivePath)
        {
            try
            {
                var drive = drivePath.Replace("\\", "");
                using var searcher = new ManagementObjectSearcher(
                    $"SELECT VolumeSerialNumber FROM Win32_LogicalDisk WHERE Name = '{drive}'");

                foreach (ManagementObject disk in searcher.Get())
                {
                    var serial = disk["VolumeSerialNumber"]?.ToString();
                    if (!string.IsNullOrEmpty(serial))
                        return serial;
                }
            }
            catch
            {
                // Fallback to volume label if WMI fails
            }

            return Guid.NewGuid().ToString();
        }
    }
}