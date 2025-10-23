using System;
using System.Net;
using SplashKitSDK;

namespace Lerp
{
    public class Program
    {
        public static void Main()
        {
            Window window = new("Lerp", 700, 700);
            Player player = new(IPAddress.Parse("192.168.1.24"), 0);

            SplashKitSDK.Timer delta = new("delta");
            delta.Start();

            _ = Task.Run(async () => await player.StartReceive());
            do
            {
                SplashKit.ProcessEvents();

                float dt = delta.Ticks / 1000.0f;
                delta.Reset();

                player.Update(dt);

                SplashKit.ClearScreen();
                player.Draw();

                SplashKit.RefreshScreen(60);

            } while(!window.CloseRequested);

        }
    }
}
