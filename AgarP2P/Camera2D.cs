using Microsoft.Xna.Framework;
using System.Runtime.Intrinsics.X86;

namespace AgarP2P
{
    public class Camera2D
    {
        public Vector2 Position { get; private set; } = Vector2.Zero;
        public float Zoom { get; private set; } = 1f;

        private readonly GraphicsDeviceManager _graphics;

        public Camera2D(GraphicsDeviceManager graphics)
        {
            _graphics = graphics;
        }

        public Matrix GetTransform()
        {
            var viewport = _graphics.GraphicsDevice.Viewport;
            var halfScreen = new Vector2(viewport.Width / 2f, viewport.Height / 2f);

            return
                Matrix.CreateTranslation(new Vector3(-Position + halfScreen, 0f)) *
                Matrix.CreateScale(Zoom);
        }

        public void Follow(Vector2 target, float lerp = 0.1f)
        {
            Position = Vector2.Lerp(Position, target, lerp); // Smooth camera
        }

        public void SetZoom(float zoom)
        {
            // Clamp to avoid zooming too far in/out
            Zoom = MathHelper.Clamp(zoom, 0.3f, 1.5f);
        }

        public void SmoothZoom(float targetZoom, float lerp = 0.05f)
        {
            float clampedTarget = MathHelper.Clamp(targetZoom, 0.3f, 1.5f);
            Zoom = MathHelper.Lerp(Zoom, clampedTarget, lerp);
        }
    }
}