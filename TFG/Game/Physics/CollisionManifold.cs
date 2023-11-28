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
        public Vector2 Contact1;
        public Vector2 Contact2;
        public int NumContacts;

        public CollisionManifold()
        {
            Entity1     = null;
            Entity2     = null;
            Physics1    = null;
            Physics2    = null;
            Normal      = Vector2.Zero;
            Contact1    = Vector2.Zero;
            Contact2    = Vector2.Zero;
            NumContacts = 0;
            Depth       = 0.0f;
        }
    }
}
