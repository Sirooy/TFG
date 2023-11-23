using Core;
using Cmps;
using Microsoft.Xna.Framework;

namespace Physics
{
    public struct CollisionManifold
    {
        public Entity Entity1;
        public Entity Entity2;
        public PhysicsCmp Physics1;
        public PhysicsCmp Physics2;
        public Vector2 Normal;
        public float Depth;
        public bool Collision;

        public CollisionManifold()
        {
            Entity1   = null;
            Entity2   = null;
            Physics1  = null;
            Physics2  = null;
            Normal    = Vector2.Zero;
            Depth     = 0.0f;
            Collision = false;
        }
    }
}
