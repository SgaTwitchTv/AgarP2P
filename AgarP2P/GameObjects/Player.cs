using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Collision.Shapes;
using PhysicsVector2 = nkast.Aether.Physics2D.Common.Vector2;

namespace GameObjects
{
    public class Player
    {
        public Body body { get; }
        private readonly Texture2D _texture;
        private float _radius = 0.5f;
        private float _mass = 1f; // start mass
        private const float _baseRadius = 0.5f; // radius when mass = 1
        private readonly float _density = 1f;

        public float Radius => _radius;
        public float Mass => _mass;

        public Player(World world, Texture2D texture, Vector2 startPos)
        {
            _texture = texture;

            body = world.CreateBody();
            body.CreateCircle(_radius, 1f);
            body.Position = new PhysicsVector2(startPos.X, startPos.Y);
            body.BodyType = BodyType.Dynamic;
            body.LinearDamping = 5f;

            var shape = body.CreateCircle(_radius, _density);
            shape.Friction = 0;
            shape.Restitution = 0;

            body.Position = new PhysicsVector2(startPos.X, startPos.Y);
        }

        public void Update(GameTime gameTime)
        {
            var keyboard = Keyboard.GetState();
            var move = Vector2.Zero;

            if(keyboard.IsKeyDown(Keys.W))
            {
                move.Y -= 1;
            }
            if(keyboard.IsKeyDown(Keys.S))
            { 
                move.Y += 1; 
            }
            if(keyboard.IsKeyDown(Keys.A))
            {
                move.X -= 1; 
            }
            if (keyboard.IsKeyDown(Keys.D) )
            {
                move.X += 1;
            }

            if (move != Vector2.Zero)
            {
                move.Normalize();
                var impulsePower = 0.5f / MathF.Sqrt(MathF.Sqrt(_mass / 2.0f)); // heavier = slower
                var impulse = new PhysicsVector2(move.X * impulsePower, move.Y * impulsePower);
                body.ApplyLinearImpulse(impulse);
            }
        }

        public void Draw(SpriteBatch spriteBatch, float meterToPixel, Vector2 screenCenter)
        {
            var pos = new Vector2(body.Position.X * meterToPixel, body.Position.Y * meterToPixel);
            float radiusPx = _radius * meterToPixel;
            var origin = new Vector2(_texture.Width / 2f, _texture.Height / 2f);
            var posPx = new Vector2(body.Position.X * meterToPixel, body.Position.Y * meterToPixel);

            spriteBatch.Draw(_texture, pos, null, Color.White, 0f, origin, radiusPx / (_texture.Width / 2f), SpriteEffects.None, 0f);
        }
        public void Grow(float delta)
        {
            _mass += delta;

            // Compute new radius
            _radius = _baseRadius * MathF.Sqrt(_mass);

            // Update physics shape radius
            body.FixtureList[0].Shape.Radius = _radius;
        }
    }
}