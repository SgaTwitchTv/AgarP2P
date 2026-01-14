using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Physics;
using GameObjects;
using System.Collections.Generic;

namespace AgarP2P
{
    public class AgarGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch = null!;
        private PhysicsWorld _physicsWorld = null!;
        private Player _player = null!;
        private List<Pellet> _pellets = new();

        private Texture2D _circleTex = null!;
        private const float MeterToPixel = 100f;

        private Camera2D _camera = null;

        private double _nextPelletSpawnTime = 0;
        private Random _rand = new Random();

        public AgarGame()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _graphics.PreferredBackBufferWidth = 1600;
            _graphics.PreferredBackBufferHeight = 900;
            _graphics.ApplyChanges();
        }

        protected override void Initialize()
        {
            _physicsWorld = new PhysicsWorld();
            _camera = new Camera2D(_graphics);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _circleTex = CreateCircleTexture(GraphicsDevice, 32, Color.White);
            _player = new Player(_physicsWorld.World, _circleTex, new Vector2(0f, 0f));

            // Here we spawn initial pellets
            var random = new System.Random();
            for(int i = 0; i < 400; i++)
            {
                float x = (float)(random.NextDouble() * 100 - 50);
                float y = (float)(random.NextDouble() * 100 - 50);
                _pellets.Add(new Pellet(_physicsWorld.World, _circleTex, new Vector2(x, y)));
            }
        }

        protected override void Update(GameTime gameTime)
        {
            // Exit game on ESC
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                Exit();
            }

            // Update player input and movement
            _player.Update(gameTime);

            // Update camera to follow player
            _camera.Follow(new Vector2(
                _player.body.Position.X * MeterToPixel,
                _player.body.Position.Y * MeterToPixel));

            // Handle pellet eating (player overlaps pellets)
            for (int i = _pellets.Count - 1; i >= 0; i--)
            {
                var pellet = _pellets[i];
                float dx = pellet.body.Position.X - _player.body.Position.X;
                float dy = pellet.body.Position.Y - _player.body.Position.Y;
                float distSq = dx * dx + dy * dy;
                float eatRadius = _player.Radius; // player radius + pellet radius threshold

                if (distSq < eatRadius * eatRadius)
                {
                    // Remove pellet when eaten
                    _physicsWorld.World.Remove(pellet.body);
                    _pellets.RemoveAt(i);

                    // Player grows slightly
                    _player.Grow(0.05f);
                }
            }

            // Spawn new pellets randomly every 0–5 seconds
            _nextPelletSpawnTime -= gameTime.ElapsedGameTime.TotalSeconds;
            if (_nextPelletSpawnTime <= 0)
            {
                for (int i = 0; i < 10; i++) // spawn 5 pellets at a time
                {
                    float x = (float)(_rand.NextDouble() * 100 - 50);
                    float y = (float)(_rand.NextDouble() * 100 - 50);
                    _pellets.Add(new Pellet(_physicsWorld.World, _circleTex, new Vector2(x, y)));
                }

                _nextPelletSpawnTime = _rand.NextDouble() * 5.0; // schedule next spawn
            }

            // Step physics world
            _physicsWorld.Step((float)gameTime.ElapsedGameTime.TotalSeconds);

            base.Update(gameTime);
        }


        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin(transformMatrix: _camera.GetTransform());
            var screenCenter = new Vector2(_graphics.PreferredBackBufferWidth / 2f, _graphics.PreferredBackBufferHeight / 2f);

            _player.Draw(_spriteBatch, MeterToPixel, screenCenter);
            foreach (var pellet in _pellets)
            {
                pellet.Draw(_spriteBatch, MeterToPixel, screenCenter);
            }
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
                    if (position.LengthSquared() <= radius * radius)
                    {
                        colorData[index] = color;
                    }
                    else
                    {
                        colorData[index] = Color.Transparent;
                    }
                }
            }

            texture.SetData(colorData);
            return texture;
        }
    }
}
