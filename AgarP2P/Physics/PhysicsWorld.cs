using nkast.Aether.Physics2D.Dynamics;
using PhysicsVector2 = nkast.Aether.Physics2D.Common.Vector2;

namespace Physics
{
    public class PhysicsWorld
    {
        public World World { get; }

        public PhysicsWorld()
        {
            // Gravity (down)
            World = new World(new PhysicsVector2(0f, 0f));
        }

        public void Step(float deltaTime)
        {
            World.Step(deltaTime);
        }
    }
}
