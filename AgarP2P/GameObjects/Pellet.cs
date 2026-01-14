using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Collision.Shapes;
using PhysicsVector2 = nkast.Aether.Physics2D.Common.Vector2;

namespace GameObjects
{
    public class Pellet
    {
        public Body body { get; }
        private readonly Texture2D _texture;
        private readonly float _radius = 0.1f;

        public Pellet(World world, Texture2D texture, Vector2 position)
        {
            _texture = texture;

            body = world.CreateBody();
            var shape = body.CreateCircle(_radius, 0.1f);
            shape.IsSensor = true; // no collision response
            body.Position = new PhysicsVector2(position.X, position.Y);
            body.BodyType = BodyType.Static;
        }

        public void Draw(SpriteBatch spriteBatch, float meterToPixel, Vector2 screenCenter)
        {
            var pos = new Vector2(body.Position.X * meterToPixel, body.Position.Y * meterToPixel);
            float radiusPx = _radius * meterToPixel;
            var origin = new Vector2(_texture.Width / 2f, _texture.Height / 2f);
            var posPx = new Vector2(body.Position.X * meterToPixel, body.Position.Y * meterToPixel);

            spriteBatch.Draw(_texture, pos, null, Color.Green, 0f, origin, radiusPx / (_texture.Width / 2f), SpriteEffects.None, 0f);
        }
    }
}