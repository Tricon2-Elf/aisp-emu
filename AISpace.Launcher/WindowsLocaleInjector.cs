using System.Runtime.InteropServices;
using System.Text;

namespace AISpace.Launcher;

internal static class WindowsLocaleInjector
{
    private const uint CreateSuspended = 0x00000004;
    private const uint MemCommit = 0x00001000;
    private const uint MemReserve = 0x00002000;
    private const uint MemRelease = 0x00008000;
    private const uint PageReadWrite = 0x04;
    private const uint Infinite = 0xFFFFFFFF;

    public static GameLaunchResult TryLaunchWithHook(string executable, string arguments, string workingDirectory, string hookDllPath)
    {
        if (!File.Exists(hookDllPath))
            return GameLaunchResult.Failure("Locale hook DLL not found.", $"Expected '{hookDllPath}'.");

        var machine = ReadPortableExecutableMachine(executable);
        if (machine is null)
            return GameLaunchResult.Failure("Unable to read game executable architecture.", executable);

        var currentIs32Bit = IntPtr.Size == 4;
        if (machine == PortableExecutableMachine.I386 && !currentIs32Bit)
        {
            return GameLaunchResult.Failure(
                "Architecture mismatch for locale hook injection.",
                "Launcher is 64-bit but game is 32-bit. Use a 32-bit launcher build for built-in locale hook."
            );
        }

        if (machine == PortableExecutableMachine.Amd64 && currentIs32Bit)
        {
            return GameLaunchResult.Failure(
                "Architecture mismatch for locale hook injection.",
                "Launcher is 32-bit but game is 64-bit. Use a 64-bit launcher build for built-in locale hook."
            );
        }

        var startup = new StartupInfo();
        startup.cb = (uint)Marshal.SizeOf<StartupInfo>();

        if (
            !CreateProcessW(
                executable,
                $"\"{executable}\" {arguments}",
                IntPtr.Zero,
                IntPtr.Zero,
                false,
                CreateSuspended,
                IntPtr.Zero,
                workingDirectory,
                ref startup,
                out var processInfo
            )
        )
        {
            return GameLaunchResult.Failure("Failed to create game process.", new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message);
        }

        try
        {
            var injectResult = InjectDll(processInfo.hProcess, hookDllPath);
            if (!injectResult.Succeeded)
            {
                TerminateProcess(processInfo.hProcess, 1);
                return injectResult;
            }

            if (ResumeThread(processInfo.hThread) == uint.MaxValue)
            {
                TerminateProcess(processInfo.hProcess, 1);
                return GameLaunchResult.Failure("Failed to resume game process.", new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message);
            }

            return GameLaunchResult.Success(executable);
        }
        finally
        {
            if (processInfo.hThread != IntPtr.Zero)
                CloseHandle(processInfo.hThread);
            if (processInfo.hProcess != IntPtr.Zero)
                CloseHandle(processInfo.hProcess);
        }
    }

    private static GameLaunchResult InjectDll(IntPtr processHandle, string dllPath)
    {
        var dllPathBytes = Encoding.Unicode.GetBytes(dllPath + '\0');
        var remoteAddress = VirtualAllocEx(processHandle, IntPtr.Zero, (nuint)dllPathBytes.Length, MemCommit | MemReserve, PageReadWrite);
        if (remoteAddress == IntPtr.Zero)
            return GameLaunchResult.Failure("Failed to allocate memory for hook DLL path.", new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message);

        try
        {
            if (!WriteProcessMemory(processHandle, remoteAddress, dllPathBytes, (nuint)dllPathBytes.Length, out _))
                return GameLaunchResult.Failure("Failed to write hook DLL path into game process.", new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message);

            var kernel32 = GetModuleHandleW("kernel32.dll");
            if (kernel32 == IntPtr.Zero)
                return GameLaunchResult.Failure("Failed to resolve kernel32 module handle.", new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message);

            var loadLibraryW = GetProcAddress(kernel32, "LoadLibraryW");
            if (loadLibraryW == IntPtr.Zero)
                return GameLaunchResult.Failure("Failed to resolve LoadLibraryW address.", new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message);

            var remoteThread = CreateRemoteThread(processHandle, IntPtr.Zero, 0, loadLibraryW, remoteAddress, 0, out _);
            if (remoteThread == IntPtr.Zero)
                return GameLaunchResult.Failure("Failed to create remote loader thread.", new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message);

            try
            {
                WaitForSingleObject(remoteThread, Infinite);
                if (!GetExitCodeThread(remoteThread, out var exitCode))
                    return GameLaunchResult.Failure("Failed to query remote loader thread result.", new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message);
                if (exitCode == 0)
                    return GameLaunchResult.Failure("Remote LoadLibraryW returned failure.", dllPath);
            }
            finally
            {
                CloseHandle(remoteThread);
            }
        }
        finally
        {
            VirtualFreeEx(processHandle, remoteAddress, 0, MemRelease);
        }

        return new GameLaunchResult(true, string.Empty, null);
    }

    private static PortableExecutableMachine? ReadPortableExecutableMachine(string executablePath)
    {
        using var stream = File.OpenRead(executablePath);
        using var reader = new BinaryReader(stream);

        if (reader.ReadUInt16() != 0x5A4D) // MZ
            return null;
        stream.Seek(0x3C, SeekOrigin.Begin);
        var peOffset = reader.ReadInt32();
        stream.Seek(peOffset, SeekOrigin.Begin);
        if (reader.ReadUInt32() != 0x00004550) // PE\0\0
            return null;
        return (PortableExecutableMachine)reader.ReadUInt16();
    }

    private enum PortableExecutableMachine : ushort
    {
        I386 = 0x014C,
        Amd64 = 0x8664,
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct StartupInfo
    {
        public uint cb;
        public string? lpReserved;
        public string? lpDesktop;
        public string? lpTitle;
        public uint dwX;
        public uint dwY;
        public uint dwXSize;
        public uint dwYSize;
        public uint dwXCountChars;
        public uint dwYCountChars;
        public uint dwFillAttribute;
        public uint dwFlags;
        public ushort wShowWindow;
        public ushort cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessInformation
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public uint dwProcessId;
        public uint dwThreadId;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CreateProcessW(
        string lpApplicationName,
        string lpCommandLine,
        IntPtr lpProcessAttributes,
        IntPtr lpThreadAttributes,
        bool bInheritHandles,
        uint dwCreationFlags,
        IntPtr lpEnvironment,
        string lpCurrentDirectory,
        ref StartupInfo lpStartupInfo,
        out ProcessInformation lpProcessInformation
    );

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, nuint dwSize, uint flAllocationType, uint flProtect);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, nuint dwSize, uint dwFreeType);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, nuint nSize, out nuint lpNumberOfBytesWritten);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetModuleHandleW(string lpModuleName);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateRemoteThread(
        IntPtr hProcess,
        IntPtr lpThreadAttributes,
        uint dwStackSize,
        IntPtr lpStartAddress,
        IntPtr lpParameter,
        uint dwCreationFlags,
        out uint lpThreadId
    );

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetExitCodeThread(IntPtr hThread, out uint lpExitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint ResumeThread(IntPtr hThread);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);
}
