using System;
using SplashKitSDK;

namespace Lerp
{
    public class Program
    {
        public static void Main()
        {
            Window window = new("Lerp", 700, 700);
            Player player = new();

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
