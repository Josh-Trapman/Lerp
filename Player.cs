using System;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SplashKitSDK;

namespace Lerp
{
    public class Player
    {
        private Vector2 _position;
        private Vector2 _targetPosition;

        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private float _sendTimer;
        private const float _sendRate = 0.05f;
        private const float _moveSpeed = 200f;

        private SplashKitSDK.Timer _uptime;

        public Player(IPAddress serverIp, int serverPort)
        {
            _tcpClient = new TcpClient();
            _tcpClient.Connect(serverIp, serverPort);
            _stream = _tcpClient.GetStream();

            _uptime = new("uptime");
            _uptime.Start();
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

            // Smoothly move toward target
            Vector2 toTarget = _targetPosition - _position;
            float distance = toTarget.Length();
            if (distance > 0.01f)
            {
                toTarget /= distance;
                float moveStep = _moveSpeed * dt;
                if (moveStep > distance) moveStep = distance;
                _position += toTarget * moveStep;
            }
        }

        public void MoveTo(float dt)
        {
            Vector2 velocity = Vector2.Zero;
            if (SplashKit.KeyDown(KeyCode.WKey)) velocity.Y -= 1;
            if (SplashKit.KeyDown(KeyCode.AKey)) velocity.X -= 1;
            if (SplashKit.KeyDown(KeyCode.SKey)) velocity.Y += 1;
            if (SplashKit.KeyDown(KeyCode.DKey)) velocity.X += 1;

            if (velocity != Vector2.Zero)
            {
                velocity = Vector2.Normalize(velocity);
                _position += 200 * dt * velocity;
            }
        }

        private async Task Send()
        {
            var state = new State
            {
                X = _position.X,
                Y = _position.Y,
                TimeStamp = _uptime.Ticks / 1000.0f
            };

            string json = JsonSerializer.Serialize(state);
            byte[] data = Encoding.UTF8.GetBytes(json);
            byte[] lengthPrefix = BitConverter.GetBytes(data.Length);

            try
            {
                // Send length prefix first
                await _stream.WriteAsync(lengthPrefix, 0, lengthPrefix.Length);
                await _stream.WriteAsync(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Send error: {ex.Message}");
            }
        }

        public async Task StartReceive()
        {
            try
            {
                while (true)
                {
                    // Read length prefix (4 bytes)
                    byte[] lengthBuffer = new byte[4];
                    int read = await ReadExactAsync(lengthBuffer, 0, 4);
                    if (read == 0) break;

                    int msgLength = BitConverter.ToInt32(lengthBuffer, 0);

                    // Read the full message
                    byte[] msgBuffer = new byte[msgLength];
                    read = await ReadExactAsync(msgBuffer, 0, msgLength);
                    if (read == 0) break;

                    string json = Encoding.UTF8.GetString(msgBuffer);
                    State? state = JsonSerializer.Deserialize<State>(json);
                    if (state != null)
                    {
                        // Update target position
                        _targetPosition = new Vector2(state.X, state.Y);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Receive error: {ex.Message}");
            }
        }

        // Helper to read exact number of bytes from stream
        private async Task<int> ReadExactAsync(byte[] buffer, int offset, int count)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int read = await _stream.ReadAsync(buffer, offset + totalRead, count - totalRead);
                if (read == 0) return 0; // disconnected
                totalRead += read;
            }
            return totalRead;
        }

        private class State
        {
            public float X { get; set; }
            public float Y { get; set; }
            public double TimeStamp { get; set; }
        }
    }
}
