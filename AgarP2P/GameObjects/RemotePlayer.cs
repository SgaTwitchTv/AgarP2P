using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameObjects
{
    public class RemotePlayer
    {
        public int Id;
        public Vector2 Position;
        public float Radius;

        public void Draw(SpriteBatch sb, Texture2D tex, float meterToPixel)
        {
            var pos = Position * meterToPixel;
            float radiusPx = Radius * meterToPixel;
            var origin = new Vector2(tex.Width / 2f, tex.Height / 2f);
            sb.Draw(tex, pos, null, Color.LightGreen, 0f, origin, radiusPx / (tex.Width / 2f), SpriteEffects.None, 0f);
        }
    }
}
