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
        private readonly float _speed = 10f;
        
        public Player(World world, Texture2D texture, Vector2 startPos)
        {
            _texture = texture;
            _radius = MathF.Max(_radius, 0.01f);

            body = world.CreateBody();
            body.CreateCircle(_radius, 1f);
            body.Position = new PhysicsVector2(startPos.X, startPos.Y);
            body.BodyType = BodyType.Dynamic;
            body.LinearDamping = 5f;    
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
                var force = new PhysicsVector2(move.X * _speed, move.Y * _speed);
                body.ApplyForce(force);
            }
        }

        public void Draw(SpriteBatch spriteBatch, float meterToPixel, Vector2 screenCenter)
        {
            var pos = new Vector2(body.Position.X * meterToPixel, body.Position.Y * meterToPixel);
            float radiusPx = _radius * meterToPixel;
            var origin = new Vector2(_texture.Width / 2f, _texture.Height / 2f);

            spriteBatch.Draw(_texture, pos, null, Color.White, 0f, origin, radiusPx / (_texture.Width / 2f), SpriteEffects.None, 0f);
        }
        public void Grow(float delta)
        {
            _radius += delta;
            _radius = MathF.Min(_radius, 3f); // cap size
            body.FixtureList[0].Shape.Radius = _radius;
        }
    }
}