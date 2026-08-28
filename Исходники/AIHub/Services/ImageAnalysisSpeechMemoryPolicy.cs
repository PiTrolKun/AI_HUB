using System.Runtime.InteropServices;
using AIHub.Models;

namespace AIHub.Services;

public sealed class ImageAnalysisSpeechMemoryPolicy
{
    public const long DefaultExpectedRuntimeBytes = 2_600_000_000L;
    public const long MinimumSafetyReserveBytes = 1_500_000_000L;

    public ImageAnalysisSpeechMemoryDecision EvaluateCurrent(
        long pendingAllocationBytes = 0,
        long expectedRuntimeBytes = DefaultExpectedRuntimeBytes)
    {
        var snapshot = PhysicalMemorySnapshot.Read();
        return Evaluate(
            snapshot.AvailableBytes,
            snapshot.TotalBytes,
            expectedRuntimeBytes,
            pendingAllocationBytes);
    }

    public static ImageAnalysisSpeechMemoryDecision Evaluate(
        long availableBytes,
        long totalBytes,
        long expectedRuntimeBytes = DefaultExpectedRuntimeBytes,
        long pendingAllocationBytes = 0)
    {
        availableBytes = Math.Max(0, availableBytes);
        totalBytes = Math.Max(0, totalBytes);
        expectedRuntimeBytes = Math.Max(0, expectedRuntimeBytes);
        pendingAllocationBytes = Math.Max(0, pendingAllocationBytes);
        var safetyReserve = Math.Max(MinimumSafetyReserveBytes, totalBytes / 10);
        var required = SaturatingAdd(
            expectedRuntimeBytes,
            pendingAllocationBytes,
            safetyReserve);
        return new ImageAnalysisSpeechMemoryDecision(
            availableBytes >= required,
            availableBytes,
            expectedRuntimeBytes,
            pendingAllocationBytes,
            safetyReserve,
            required);
    }

    private static long SaturatingAdd(params long[] values)
    {
        long total = 0;
        foreach (var value in values)
        {
            if (long.MaxValue - total < value)
            {
                return long.MaxValue;
            }
            total += value;
        }
        return total;
    }

    private sealed class PhysicalMemorySnapshot
    {
        public long TotalBytes { get; init; }
        public long AvailableBytes { get; init; }

        public static PhysicalMemorySnapshot Read()
        {
            var status = new MemoryStatusEx();
            if (!GlobalMemoryStatusEx(status))
            {
                throw new InvalidOperationException("Windows did not provide the current physical-memory status.");
            }
            return new PhysicalMemorySnapshot
            {
                TotalBytes = status.TotalPhysical > long.MaxValue ? long.MaxValue : (long)status.TotalPhysical,
                AvailableBytes = status.AvailablePhysical > long.MaxValue ? long.MaxValue : (long)status.AvailablePhysical
            };
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MemoryStatusEx buffer);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private sealed class MemoryStatusEx
        {
            public uint Length = (uint)Marshal.SizeOf<MemoryStatusEx>();
            public uint MemoryLoad;
            public ulong TotalPhysical;
            public ulong AvailablePhysical;
            public ulong TotalPageFile;
            public ulong AvailablePageFile;
            public ulong TotalVirtual;
            public ulong AvailableVirtual;
            public ulong AvailableExtendedVirtual;
        }
    }
}
