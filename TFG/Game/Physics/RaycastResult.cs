
using Core;

namespace Physics
{
    public struct RaycastResult
    {
        public bool HasCollided;
        public float Distance;
        public Entity Entity;
        public ColliderBody Body;
        public ColliderType ColliderType;

        public RaycastResult()
        {
            HasCollided  = false;
            Distance     = float.MaxValue;
            Entity       = null;
            Body         = null;
            ColliderType = ColliderType.None;
        }
    }
}
