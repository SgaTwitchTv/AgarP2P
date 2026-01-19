using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Physics;
using GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using Networking;

namespace AgarP2P
{
    public class AgarGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;  //our graphics device manager
        private SpriteBatch _spriteBatch = null!;         //rendering variable
        private PhysicsWorld _physicsWorld = null!;      //physics world
        private Player _player = null!;                 //our player
        private List<Pellet> _pellets = new();         //all the edible pellets

        private Texture2D _circleTex = null!;       //circle texture for players and pellets
        private const float MeterToPixel = 100f;

        private Camera2D _camera = null!;               //camera operator

        private double _nextPelletSpawnTime = 0;
        private Random _rand = new Random();

        private NetworkManager _network = null!;   //network manager
        private Dictionary<int, RemotePlayer> _remotePlayers = new();

        private string _targetIp = "127.0.0.1"; //default IP for client connect
        private Task? _ipInputTask;
        private bool _isReadingIp = false;

        public AgarGame()   //the constructor
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _graphics.PreferredBackBufferWidth = 1600;
            _graphics.PreferredBackBufferHeight = 900;
            _graphics.ApplyChanges();
        }

        protected override void Initialize()    //game initialization
        {
            _physicsWorld = new PhysicsWorld();
            _camera = new Camera2D(_graphics);

            _network = new NetworkManager();
            _network.OnPlayerStateReceived += OnPlayerStateReceived;
            _network.OnPlayerEatenReceived += OnPlayerEatenReceived;

            base.Initialize();
        }

        //handler for receiving other players states
        private void OnPlayerStateReceived(PlayerStateMessage msg)
        {
            if (!_remotePlayers.ContainsKey(msg.PlayerId))
            {
                _remotePlayers[msg.PlayerId] = new RemotePlayer { Id = msg.PlayerId };
            }

            _remotePlayers[msg.PlayerId].Position = new Vector2(msg.X, msg.Y);
            _remotePlayers[msg.PlayerId].Radius = msg.Radius;
        }

        private void OnPlayerEatenReceived(int killerId, int victimId)
        {
            // If a player was eaten, respawn smaller
            if (victimId == _network.LocalPlayerId)
            {
                _player = new Player(_physicsWorld.World, _circleTex, new Vector2((float)(_rand.NextDouble() * 100 - 50), (float)(_rand.NextDouble() * 100 - 50)));
                Console.WriteLine("You were eaten! Respawning...");
            }

            // Remove victim from remote players
            if (_remotePlayers.ContainsKey(victimId))
            {
                _remotePlayers.Remove(victimId);
            }
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _circleTex = CreateCircleTexture(GraphicsDevice, 32, Color.White);
            _player = new Player(_physicsWorld.World, _circleTex, new Vector2(0f, 0f));

            // Spawn initial pellets
            var random = new Random();
            for (int i = 0; i < 400; i++)
            {
                float x = (float)(random.NextDouble() * 100 - 50);
                float y = (float)(random.NextDouble() * 100 - 50);
                _pellets.Add(new Pellet(_physicsWorld.World, _circleTex, new Vector2(x, y)));
            }
        }

        //the core
        protected override void Update(GameTime gameTime)
        {
            var keyboard = Keyboard.GetState();

            // Exit game on ESC
            if (keyboard.IsKeyDown(Keys.Escape))
            {
                Exit();
            }

            // Host game
            if (keyboard.IsKeyDown(Keys.H))
            {
                _network.StartHost();
            }

            // Parallel Non-blocking IP input section 
            if (keyboard.IsKeyDown(Keys.I) && !_isReadingIp)
            {
                _isReadingIp = true;
                Task.Run(() =>
                {
                    Console.Write("\nEnter host IP: ");
                    string? ip = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(ip))
                        _targetIp = ip.Trim();
                    _isReadingIp = false;
                });
            }

            // Client connect
            if (keyboard.IsKeyDown(Keys.C))
            {
                _network.Connect(_targetIp);
            }

            // Update local player
            _player.Update(gameTime);

            // Update camera
            _camera.Follow(new Vector2(_player.body.Position.X * MeterToPixel, _player.body.Position.Y * MeterToPixel));

            // Dynamic zoom
            float baseZoom = 1.0f;
            float targetZoom = baseZoom / (1f + (_player.Radius - 0.5f) * 0.8f);
            _camera.SmoothZoom(targetZoom);

            // Handle pellets
            for (int i = _pellets.Count - 1; i >= 0; i--)
            {
                var pellet = _pellets[i];
                float dx = pellet.body.Position.X - _player.body.Position.X;
                float dy = pellet.body.Position.Y - _player.body.Position.Y;
                float distSq = dx * dx + dy * dy;

                if (distSq < _player.Radius * _player.Radius)
                {
                    _physicsWorld.World.Remove(pellet.body);
                    _pellets.RemoveAt(i);
                    _player.Grow(0.05f);
                }
            }

            // Handle remote players eating
            foreach (var kvp in _remotePlayers.ToList())
            {
                var other = kvp.Value;
                float dx = other.Position.X - _player.body.Position.X;
                float dy = other.Position.Y - _player.body.Position.Y;
                float distSq = dx * dx + dy * dy;

                if (distSq < MathF.Pow(_player.Radius + other.Radius, 2))
                {
                    if (_player.Radius > other.Radius * 1.2f)
                    {
                        _remotePlayers.Remove(other.Id);
                        _player.Grow(other.Radius * 0.3f);
                        Console.WriteLine($"Ate player {other.Id}");
                        _network.SendPlayerEaten(_network.LocalPlayerId, other.Id);
                    }
                }
            }

            // Spawn pellets randomly
            _nextPelletSpawnTime -= gameTime.ElapsedGameTime.TotalSeconds;
            if (_nextPelletSpawnTime <= 0)
            {
                for (int i = 0; i < 10; i++)
                {
                    float x = (float)(_rand.NextDouble() * 100 - 50);
                    float y = (float)(_rand.NextDouble() * 100 - 50);
                    _pellets.Add(new Pellet(_physicsWorld.World, _circleTex, new Vector2(x, y)));
                }
                _nextPelletSpawnTime = _rand.NextDouble() * 5.0;
            }

            // Step physics and network
            _physicsWorld.Step((float)gameTime.ElapsedGameTime.TotalSeconds);
            _network.PollEvents();
            _network.SendPlayerState(
                _player.body.Position.X,
                _player.body.Position.Y,
                _player.Radius);

            base.Update(gameTime);
        }


        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin(transformMatrix: _camera.GetTransform());
            var screenCenter = new Vector2(_graphics.PreferredBackBufferWidth / 2f, _graphics.PreferredBackBufferHeight / 2f);

            _player.Draw(_spriteBatch, MeterToPixel, screenCenter);
            foreach (var pellet in _pellets)
                pellet.Draw(_spriteBatch, MeterToPixel, screenCenter);

            foreach (var rp in _remotePlayers.Values)
                rp.Draw(_spriteBatch, _circleTex, MeterToPixel);

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private Texture2D CreateCircleTexture(GraphicsDevice graphicsDevice, int radius, Color color)
        {
            int diameter = radius * 2;
            Texture2D texture = new Texture2D(graphicsDevice, diameter, diameter);
            Color[] colorData = new Color[diameter * diameter];

            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    int index = y * diameter + x;
                    Vector2 position = new Vector2(x - radius, y - radius);
                    colorData[index] = position.LengthSquared() <= radius * radius ? color : Color.Transparent;
                }
            }

            texture.SetData(colorData);
            return texture;
        }
    }
}
