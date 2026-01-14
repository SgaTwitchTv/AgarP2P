using Microsoft.Xna.Framework;
using System.Runtime.Intrinsics.X86;

namespace AgarP2P
{
    public class Camera2D
    {
        public Vector2 Position { get; private set; } = Vector2.Zero;
        public float Zoom { get; private set; } = 1.0f;
        public Matrix GetTransform()
        {
            return
                Matrix.CreateTranslation(new Vector3(-Position, 0f)) *
                Matrix.CreateScale(Zoom) *
                Matrix.CreateTranslation(new Vector3(400, 240, 0)); // Assuming screen center at (400,240)
        }

        public void Follow(Vector2 target, float lerp = 0.1f)
        {
            Position = Vector2.Lerp(Position, target, lerp);
        }

    }
}