using Celsius.Core.Models;

namespace Celsius.Core.Storage;

/// <summary>
/// Enumerates fixed drives and reports their used-space percentage.
/// </summary>
public sealed class DiskInfoProvider
{
    /// <summary>
    /// Returns usage for every ready fixed drive. Never throws.
    /// </summary>
    public IReadOnlyList<DriveUsage> GetDrives()
    {
        var result = new List<DriveUsage>();

        foreach (var drive in DriveInfo.GetDrives())
        {
            try
            {
                if (drive.DriveType != DriveType.Fixed || !drive.IsReady)
                {
                    continue;
                }

                var total = drive.TotalSize;
                float? usedPercent = total > 0
                    ? (float)((total - drive.AvailableFreeSpace) * 100.0 / total)
                    : null;

                var name = string.IsNullOrWhiteSpace(drive.VolumeLabel)
                    ? drive.Name
                    : $"{drive.Name} ({drive.VolumeLabel})";

                result.Add(new DriveUsage(name, usedPercent, total));
            }
            catch
            {
                // Individual drives can disappear mid-enumeration; skip them.
            }
        }

        return result;
    }
}
