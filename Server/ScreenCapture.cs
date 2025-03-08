using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;

namespace Server
{
    public static class ScreenCapture
    {
        [DllImport("user32.dll")]
        private static extern bool SetThreadDesktop(IntPtr hDesktop);

        [DllImport("user32.dll")]
        private static extern IntPtr GetThreadDesktop(int dwThreadId);

        [DllImport("kernel32.dll")]
        private static extern int GetCurrentThreadId();

        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, uint dwRop);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        private const uint SRCCOPY = 0x00CC0020;

        public static byte[] CaptureScreen(int width, int height, IntPtr desktopHandle)
        {
            IntPtr originalDesktop = GetThreadDesktop(GetCurrentThreadId());
            try
            {
                if (!SetThreadDesktop(desktopHandle))
                {
                    throw new Exception("Kunde inte byta till ny desktop: " + Marshal.GetLastWin32Error());
                }

                Thread.Sleep(1000); // Vänta på att explorer laddar

                IntPtr hDesktopWnd = GetDesktopWindow();
                IntPtr hdcSrc = GetDC(hDesktopWnd);
                IntPtr hdcDest = CreateCompatibleDC(hdcSrc);
                IntPtr hBitmap = CreateCompatibleBitmap(hdcSrc, width, height);
                IntPtr hOld = SelectObject(hdcDest, hBitmap);

                BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, SRCCOPY);

                SelectObject(hdcDest, hOld);
                DeleteDC(hdcDest);
                ReleaseDC(hDesktopWnd, hdcSrc);

                using (var bitmap = Bitmap.FromHbitmap(hBitmap))
                using (var stream = new MemoryStream())
                {
                    bitmap.Save(stream, ImageFormat.Jpeg);
                    DeleteObject(hBitmap);
                    return stream.ToArray();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fel vid skärmdump: " + ex.Message);
                return new byte[0];
            }
            finally
            {
                SetThreadDesktop(originalDesktop);
            }
        }
    }
}