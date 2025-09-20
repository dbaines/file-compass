using System.Collections.Generic;
using System.Threading.Tasks;
using HDDIndexer.Models;
using System.IO;

namespace HDDIndexer.Services
{
    public interface IDriveService
    {
        Task<IEnumerable<DriveInfo>> GetAvailableDrivesAsync();
        Task<IEnumerable<Drive>> GetCatalogedDrivesAsync();
        Task<Drive?> GetDriveAsync(int driveId);
        Task<Drive> AddDriveAsync(string driveName, DriveInfo driveInfo);
        Task UpdateDriveAsync(Drive drive);
        Task RemoveDriveAsync(int driveId);
        Task<bool> IsDriveCataloged(string serialNumber);
        Task<DriveStatus> GetDriveStatusAsync(int driveId);
        Task RefreshDriveStatusAsync();
    }
}