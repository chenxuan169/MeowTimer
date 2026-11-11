namespace MeowTimer.Services;

/// <summary>用 kernel32 采样 CPU / 内存占用，避免引入性能计数器包。</summary>
public static class SystemMonitor
{
    private static ulong _prevIdle, _prevKernel, _prevUser;
    private static bool _hasPrev;

    public static int CpuPercent()
    {
        try
        {
            if (!NativeMethods.GetSystemTimes(out var idle, out var kernel, out var user))
                return 0;

            ulong i = NativeMethods.ToUInt64(idle);
            ulong k = NativeMethods.ToUInt64(kernel);
            ulong u = NativeMethods.ToUInt64(user);

            if (!_hasPrev)
            {
                _prevIdle = i; _prevKernel = k; _prevUser = u; _hasPrev = true;
                return 0;
            }

            ulong idleDelta = i - _prevIdle;
            ulong totalDelta = (k - _prevKernel) + (u - _prevUser);

            _prevIdle = i; _prevKernel = k; _prevUser = u;

            if (totalDelta == 0) return 0;
            int busy = (int)(100.0 * (totalDelta - idleDelta) / totalDelta);
            return Math.Clamp(busy, 0, 100);
        }
        catch
        {
            return 0;
        }
    }

    public static int MemoryPercent()
    {
        try
        {
            var mem = new NativeMethods.MEMORYSTATUSEX();
            mem.dwLength = (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.MEMORYSTATUSEX>();
            if (NativeMethods.GlobalMemoryStatusEx(ref mem))
                return (int)mem.dwMemoryLoad;
            return 0;
        }
        catch
        {
            return 0;
        }
    }
}
