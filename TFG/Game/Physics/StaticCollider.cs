using Microsoft.Xna.Framework;
using Cmps;
using Core;

namespace Physics
{
    public sealed class StaticCollider : ColliderBody
    {
        public Material Material;
        private EntityChildTransform transform;

        public Vector2 Position
        {
            get { return transform.CachedWorldPosition; }
            set
            {
                transform.CachedWorldPosition = value;
                Collider.RecalculateBoundingAABBAndTransform(in transform);
            }
        }

        public float Rotation
        {
            get { return transform.CachedWorldRotation; }
            set
            {
                transform.CachedWorldRotation = value;
                Collider.RecalculateBoundingAABBAndTransform(in transform);
            }
        }

        public float Scale
        {
            get { return transform.CachedWorldScale; }
            set
            {
                transform.CachedWorldScale = value;
                Collider.RecalculateBoundingAABBAndTransform(in transform);
            }
        }

        public StaticCollider(ColliderShape shape, Material material,
            CollisionBitmask layer, CollisionBitmask mask) : base(shape, layer, mask)
        {
            this.transform = new EntityChildTransform();
            this.Material  = material;
        }

        public void SetTransform(Vector2 position, float rotation, float scale)
        {
            transform.CachedWorldPosition = position;
            transform.CachedWorldRotation = rotation;
            transform.CachedWorldScale = scale;
            Collider.RecalculateBoundingAABBAndTransform(in transform);
        }
    }
}
