using System;
using System.Runtime.InteropServices;

namespace Microsoft.Research.Science.Jobs
{
    public class SystemInfo
    {
        public static int GetMemoryUsage()
        {
            var pi = PerformanceInformation.GetCurrent();
            return (int)(100 * pi.CommitTotal / pi.PhysicalTotal);
        }

        public static int GetProcessorUsage()
        {
            return 0; /* TODO! */
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PerformanceInformation
    {
        public int Size;
        public UInt64 CommitTotal;
        public UInt64 CommitLimit;
        public UInt64 CommitPeak;
        public UInt64 PhysicalTotal;
        public UInt64 PhysicalAvailable;
        public UInt64 SystemCache;
        public UInt64 KernelTotal;
        public UInt64 KernelPaged;
        public UInt64 KernelNonPaged;
        public UInt64 PageSize;
        public int HandlesCount;
        public int ProcessCount;
        public int ThreadCount;

        [DllImport("psapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetPerformanceInfo([Out] out PerformanceInformation PerformanceInformation, [In] int Size);

        public static PerformanceInformation GetCurrent()
        {
            PerformanceInformation pi = new PerformanceInformation();
            if (GetPerformanceInfo(out pi, Marshal.SizeOf(pi)))
                return pi;
            else
                throw new InvalidOperationException("GetPerformanceInfo returns false");
        }
    }
}