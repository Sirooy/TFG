using Core;
using Physics;
using Engine.Debug;

namespace Cmps
{
    //TODO: Add collision mask/collision layer
    public class CollisionCmp
    {
        public EntityChildTransform Transform;
        public ColliderShape Collider;

        public CollisionCmp(ColliderShape shape)
        {
            DebugAssert.Success(shape != null, "Cannot create \"{0}\" with null shape",
                typeof(CollisionCmp).Name);

            this.Transform = new EntityChildTransform();
            this.Collider  = shape;
        }

        public void CacheTransform(Entity e)
        {
            Transform.CacheTransform(e);
            Collider.RecalculateBoundingAABBAndTransform(in Transform);
        }
    }
}
