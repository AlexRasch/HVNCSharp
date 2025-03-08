using System;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Poc
{
    class Program
    {
        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        private static extern IntPtr OpenDesktopA(string lpszDesktop, uint dwFlags, bool fInherit, uint dwDesiredAccess);

        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        private static extern IntPtr CreateDesktopA(string lpszDesktop, string lpszDevice, IntPtr pDevmode, uint dwFlags, uint dwDesiredAccess, IntPtr lpsa);

        [DllImport("user32.dll")]
        private static extern IntPtr GetThreadDesktop(int dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool SetThreadDesktop(IntPtr hDesktop);

        [DllImport("kernel32.dll")]
        private static extern int GetCurrentThreadId();

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
        private static extern uint ExpandEnvironmentStringsA(string lpSrc, string lpDst, uint nSize);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern int CreateProcessA(string lpApplicationName, string lpCommandLine, IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, ref STARTUPINFOA lpStartupInfo, ref PROCESS_INFORMATION lpProcessInformation);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SwitchDesktop(IntPtr hDesktop);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool GetMessage(ref MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        private const uint GENERIC_ALL = 0x10000000;
        private const uint MAX_PATH = 260;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_NOREPEAT = 0x4000;
        private const uint WM_HOTKEY = 0x0312;

        [StructLayout(LayoutKind.Sequential)]
        private struct STARTUPINFOA
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

        [StructLayout(LayoutKind.Sequential)]
        private struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public POINT pt;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        private static IntPtr CreateHiddenDesktop(string desktopName)
        {
            // Skapa en buffert för explorerPath, 
            string explorerPath = new string('\0', (int)MAX_PATH);
            IntPtr hiddenDesktop = IntPtr.Zero;
            IntPtr originalDesktop;
            STARTUPINFOA startupInfo = new STARTUPINFOA();
            PROCESS_INFORMATION processInfo = new PROCESS_INFORMATION();

            //// Expandera %windir%\explorer.exe till fullständig sökväg, den här koden funkar inte
            //uint result = ExpandEnvironmentStringsA("%windir%", explorerPath, MAX_PATH);
            //if (result == 0)
            //{
            //    Debug.WriteLine("Failed to expand environment strings: " + Marshal.GetLastWin32Error());
            //    return IntPtr.Zero;
            //}

            // Ta bort null-tecken från strängen
            explorerPath = @"C:\WINDOWS\explorer.exe"; //explorerPath.TrimEnd('\0');
            Debug.WriteLine($"Explorer Path:{explorerPath}");

            hiddenDesktop = CreateDesktopA(desktopName, null, IntPtr.Zero, 0, GENERIC_ALL, IntPtr.Zero);
            if (hiddenDesktop == IntPtr.Zero)
            {
                Debug.WriteLine("Failed to create desktop: " + Marshal.GetLastWin32Error());
                return IntPtr.Zero;
            }

            originalDesktop = GetThreadDesktop(GetCurrentThreadId());

            // Växla till det nya skrivbordet
            if (!SwitchDesktop(hiddenDesktop))
            {
                Debug.WriteLine("Failed to switch desktop: " + Marshal.GetLastWin32Error());
                CloseHandle(hiddenDesktop);
                return IntPtr.Zero;
            }

            // Sätt tråden till det nya skrivbordet
            if (!SetThreadDesktop(hiddenDesktop))
            {
                Debug.WriteLine("Failed to set thread desktop: " + Marshal.GetLastWin32Error());
                SwitchDesktop(originalDesktop);
                CloseHandle(hiddenDesktop);
                return IntPtr.Zero;
            }

            // Konfigurera STARTUPINFO
            startupInfo.cb = Marshal.SizeOf(startupInfo);
            startupInfo.lpDesktop = desktopName;

            // Skapa processen
            int success = CreateProcessA(explorerPath, null, IntPtr.Zero, IntPtr.Zero, false, 0, IntPtr.Zero, null, ref startupInfo, ref processInfo);
            if (success == 0)
            {
                Debug.WriteLine("Failed to create Explorer: " + Marshal.GetLastWin32Error());
                SetThreadDesktop(originalDesktop);
                SwitchDesktop(originalDesktop);
                CloseHandle(hiddenDesktop);
                return IntPtr.Zero;
            }

            Debug.WriteLine("Explorer started on " + desktopName + " with PID: " + processInfo.dwProcessId);
            CloseHandle(processInfo.hProcess);
            CloseHandle(processInfo.hThread);

            return hiddenDesktop;
        }

        static void Main(string[] args)
        {
            IntPtr originalDesktop = GetThreadDesktop(GetCurrentThreadId());
            IntPtr hiddenDesktop = CreateHiddenDesktop("HVNCSharp");

            if (hiddenDesktop == IntPtr.Zero)
            {
                Debug.WriteLine("Failed to create hidden desktop.");
                return;
            }

            Debug.WriteLine("Entering hidden desktop");

            // Ctrl + Alt + E
            if (RegisterHotKey(IntPtr.Zero, 1, MOD_CONTROL | MOD_ALT | MOD_NOREPEAT, 0x45))
            {
                MSG msg = new MSG();
                while (GetMessage(ref msg, IntPtr.Zero, 0, 0))
                {
                    if (msg.message == WM_HOTKEY)
                    {
                        Debug.WriteLine("Exiting hidden desktop");
                        SwitchDesktop(originalDesktop);
                        SetThreadDesktop(originalDesktop);
                        break;
                    }
                }
            }

            CloseHandle(hiddenDesktop);
            Debug.WriteLine("Press any key to exit...");
            Console.ReadLine();
        }
    }
}