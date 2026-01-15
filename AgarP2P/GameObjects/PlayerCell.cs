using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Collision.Shapes;
using PhysicsVector2 = nkast.Aether.Physics2D.Common.Vector2;

namespace GameObjects
{
    public class PlayerCell
    {
        public Body body { get; }
        private readonly Texture2D _texture;
        public float Radius { get; private set; }
        public float Mass {  get; private set; }

        public PlayerCell(World world, Texture2D texture, Vector2 startPos, float radius)
        { 
            _texture = texture;
            Radius = radius;
            Mass = radius * radius; // Example mass calculation

            body = world.CreateBody();
            body.BodyType = BodyType.Dynamic;
            body.FixedRotation = true;
            body.LinearDamping = 1.5f;

            var shape = body.CreateCircle(radius, 1f);
            shape.Friction = 0f;
            shape.Restitution = 0f;
            body.Position = new PhysicsVector2(startPos.X, startPos.Y);
        }

        public void ApplyMovement(Vector2 dir, float baseSpeed)
        {
            if(dir == Vector2.Zero)
            {
                return;
            }

            dir.Normalize();
            float impulsePower = baseSpeed / MathF.Sqrt(Mass);
            var impulse = new PhysicsVector2(dir.X * impulsePower, dir.Y * impulsePower);
            body.ApplyLinearImpulse(impulse);
        }

        public void Draw(SpriteBatch sb, float meterToPixel)
        {
            var pos = new Vector2(body.Position.X * meterToPixel, body.Position.Y * meterToPixel);
            float radiusPx = Radius * meterToPixel;
            var origin = new Vector2(_texture.Width / 2f, _texture.Height / 2f);
            sb.Draw(_texture, pos, null, Color.White, 0f, origin, radiusPx / (_texture.Width / 2f), SpriteEffects.None, 0f);
        }

        public void SetRadius(float newRadius)
        {
            Radius = newRadius;
            body.FixtureList[0].Shape.Radius = newRadius;
        }
    }
    
}