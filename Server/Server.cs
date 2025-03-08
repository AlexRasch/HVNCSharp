using System.Drawing;
using System.Drawing.Imaging;
using System.Net.Sockets;
using System.Runtime.InteropServices;



namespace Server
{
    internal class Server
    {

        static void Main(string[] args)
        {
            string desktopName = "HiddenDesktop_" + Guid.NewGuid().ToString();
            string host = "127.0.0.1";
            int port = 12345;

            // Starta den dolda desktoppen
            IntPtr hDesk = HiddenDesktop.StartHiddenDesktop(desktopName);
            if (hDesk == IntPtr.Zero)
            {
                Console.WriteLine("Kunde inte skapa desktop");
                return;
            }
            Console.WriteLine("Dold desktop skapad: " + desktopName);

            // Starta en tråd för att fånga och skicka skärmen
            var thread = new Thread(() =>
            {
                TcpListener server = new TcpListener(System.Net.IPAddress.Any, 12345);
                server.Start();
                Console.WriteLine("Server startad, väntar på anslutning...");
                using (TcpClient client = server.AcceptTcpClient())
                using (NetworkStream stream = client.GetStream())
                {
                    while (true)
                    {
                        Console.WriteLine("Skickar skärmbild...");
                        byte[] screenData = ScreenCapture.CaptureScreen(2560, 1440, hDesk);
                        stream.Write(BitConverter.GetBytes(screenData.Length), 0, 4);
                        stream.Write(screenData, 0, screenData.Length);
                        Thread.Sleep(1000);
                    }
                }
            });
            thread.Start();

            Console.WriteLine("Kör hidden desktop. Tryck på valfri tangent för att avsluta...");
            Console.ReadKey();
            HiddenDesktop.Cleanup(hDesk);
        }
    }
}
