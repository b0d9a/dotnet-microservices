using System.Runtime.InteropServices;

namespace ApiGateway.Interop;

// ── Раздел 1.4 методички: PInvoke — взаимодействие с DLL на языке C ──────────

/// <summary>
/// Структура MEMORYSTATUSEX из Win32 API (kernel32.dll).
/// StructLayout(Sequential) обязателен для маршалинга в неуправляемую память.
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
public struct MEMORYSTATUSEX
{
    public uint  dwLength;
    public uint  dwMemoryLoad;       // % of memory in use
    public ulong ullTotalPhys;       // Total physical memory bytes
    public ulong ullAvailPhys;       // Available physical memory bytes
    public ulong ullTotalPageFile;
    public ulong ullAvailPageFile;
    public ulong ullTotalVirtual;
    public ulong ullAvailVirtual;
    public ulong ullAvailExtendedVirtual;
}

/// <summary>
/// Структура SYSTEM_INFO из Win32 API (kernel32.dll).
/// Содержит информацию о процессоре и архитектуре системы.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct SYSTEM_INFO
{
    public ushort wProcessorArchitecture;   // тип архитектуры процессора
    public ushort wReserved;
    public uint   dwPageSize;               // размер страницы памяти
    public IntPtr lpMinimumApplicationAddress;
    public IntPtr lpMaximumApplicationAddress;
    public UIntPtr dwActiveProcessorMask;
    public uint   dwNumberOfProcessors;     // количество логических процессоров
    public uint   dwProcessorType;
    public uint   dwAllocationGranularity;
    public ushort wProcessorLevel;
    public ushort wProcessorRevision;
}

public static class Win32Memory
{
    // GlobalMemoryStatusEx — раздел 1.4: поле ExactSpelling=false (по умолчанию),
    // CharSet.Auto — runtime выберет ANSI или Unicode (раздел 1.4, поле CharSet)
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    // GetSystemInfo — дополнительный PInvoke: информация о CPU
    // CallingConvention.StdCall — стандартное соглашение Win32 (раздел 1.4)
    [DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall)]
    private static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);

    public static MemoryInfo GetMemoryInfo()
    {
        var status = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };

        if (!GlobalMemoryStatusEx(ref status))
            throw new InvalidOperationException("GlobalMemoryStatusEx failed: " + Marshal.GetLastWin32Error());

        return new MemoryInfo(
            MemoryLoadPercent:     status.dwMemoryLoad,
            TotalPhysicalMB:       status.ullTotalPhys   / (1024 * 1024),
            AvailablePhysicalMB:   status.ullAvailPhys   / (1024 * 1024),
            TotalPageFileMB:       status.ullTotalPageFile / (1024 * 1024),
            AvailablePageFileMB:   status.ullAvailPageFile / (1024 * 1024),
            TotalVirtualMB:        status.ullTotalVirtual  / (1024 * 1024),
            AvailableVirtualMB:    status.ullAvailVirtual  / (1024 * 1024)
        );
    }

    public static CpuInfo GetCpuInfo()
    {
        GetSystemInfo(out var info);

        var archName = info.wProcessorArchitecture switch
        {
            9  => "x64 (AMD or Intel)",
            5  => "ARM",
            12 => "ARM64",
            6  => "Itanium",
            0  => "x86",
            _  => "Unknown"
        };

        return new CpuInfo(
            Architecture:          archName,
            NumberOfProcessors:    info.dwNumberOfProcessors,
            PageSizeBytes:         info.dwPageSize,
            ProcessorLevel:        info.wProcessorLevel,
            ProcessorRevision:     info.wProcessorRevision,
            AllocationGranularity: info.dwAllocationGranularity
        );
    }
}

public record MemoryInfo(
    uint   MemoryLoadPercent,
    ulong  TotalPhysicalMB,
    ulong  AvailablePhysicalMB,
    ulong  TotalPageFileMB,
    ulong  AvailablePageFileMB,
    ulong  TotalVirtualMB,
    ulong  AvailableVirtualMB
);

public record CpuInfo(
    string Architecture,
    uint   NumberOfProcessors,
    uint   PageSizeBytes,
    ushort ProcessorLevel,
    ushort ProcessorRevision,
    uint   AllocationGranularity
);
