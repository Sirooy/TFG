using System;
using Core;
using Physics;
using Systems;

namespace Cmps
{
    [Flags]
    public enum CollisionBitmask : uint
    {
        None = 0x00,
        All  = 0xFFFFFFFF
    }

    public class ColliderCmp : ColliderBody
    {
        public delegate void CollisionEvent(Entity e1, ColliderCmp c1,
            Entity e2, ColliderBody c2, ColliderType type, in Manifold manifold);

        public EntityChildTransform Transform;
        public Material Material;
        public event CollisionEvent OnCollision;

        public bool HasOnCollisionEvent    
        { 
            get { return OnCollision != null; } 
        }

        public ColliderCmp(ColliderShape shape, Material material,
            CollisionBitmask layer, CollisionBitmask mask) : base(shape, layer, mask)
        {
            this.Transform   = new EntityChildTransform();
            this.Material    = material;
            this.OnCollision = null;
        }

        public ColliderCmp(ColliderShape shape) : this(shape, Material.Zero, 
            CollisionBitmask.None, CollisionBitmask.None) { }

        public ColliderCmp(ColliderShape shape, Material material) : this(shape, 
            material, CollisionBitmask.None, CollisionBitmask.None) { }

        public ColliderCmp(ColliderShape shape, CollisionBitmask layer, 
            CollisionBitmask mask) : this(shape, Material.Zero, layer, mask) { }

        public void ExecuteCollisionEvent(Entity e1, Entity e2, 
            ColliderBody c2, ColliderType type, in Manifold manifold)
        {
            OnCollision.Invoke(e1, this, e2, c2, type, in manifold);
        }

        public void CacheTransform(Entity e)
        {
            Transform.CacheTransform(e);
            Collider.RecalculateBoundingAABBAndTransform(in Transform);
        }
    }
}
