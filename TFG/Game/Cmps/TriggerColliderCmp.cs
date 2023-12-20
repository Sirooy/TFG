using Cmps;
using Core;
using Engine.Debug;
using Physics;
using System.Collections.Generic;
using Systems;

namespace Cmps
{
    public class TriggerColliderCmp : ColliderBody
    {
        public delegate void CollisionEvent(Entity e1, TriggerColliderCmp c1,
            Entity e2, ColliderBody c2, ColliderType type, in Manifold manifold);
        public delegate void CollisionExitEvent(Entity e1, TriggerColliderCmp c1,
            Entity e2, ColliderBody c2, ColliderType type);

        public EntityChildTransform Transform;
        public event CollisionEvent OnTriggerEnter;
        public event CollisionEvent OnTriggerStay;
        public event CollisionExitEvent OnTriggerExit;
        public HashSet<ColliderBody> LastCollisions;
        public HashSet<ColliderBody> CurrentCollisions;

        public bool HasOnTriggerEnterEvent
        {
            get { return OnTriggerEnter != null; }
        }

        public bool HasOnTriggerStayEvent
        {
            get { return OnTriggerStay != null; }
        }

        public bool HasOnTriggerExitEvent
        {
            get { return OnTriggerExit != null; }
        }

        public TriggerColliderCmp(ColliderShape shape, CollisionBitmask layer,
            CollisionBitmask mask) : base(shape, layer, mask)
        {
            this.Transform         = new EntityChildTransform();
            this.OnTriggerEnter    = null;
            this.OnTriggerStay     = null;
            this.OnTriggerExit     = null;
            this.LastCollisions    = new HashSet<ColliderBody>();
            this.CurrentCollisions = new HashSet<ColliderBody>();
        }

        public TriggerColliderCmp(ColliderShape shape) : 
            this(shape, CollisionBitmask.None, CollisionBitmask.None) { }

        public void ExecuteTriggerEnterEvent(Entity e1, Entity e2, 
            ColliderBody c2, ColliderType type, in Manifold manifold)
        {
            OnTriggerEnter.Invoke(e1, this, e2, c2, type, in manifold);
        }

        public void ExecuteTriggerStayEvent(Entity e1, Entity e2,
            ColliderBody c2, ColliderType type, in Manifold manifold)
        {
            OnTriggerStay.Invoke(e1, this, e2, c2, type, in manifold);
        }

        public void ExecuteTriggerExitEvent(Entity e1, Entity e2,
            ColliderBody c2, ColliderType type)
        {
            OnTriggerExit.Invoke(e1, this, e2, c2, type);
        }

        public void CacheTransform(Entity e)
        {
            Transform.CacheTransform(e);
            Collider.RecalculateBoundingAABBAndTransform(in Transform);
        }
    }
}
