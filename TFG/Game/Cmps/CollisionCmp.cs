using Core;
using Physics;
using Engine.Core;

namespace Cmps
{
    //TODO: Add collision mask/collision layer
    public class CollisionCmp
    {
        public EntityChildTransform Transform;
        public ColliderShape Collider;

        public CollisionCmp(ColliderShape shape)
        {
            Debug.Assert(shape != null, "Cannot create \"{0}\" with null shape",
                typeof(CollisionCmp).Name);

            this.Transform = new EntityChildTransform();
            this.Collider  = shape;
        }
    }
}
