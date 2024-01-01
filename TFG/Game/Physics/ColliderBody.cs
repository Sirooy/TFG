using System;
using Engine.Debug;
using Cmps;

namespace Physics
{
    [Flags]
    public enum ColliderType
    {
        None    = 0x00,
        Static  = 0x01,
        Dynamic = 0x02,
        Trigger = 0x04,
        All     = Static | Dynamic | Trigger
    }

    [Flags]
    public enum CollisionBitmask : uint
    {
        None   = 0x00,
        Player = 1 << 0,
        Enemy  = 1 << 1,
        Wall   = 1 << 2,
        Attack = 1 << 3,
        All    = 0xFFFFFFFF
    }

    public class ColliderBody
    {
        public ColliderShape Collider;
        public CollisionBitmask CollisionLayer;
        public CollisionBitmask CollisionMask;

        public ColliderBody(ColliderShape shape, CollisionBitmask layer,
            CollisionBitmask mask)
        {
            DebugAssert.Success(shape != null, "Cannot create collider " +
                "with null shape");

            Collider       = shape;
            CollisionLayer = layer;
            CollisionMask  = mask;
        }

        public void AddCollisionLayer(CollisionBitmask layer)
        {
            CollisionLayer |= layer;
        }

        public void RemoveCollisionLayer(CollisionBitmask layer)
        {
            CollisionLayer &= (~layer);
        }

        public void AddCollisionMask(CollisionBitmask mask)
        {
            CollisionMask |= mask;
        }

        public void RemoveCollisionMask(CollisionBitmask mask)
        {
            CollisionMask &= (~mask);
        }
    }
}
