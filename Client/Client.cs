using System;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using static System.Net.Mime.MediaTypeNames;

namespace Client
{
    class Client
    {
        static void Main(string[] args)
        {
            string host = "127.0.0.1";
            int port = 12345;

            Thread thread = new Thread(() =>
            {
                using (TcpClient client = new TcpClient(host, port))
                using (NetworkStream stream = client.GetStream())
                {
                    while (true)
                    {
                        byte[] sizeBuffer = new byte[4];
                        stream.Read(sizeBuffer, 0, 4);
                        int size = BitConverter.ToInt32(sizeBuffer, 0);

                        byte[] imageData = new byte[size];
                        stream.Read(imageData, 0, size);

                        File.WriteAllBytes("screenshot.jpg", imageData);
                    }
                }
            });
            thread.Start();

            Console.WriteLine("Klient körs. Tryck på valfri tangent för att avsluta...");
            Console.ReadKey();
        }
    }
}