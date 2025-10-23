using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Text.Json;
using SplashKitSDK;

namespace Lerp
{
    public class Player
    {
        private Vector2 _position;
        private Vector2 _targetPosition;
        private UdpClient _udpClient;
        private float _sendTimer;
        private const float _sendRate = 0.05f;


        public Player()
        {
            _udpClient = new UdpClient(11000);
        }

        public void Draw()
        {
            SplashKit.FillCircle(Color.Black, _position.X, _position.Y, 10);
        }

        public void Update(float dt)
        {
            MoveTo(dt);

            _sendTimer += dt;
            if (_sendTimer >= _sendRate)
            {
                _sendTimer = 0;
                _ = Send();
            }
        }

        public void MoveTo(float dt)
        {
            Vector2 velocity = Vector2.Zero;
            if (SplashKit.KeyDown(KeyCode.WKey)) velocity.Y += -1;
            if (SplashKit.KeyDown(KeyCode.AKey)) velocity.X += -1;
            if (SplashKit.KeyDown(KeyCode.SKey)) velocity.Y += 1;
            if (SplashKit.KeyDown(KeyCode.DKey)) velocity.X += 1;

            if (velocity != Vector2.Zero) velocity = Vector2.Normalize(velocity);
            _position += 200 * dt * velocity;
        }

        public async Task Send()
        {
            var message = new State
            {
                X = _position.X,
                Y = _position.Y
            };

            string json = JsonSerializer.Serialize(message);
            byte[] buffer = Encoding.UTF8.GetBytes(json);

            try
            {
                IPEndPoint ep = new IPEndPoint(IPAddress.Parse("192.168.1.20"), 11000);
                await _udpClient.SendAsync(buffer, buffer.Length, ep);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Data);
            }
        }

        public async Task StartReceive()
        {
            while (true)
            {
                try
                {
                    UdpReceiveResult result = await _udpClient.ReceiveAsync();
                    string message = Encoding.UTF8.GetString(result.Buffer);

                    State? data = JsonSerializer.Deserialize<State>(message);

                    if (data != null) _targetPosition = new Vector2(data.X, data.Y);
                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Receive error: {ex.Message}");
                }
            }
        }
        
        private class State
        {
            public float X { get; set; }
            public float Y { get; set; }
        }
    }
}