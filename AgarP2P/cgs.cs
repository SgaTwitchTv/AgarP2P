using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using nkast.Aether.Physics2D.Common;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics.Contacts;
using System;
using System.Collections.Generic;

namespace AgarP2P
{
    public class Player
    {
        public Body Body { get; set; } = null!;
        public float Radius { get; set; } = 20f;
        public Vector2 Velocity;
        public string Name = "giganiga67";
        public bool IsSplit = false;
    }

    public class Camera
    {
        public Vector2 Position { get; set; } = Vector2.Zero;
        public float Zoom { get; set; } = 1f;
        public float Rotation { get; set; } = 0f;

        public Matrix GetTransformMatrix(Viewport viewport)
        {
            return Matrix.CreateTranslation(new Vector3(-Position.X, -Position.Y, 0)) *
                   Matrix.CreateRotationZ(Rotation) *
                   Matrix.CreateScale(Zoom) *
                   Matrix.CreateTranslation(new Vector3(viewport.Width * 0.5f, viewport.Height * 0.5f, 0));
        }

        public void Follow(Vector2 targetPosition)
        {
            Position = targetPosition;
            // For smoother camera: Position = Vector2.Lerp(Position, targetPosition, 0.1f);
        }
    }

    public class AgarGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        private SpriteBatch? _batch;
        private World? _physics;
        private Player? _localPlayer;
        private readonly List<Player> _players = new();
        private readonly List<Body> _pellets = new();
        private Texture2D? _circleTex;
        private readonly Camera _camera = new();
        private GameNetwork _network;

        public AgarGame()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _physics = new World(Vector2.Zero);

            SpawnLocalPlayer();
            SpawnPellets(100);

            base.Initialize();

            _network = new GameNetwork();
            base.Initialize();
        }

        private void SpawnLocalPlayer()
        {
            _localPlayer = new Player();

            // Create dynamic body + circle fixture
            _localPlayer.Body = _physics!.CreateCircle(
                radius: _localPlayer.Radius / 32f,
                density: 1f,
                position: Vector2.Zero,
                bodyType: BodyType.Dynamic
            );

            // Collision event on the fixture (not body!)
            _localPlayer.Body.FixtureList[0].OnCollision += OnPlayerCollision;

            _players.Add(_localPlayer);
        }

        private bool OnPlayerCollision(Fixture fixtureA, Fixture fixtureB, Contact contact)
        {
            if (fixtureB.Body?.Tag is "pellet")
            {
                _localPlayer!.Radius += 1f;           // Grow
                _physics!.Remove(fixtureB.Body);      // Remove pellet body
                _pellets.Remove(fixtureB.Body);
                SpawnPellets(1);                      // Respawn one
                return true;
            }

            return true; // allow collision
        }

        private void SpawnPellets(int count)
        {
            var rand = new Random();
            for (int i = 0; i < count; i++)
            {
                Vector2 pos = new(rand.Next(-2000, 2000), rand.Next(-2000, 2000));

                var pelletBody = _physics!.CreateCircle(
                    radius: 2f / 32f,
                    density: 1f,
                    position: pos,
                    bodyType: BodyType.Static
                );

                pelletBody.Tag = "pellet";
                _pellets.Add(pelletBody);
            }
        }

        private static Texture2D CreateCircleTexture(GraphicsDevice graphics, int diameter, Color color)
        {
            int radius = diameter / 2;
            var texture = new Texture2D(graphics, diameter, diameter);
            var data = new Color[diameter * diameter];

            float center = radius;
            for (int x = 0; x < diameter; x++)
            {
                for (int y = 0; y < diameter; y++)
                {
                    float distance = MathF.Sqrt(MathF.Pow(x - center, 2) + MathF.Pow(y - center, 2));
                    data[x + y * diameter] = distance <= radius ? color : Color.Transparent;
                }
            }

            texture.SetData(data);
            return texture;
        }

        protected override void LoadContent()
        {
            _batch = new SpriteBatch(GraphicsDevice);
            _circleTex = CreateCircleTexture(GraphicsDevice, 64, Color.White); // base white, tint later
        }

        protected override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Input
            var kb = Keyboard.GetState();
            if (kb.IsKeyDown(Keys.C) && kb.IsKeyUp(Keys.C)) // simple press detection
            {
                // Replace with real friend's IP:port
                // Example for same PC testing: 127.0.0.1 and friend's printed port
                _network.Connect("127.0.0.1", 54321); // ← CHANGE PORT!
            }

            Vector2 input = Vector2.Zero;

            if (kb.IsKeyDown(Keys.W)) input.Y--;
            if (kb.IsKeyDown(Keys.S)) input.Y++;
            if (kb.IsKeyDown(Keys.A)) input.X--;
            if (kb.IsKeyDown(Keys.D)) input.X++;

            if (_localPlayer != null)
            {
                _localPlayer.Velocity = input * (100f / _localPlayer.Radius); // slower when bigger
                _localPlayer.Body.LinearVelocity = _localPlayer.Velocity;

                // Simple split (just flag for now)
                if (kb.IsKeyDown(Keys.Space) && !_localPlayer.IsSplit)
                {
                    _localPlayer.IsSplit = true;
                    // TODO: implement actual split later
                }
            }

            _physics?.Step(dt);
            _network?.Poll();

            if (_localPlayer?.Body != null)
            {
                _network.SendPosition(_localPlayer.Body.Position.X, _localPlayer.Body.Position.Y, _localPlayer.Radius);
                _camera.Follow(_localPlayer.Body.Position * 32f); // pixel scale
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            if (_batch == null || _circleTex == null) return;

            var cameraMatrix = _camera.GetTransformMatrix(GraphicsDevice.Viewport);

            _batch.Begin(transformMatrix: cameraMatrix);

            // Draw pellets
            foreach (var pellet in _pellets)
            {
                _batch.Draw(
                    _circleTex,
                    pellet.Position * 32f,
                    null,
                    Color.LimeGreen,
                    0f,
                    new Vector2(32, 32),      // origin = center of 64×64 texture
                    0.0625f,                  // 2 / 32 scale
                    SpriteEffects.None,
                    0f
                );
            }

            // Draw players
            foreach (var pl in _players)
            {
                if (pl.Body == null) continue;

                float scale = pl.Radius / 32f;

                _batch.Draw(
                    _circleTex,
                    pl.Body.Position * 32f,
                    null,
                    Color.Cyan,
                    0f,
                    new Vector2(32, 32),
                    scale,
                    SpriteEffects.None,
                    0f
                );
            }

            _batch.End();

            base.Draw(gameTime);
        }
    }
}