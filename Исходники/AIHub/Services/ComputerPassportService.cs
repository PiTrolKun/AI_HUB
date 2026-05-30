using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using AIHub.Models;
using Microsoft.Win32;

namespace AIHub.Services;

public sealed class ComputerPassportService
{
    private const double BytesInGb = 1024d * 1024d * 1024d;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public ComputerPassport EnsurePassport()
    {
        AppDataPaths.EnsureBaseDirectory();

        if (!File.Exists(AppDataPaths.ComputerPassportPath))
        {
            return RegeneratePassport();
        }

        try
        {
            var json = File.ReadAllText(AppDataPaths.ComputerPassportPath);
            return JsonSerializer.Deserialize<ComputerPassport>(json, JsonOptions) ?? RegeneratePassport();
        }
        catch
        {
            return RegeneratePassport();
        }
    }

    public ComputerPassport RegeneratePassport()
    {
        AppDataPaths.EnsureBaseDirectory();

        var passport = new ComputerPassport
        {
            CreatedAt = DateTimeOffset.Now,
            MachineName = Environment.MachineName,
            WindowsVersion = Environment.OSVersion.VersionString,
            OperatingSystemArchitecture = RuntimeInformation.OSArchitecture.ToString(),
            UserName = Environment.UserName,
            CpuName = GetCpuName(),
            RamTotalGb = GetTotalMemoryGb(),
            Drives = GetDrives()
        };

        var json = JsonSerializer.Serialize(passport, JsonOptions);
        File.WriteAllText(AppDataPaths.ComputerPassportPath, json);

        return passport;
    }

    private static string GetCpuName()
    {
        const string cpuKey = @"HARDWARE\DESCRIPTION\System\CentralProcessor\0";

        return Registry.LocalMachine
            .OpenSubKey(cpuKey)
            ?.GetValue("ProcessorNameString")
            ?.ToString()
            ?.Trim() ?? "unknown";
    }

    private static double GetTotalMemoryGb()
    {
        var memoryStatus = new MemoryStatusEx();
        if (!GlobalMemoryStatusEx(memoryStatus))
        {
            return 0;
        }

        return RoundGb(memoryStatus.TotalPhysicalMemory);
    }

    private static List<DrivePassport> GetDrives()
    {
        var drives = new List<DrivePassport>();

        foreach (var drive in DriveInfo.GetDrives())
        {
            try
            {
                if (!drive.IsReady)
                {
                    continue;
                }

                drives.Add(new DrivePassport
                {
                    Name = drive.Name,
                    DriveType = drive.DriveType.ToString(),
                    TotalGb = RoundGb((ulong)drive.TotalSize),
                    FreeGb = RoundGb((ulong)drive.AvailableFreeSpace)
                });
            }
            catch
            {
                // Skip a drive if Windows denies details or the drive disappears during the scan.
            }
        }

        return drives;
    }

    private static double RoundGb(ulong bytes)
    {
        return Math.Round(bytes / BytesInGb, 2);
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MemoryStatusEx buffer);

    [StructLayout(LayoutKind.Sequential)]
    private sealed class MemoryStatusEx
    {
        private readonly uint _length = (uint)Marshal.SizeOf<MemoryStatusEx>();
        private readonly uint _memoryLoad;

        public ulong TotalPhysicalMemory;
        private readonly ulong _availablePhysicalMemory;
        private readonly ulong _totalPageFile;
        private readonly ulong _availablePageFile;
        private readonly ulong _totalVirtual;
        private readonly ulong _availableVirtual;
        private readonly ulong _availableExtendedVirtual;
    }
}
