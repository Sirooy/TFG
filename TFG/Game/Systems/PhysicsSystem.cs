using System;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Engine.Ecs;
using Engine.Debug;
using Engine.Core;
using Core;
using Cmps;
using Physics;

namespace Systems
{
    public class PhysicsSystem : GameSystem
    {
        public const string DEBUG_DRAW_LAYER = "Physics";

        private class CollisionData
        {
            public Entity Entity1;
            public Entity Entity2;
            public PhysicsCmp Physics1;
            public PhysicsCmp Physics2;
            public ColliderCmp Collision1;
            public ColliderCmp Collision2;
            public Manifold Manifold;

            public CollisionData(Entity ent1, Entity ent2, 
                PhysicsCmp phy1, PhysicsCmp phy2,
                ColliderCmp col1, ColliderCmp col2,
                in Manifold manifold)
            {
                this.Entity1    = ent1;
                this.Entity2    = ent2;
                this.Physics1   = phy1;
                this.Physics2   = phy2;
                this.Collision1 = col1;
                this.Collision2 = col2;
                this.Manifold   = manifold;
            }
        }

        private class StaticCollisionData
        {
            public Entity Entity;
            public PhysicsCmp Physics;
            public ColliderCmp Collision1;
            public StaticCollider Collision2;
            public Manifold Manifold;

            public StaticCollisionData(Entity ent, PhysicsCmp phy, 
                ColliderCmp col1, StaticCollider col2,
                in Manifold manifold)
            {
                this.Entity     = ent;
                this.Physics    = phy;
                this.Collision1 = col1;
                this.Collision2 = col2;
                this.Manifold   = manifold;
            }
        }

        private class CollisionEventData
        {
            public Entity Entity1;
            public Entity Entity2;
            public ColliderCmp Collision1;
            public ColliderBody Collision2;
            public ColliderType CollisionType;
            public Manifold Manifold;

            public CollisionEventData(Entity ent1, Entity ent2,
                ColliderCmp col1, ColliderBody col2,
                ColliderType type, in Manifold manifold)
            {
                this.Entity1       = ent1;
                this.Entity2       = ent2;
                this.Collision1    = col1;
                this.Collision2    = col2;
                this.CollisionType = type;
                this.Manifold      = manifold;
            }
        }

        private class TriggerCollisionEventData
        {
            public Entity Entity1;
            public Entity Entity2;
            public TriggerColliderCmp Collision1;
            public ColliderBody Collision2;
            public ColliderType CollisionType;
            public Manifold Manifold;

            public TriggerCollisionEventData(Entity ent1, Entity ent2,
                TriggerColliderCmp col1, ColliderBody col2,
                ColliderType type, in Manifold manifold)
            {
                this.Entity1 = ent1;
                this.Entity2 = ent2;
                this.Collision1 = col1;
                this.Collision2 = col2;
                this.CollisionType = type;
                this.Manifold = manifold;
            }
        }

        private struct ResolutionData
        {
            public Vector2[] Contacts;
            public float[]   LinearImpulses;
            public Vector2[] FrictionImpulses;
            //Vectors from the center of mass to the contact point
            public Vector2[] R1Vectors;
            public Vector2[] R2Vectors;

            public ResolutionData()
            {
                Contacts         = new Vector2[2];
                LinearImpulses   = new float[2];
                FrictionImpulses = new Vector2[2];
                R1Vectors        = new Vector2[2];
                R2Vectors        = new Vector2[2];
            }
        }

        public Vector2 Gravity;

        private EntityManager<Entity> entityManager;
        private List<StaticCollider> staticColliders;
        private List<CollisionData> dynamicCollisions;
        private List<StaticCollisionData> staticCollisions;
        private List<CollisionEventData> rigidBodyEvents;
        private List<TriggerCollisionEventData> triggerEnterEvents;
        private List<TriggerCollisionEventData> triggerStayEvents;
        private List<TriggerCollisionEventData> triggerExitEvents;
        private ResolutionData resolution;
        private int iterations;

        public List<StaticCollider> StaticColliders { get { return staticColliders; } }

        public int Iterations 
        { 
            get { return iterations; } 
            set { iterations = Math.Clamp(value, 1, 64); }
        }

        public PhysicsSystem(EntityManager<Entity> entityManager, 
            Vector2 gravity, float dt)
        {
            this.entityManager = entityManager;
            this.staticColliders = new List<StaticCollider>();
            this.dynamicCollisions = new List<CollisionData>();
            this.staticCollisions = new List<StaticCollisionData>();
            this.rigidBodyEvents = new List<CollisionEventData>();
            this.triggerEnterEvents = new List<TriggerCollisionEventData>();
            this.triggerStayEvents  = new List<TriggerCollisionEventData>();
            this.triggerExitEvents  = new List<TriggerCollisionEventData>();
            this.resolution = new ResolutionData();
            this.Gravity       = gravity;
            this.iterations    = 1;
        }

        #region Static colliders management
        public StaticCollider GetStaticCollider(int index)
        {
            DebugAssert.Success(index >= 0 && index < staticColliders.Count,
                "Index ({0}) out of bounds", index);

            return staticColliders[index];
        }

        public StaticCollider AddStaticCollider(ColliderShape shape, Material material)
        {
            StaticCollider collider = new StaticCollider(shape, material, 
                CollisionBitmask.None, CollisionBitmask.None);

            staticColliders.Add(collider);

            return collider;
        }

        public StaticCollider AddStaticCollider(ColliderShape shape, 
            CollisionBitmask layer, CollisionBitmask mask)
        {
            StaticCollider collider = new StaticCollider(shape, Material.Zero,
                layer, mask);

            staticColliders.Add(collider);

            return collider;
        }

        public StaticCollider AddStaticCollider(ColliderShape shape, Material material,
            CollisionBitmask layer, CollisionBitmask mask)
        {
            StaticCollider ret = new StaticCollider(shape, material,
                layer, mask);

            staticColliders.Add(ret);

            return ret;
        }

        public void RemoveStaticCollider(int index)
        {
            DebugAssert.Success(index >= 0 && index < staticColliders.Count,
                "Index ({0}) out of bounds", index);

            staticColliders.RemoveAt(index);
        }

        public void RemoveStaticCollider(StaticCollider collider)
        {
            staticColliders.Remove(collider);
        }
        #endregion

        public override void Update(float dt)
        {
            DebugTimer.Start("Physics");
            var colCmps        = entityManager.GetComponents<ColliderCmp>();
            var triggerColCmps = entityManager.GetComponents<TriggerColliderCmp>();

            IntegrateEntities(dt);
            UpdateDynamicColliders();
            UpdateTriggerColliders();
            CheckDynamicVsStaticCollisions(colCmps, true);
            CheckDynamicVsDynamicCollisions(colCmps, true);
            CheckTriggerVsStaticCollisions(triggerColCmps);
            CheckTriggerVsDynamicCollisions(triggerColCmps, colCmps);
            CheckTriggerVsTriggerCollisions(triggerColCmps);
            DebugDrawEntityColliders();
            DebugDrawCollisionsContactPoints();
            SolveCollisions();
            ExecuteCollisionEvents();
            ExecuteTriggerCollisionEvents();
            
            for(int i = 1;i < iterations; ++i)
            {
                UpdateDynamicColliders();
                CheckDynamicVsStaticCollisions(colCmps, false);
                CheckDynamicVsDynamicCollisions(colCmps, false);
                SolveCollisions();
            }

            DebugTimer.Stop("Physics");
        }

        //Implicit euler integration
        private void IntegrateEntities(float dt)
        {
            entityManager.ForEachComponent((Entity e, PhysicsCmp physics) =>
            {
                //Calculate linear and angular velocities
                Vector2 force = physics.Force + Gravity * physics.GravityMultiplier;
                physics.LinearVelocity  += force * physics.InverseMass * dt;
                physics.AngularVelocity += physics.Torque * physics.InverseIntertia * dt;

                //Apply damping
                physics.LinearVelocity  -= physics.LinearVelocity * 
                    physics.LinearDamping * dt;
                physics.AngularVelocity -= physics.AngularVelocity * 
                    physics.AngularDamping * dt;

                //Clamp the linear velocity before updating the position
                physics.LinearVelocity.X = Math.Clamp(physics.LinearVelocity.X,
                    -physics.MaxLinearVelocity.X, physics.MaxLinearVelocity.X);
                physics.LinearVelocity.Y = Math.Clamp(physics.LinearVelocity.Y,
                    -physics.MaxLinearVelocity.Y, physics.MaxLinearVelocity.Y);

                //Clamp the angular velocity before updating the rotation
                physics.AngularVelocity = Math.Clamp(physics.AngularVelocity,
                    -physics.MaxAngularVelocity, physics.MaxAngularVelocity);
                
                //Update the position and rotation
                e.Position += physics.LinearVelocity * dt;
                e.Rotation += physics.AngularVelocity * dt;
                e.Rotation  = MathHelper.WrapAngle(e.Rotation);
                
                //Reset the forces
                physics.Force  = Vector2.Zero;
                physics.Torque = 0.0f;
            });
        }

        private void UpdateDynamicColliders()
        {
            entityManager.ForEachComponent(
                (Entity e, ColliderCmp cmp) =>
                {
                    cmp.CacheTransform(e);
                });
        }

        private void UpdateTriggerColliders()
        {
            entityManager.ForEachComponent(
                (Entity e, TriggerColliderCmp cmp) =>
                {
                    cmp.CacheTransform(e);
                    Util.Swap(ref cmp.LastCollisions, ref cmp.CurrentCollisions);
                    cmp.CurrentCollisions.Clear();
                });
        }

        #region Check Collisions Functions
        public void CheckDynamicVsStaticCollisions(
            ReadOnlyMSA<ColliderCmp> cmps, bool firstIteration)
        {
            for (int i = 0; i < cmps.Count; ++i)
            {
                ColliderCmp c1 = cmps[i];
                Entity e1       = entityManager.GetEntity(cmps.GetKey(i));

                for (int j = 0;j < staticColliders.Count; ++j)
                {
                    StaticCollider c2 = staticColliders[j];

                    if (CheckBroadPhaseCollision(c1, c2))
                        HandleNarrowPhaseCollision(e1, c1, c2, firstIteration);
                }
            }
        }

        private void CheckDynamicVsDynamicCollisions(
            ReadOnlyMSA<ColliderCmp> cmps, bool firstIteration)
        {
            for (int i = 0; i < cmps.Count - 1; ++i)
            {
                ColliderCmp c1 = cmps[i];
                Entity e1       = entityManager.GetEntity(cmps.GetKey(i));

                for (int j = i + 1; j < cmps.Count; ++j)
                {
                    ColliderCmp c2 = cmps[j];
                    Entity e2       = entityManager.GetEntity(cmps.GetKey(j));

                    if (CheckBroadPhaseCollision(c1, c2))
                        HandleNarrowPhaseCollision(e1, c1, e2, c2, firstIteration);
                }
            }
        }

        private void CheckTriggerVsStaticCollisions(
            ReadOnlyMSA<TriggerColliderCmp> cmps)
        {
            for (int i = 0; i < cmps.Count; ++i)
            {
                TriggerColliderCmp c1 = cmps[i];
                Entity e1 = entityManager.GetEntity(cmps.GetKey(i));

                for (int j = 0; j < staticColliders.Count; ++j)
                {
                    StaticCollider c2 = staticColliders[j];

                    Manifold manifold = default;
                    bool collision    = CheckBroadPhaseCollision(c1, c2) && 
                        CollisionTester.Collides(c1.Transform.CachedWorldPosition, 
                        c1.Collider, c2.Position, c2.Collider, out manifold);

                    HandleNarrowPhaseCollision(collision, in manifold, 
                        ColliderType.Static, e1, c1, null, c2);
                }
            }
        }

        private void CheckTriggerVsDynamicCollisions(
            ReadOnlyMSA<TriggerColliderCmp> cmps1,
            ReadOnlyMSA<ColliderCmp> cmps2)
        {
            for (int i = 0; i < cmps1.Count; ++i)
            {
                TriggerColliderCmp c1 = cmps1[i];
                Entity e1 = entityManager.GetEntity(cmps1.GetKey(i));

                for (int j = 0; j < cmps2.Count; ++j)
                {
                    ColliderCmp c2 = cmps2[j];
                    Entity e2 = entityManager.GetEntity(cmps2.GetKey(j));

                    Manifold manifold = default;
                    bool collision = CheckBroadPhaseCollision(c1, c2) &&
                        CollisionTester.Collides(c1.Transform.CachedWorldPosition,
                        c1.Collider, c2.Transform.CachedWorldPosition, c2.Collider, 
                        out manifold);

                    HandleNarrowPhaseCollision(collision, in manifold, 
                        ColliderType.Dynamic, e1, c1, e2, c2);
                }
            }
        }

        private void CheckTriggerVsTriggerCollisions(
            ReadOnlyMSA<TriggerColliderCmp> cmps)
        {
            for (int i = 0; i < cmps.Count - 1; ++i)
            {
                TriggerColliderCmp c1 = cmps[i];
                Entity e1 = entityManager.GetEntity(cmps.GetKey(i));

                for (int j = i + 1; j < cmps.Count; ++j)
                {
                    TriggerColliderCmp c2 = cmps[j];
                    Entity e2 = entityManager.GetEntity(cmps.GetKey(j));

                    Manifold manifold = default;
                    bool collision    = CheckBroadPhaseCollision(c1, c2) &&
                        CollisionTester.Collides(c1.Transform.CachedWorldPosition,
                        c1.Collider, c2.Transform.CachedWorldPosition, c2.Collider,
                        out manifold);

                    HandleNarrowPhaseCollision(collision, in manifold,
                        ColliderType.Trigger, e1, c1, e2, c2);
                    manifold.Normal = -manifold.Normal;
                    HandleNarrowPhaseCollision(collision, in manifold,
                        ColliderType.Trigger, e2, c2, e1, c1);
                }
            }
        }

        #endregion

        public bool CheckBroadPhaseCollision(ColliderBody c1,
            ColliderBody c2)
        {
            if ((c1.CollisionMask & c2.CollisionLayer) == CollisionBitmask.None ||
                (c1.CollisionLayer & c2.CollisionMask) == CollisionBitmask.None)
                return false;

            return CollisionTester.AABBVsAABB(
                c1.Collider.BoundingAABB,
                c2.Collider.BoundingAABB);
        }

        public void HandleNarrowPhaseCollision(Entity ent, ColliderCmp col1,
            StaticCollider col2, bool firstIteration)
        {
            if (!CollisionTester.Collides(
                col1.Transform.CachedWorldPosition, col1.Collider,
                col2.Position, col2.Collider,
                out Manifold manifold))
                return;

            if (entityManager.TryGetComponent(ent, out PhysicsCmp phy) && 
                phy.Mass != 0.0f)
            {
                staticCollisions.Add(new StaticCollisionData
                    (ent, phy, col1, col2, in manifold));
            }

            //Add collision events only in the first iteration
            if (firstIteration && col1.HasOnCollisionEvent)
            {
                rigidBodyEvents.Add(new CollisionEventData(
                    ent, null, col1, null, ColliderType.Static, in manifold));
            }
        }

        public void HandleNarrowPhaseCollision(Entity ent1, ColliderCmp col1,
            Entity ent2, ColliderCmp col2, bool firstIteration)
        {
            if (!CollisionTester.Collides(
                col1.Transform.CachedWorldPosition, col1.Collider,
                col2.Transform.CachedWorldPosition, col2.Collider,
                out Manifold manifold))
                return;

            if (entityManager.TryGetComponent(ent1, out PhysicsCmp phy1) &&
                entityManager.TryGetComponent(ent2, out PhysicsCmp phy2))
            {
                //At least one of the physics component needs to have mass
                if (phy1.Mass != 0.0f || phy2.Mass != 0.0f)
                {
                    dynamicCollisions.Add(new CollisionData
                        (ent1, ent2, phy1, phy2, col1, col2, in manifold));
                }
            }

            //Add collision events only in the first iteration
            if(firstIteration)
            {
                if (col1.HasOnCollisionEvent)
                {
                    rigidBodyEvents.Add(new CollisionEventData(
                        ent1, ent2, col1, col2, ColliderType.Dynamic, in manifold));
                }

                if (col2.HasOnCollisionEvent)
                {
                    manifold.Normal = -manifold.Normal;
                    rigidBodyEvents.Add(new CollisionEventData(
                        ent2, ent1, col2, col1, ColliderType.Dynamic, in manifold));
                }
            }
        }

        public void HandleNarrowPhaseCollision(bool collision, in Manifold manifold,
            ColliderType type, Entity ent1, TriggerColliderCmp col1, 
            Entity ent2, ColliderBody col2)
        {
            if(collision)
            {
                if (col1.LastCollisions.Contains(col2))
                {
                    col1.CurrentCollisions.Add(col2);
                    if(col1.HasOnTriggerStayEvent)
                    {
                        triggerStayEvents.Add(new TriggerCollisionEventData(ent1, ent2,
                            col1, col2, type, in manifold));
                    }
                }
                else
                {
                    col1.CurrentCollisions.Add(col2);
                    if (col1.HasOnTriggerEnterEvent)
                    {
                        triggerEnterEvents.Add(new TriggerCollisionEventData(ent1, ent2,
                            col1, col2, type, in manifold));
                    }
                }
            }
            else if(col1.LastCollisions.Contains(col2))
            {
                if (col1.HasOnTriggerExitEvent)
                {
                    triggerExitEvents.Add(new TriggerCollisionEventData(ent1, ent2,
                        col1, col2, type, in manifold));
                }
            }
        }

        public void ExecuteCollisionEvents()
        {
            foreach(var data in rigidBodyEvents)
            {
                data.Collision1.ExecuteCollisionEvent(data.Entity1, data.Entity2, 
                    data.Collision2, data.CollisionType, in data.Manifold);
            }

            rigidBodyEvents.Clear();
        }

        public void ExecuteTriggerCollisionEvents()
        {
            foreach (var data in triggerEnterEvents)
            {
                data.Collision1.ExecuteTriggerEnterEvent(data.Entity1, data.Entity2,
                    data.Collision2, data.CollisionType, in data.Manifold);
            }

            foreach (var data in triggerStayEvents)
            {
                data.Collision1.ExecuteTriggerStayEvent(data.Entity1, data.Entity2,
                    data.Collision2, data.CollisionType, in data.Manifold);
            }

            foreach (var data in triggerExitEvents)
            {
                data.Collision1.ExecuteTriggerExitEvent(data.Entity1, data.Entity2,
                    data.Collision2, data.CollisionType);
            }

            triggerEnterEvents.Clear();
            triggerStayEvents.Clear();
            triggerExitEvents.Clear();
        }

        #region Collision Solve

        public void SolveCollisions()
        {
            for (int i = 0; i < dynamicCollisions.Count; ++i)
            {
                CollisionData data = dynamicCollisions[i];
                SolveDynamicVsDynamicCollision(
                    data.Entity1, data.Entity2, data.Physics1, data.Physics2,
                    data.Collision1, data.Collision2, in data.Manifold);
                SeparateBodies(data.Entity1, data.Entity2, data.Physics1, 
                    data.Physics2, in data.Manifold);
            }

            for(int i = 0;i < staticCollisions.Count; ++i)
            {
                StaticCollisionData data = staticCollisions[i];
                SolveDynamicVsStaticCollision(data.Entity, data.Physics, 
                    data.Collision1, data.Collision2, in data.Manifold);

                data.Entity.Position += data.Manifold.Normal * data.Manifold.Depth;
            }

            dynamicCollisions.Clear();
            staticCollisions.Clear();
        }

        private void SolveDynamicVsDynamicCollision(Entity ent1, Entity ent2, 
            PhysicsCmp phy1, PhysicsCmp phy2, ColliderCmp col1, ColliderCmp col2,
            in Manifold m)
        {
            resolution.Contacts[0] = m.Contact1;
            resolution.Contacts[1] = m.Contact2;

            CalculateLinearImpulse(ent1, ent2, phy1, phy2, col1, col2, in m);
            //Apply linear impulse
            for (int i = 0;i < m.NumContacts; ++i)
            {
                Vector2 impulse = resolution.LinearImpulses[i] * m.Normal;

                phy1.LinearVelocity += impulse * phy1.InverseMass;
                phy2.LinearVelocity -= impulse * phy2.InverseMass;

                phy1.AngularVelocity += phy1.InverseIntertia *
                    MathUtil.Cross2D(resolution.R1Vectors[i], impulse);
                phy2.AngularVelocity -= phy2.InverseIntertia *
                    MathUtil.Cross2D(resolution.R2Vectors[i], impulse);
            }

            CalculateFrictionImpulse(ent1, ent2, phy1, phy2, col1, col2, in m);
            //Apply friction impulse
            for (int i = 0; i < m.NumContacts; ++i)
            {
                Vector2 impulse = resolution.FrictionImpulses[i];
                
                phy1.LinearVelocity += impulse * phy1.InverseMass;
                phy2.LinearVelocity -= impulse * phy2.InverseMass;

                phy1.AngularVelocity += phy1.InverseIntertia *
                    MathUtil.Cross2D(resolution.R1Vectors[i], impulse);
                phy2.AngularVelocity -= phy2.InverseIntertia *
                    MathUtil.Cross2D(resolution.R2Vectors[i], impulse);
            }
        }

        //Calculates the linear impulse of the colision between two dynamic bodies
        private void CalculateLinearImpulse(Entity ent1, Entity ent2, 
            PhysicsCmp phy1, PhysicsCmp phy2, ColliderCmp col1, ColliderCmp col2, 
            in Manifold m)
        {
            float e = (col1.Material.Restitution + 
                col2.Material.Restitution) * 0.5f;

            for (int i = 0; i < m.NumContacts; ++i)
            {
                resolution.LinearImpulses[i] = 0.0f;

                resolution.R1Vectors[i] = resolution.Contacts[i] - ent1.Position;
                resolution.R2Vectors[i] = resolution.Contacts[i] - ent2.Position;
                Vector2 r1 = MathUtil.Rotate90(resolution.R1Vectors[i]);
                Vector2 r2 = MathUtil.Rotate90(resolution.R2Vectors[i]);

                Vector2 vel1 = phy1.LinearVelocity + r1 * phy1.AngularVelocity;
                Vector2 vel2 = phy2.LinearVelocity + r2 * phy2.AngularVelocity;
                Vector2 relativeVel = vel1 - vel2;

                float velAlongNormal = Vector2.Dot(relativeVel, m.Normal);
                //The bodies are separating
                if (velAlongNormal > 0.0f)
                    continue;

                float j = -(1.0f + e) * velAlongNormal;
                float dot1 = Vector2.Dot(r1, m.Normal);
                float dot2 = Vector2.Dot(r2, m.Normal);

                j /= (phy1.InverseMass + phy2.InverseMass +
                    (dot1 * dot1 * phy1.InverseIntertia) +
                    (dot2 * dot2 * phy2.InverseIntertia));

                resolution.LinearImpulses[i] = j;
            }
        }

        //Calculates the friction impulse of the colision between two dynamic bodies
        private void CalculateFrictionImpulse(Entity ent1, Entity ent2,
            PhysicsCmp phy1, PhysicsCmp phy2, ColliderCmp col1, ColliderCmp col2,
            in Manifold m)
        {
            float inverseContacts = 1.0f / (float)m.NumContacts;
            float sf = (col1.Material.StaticFriction +
                col2.Material.StaticFriction) * 0.5f;
            float df = (col1.Material.DynamicFriction +
                col2.Material.DynamicFriction) * 0.5f;

            for (int i = 0; i < m.NumContacts; ++i)
            {
                resolution.FrictionImpulses[i] = Vector2.Zero;

                Vector2 r1 = MathUtil.Rotate90(resolution.R1Vectors[i]);
                Vector2 r2 = MathUtil.Rotate90(resolution.R2Vectors[i]);

                Vector2 vel1 = phy1.LinearVelocity + r1 * phy1.AngularVelocity;
                Vector2 vel2 = phy2.LinearVelocity + r2 * phy2.AngularVelocity;
                Vector2 relativeVel = vel1 - vel2;

                Vector2 tangent = relativeVel - Vector2.Dot(relativeVel,
                    m.Normal) * m.Normal;

                if (tangent == Vector2.Zero) continue;
                tangent.Normalize();

                float jf   = -Vector2.Dot(relativeVel, tangent);
                float dot1 = Vector2.Dot(r1, tangent);
                float dot2 = Vector2.Dot(r2, tangent);

                jf /= (phy1.InverseMass + phy2.InverseMass +
                    (dot1 * dot1 * phy1.InverseIntertia) +
                    (dot2 * dot2 * phy2.InverseIntertia));
                jf *= inverseContacts;

                float j = resolution.LinearImpulses[i];
                if (MathF.Abs(jf) <= j * sf)
                    resolution.FrictionImpulses[i] = jf * tangent;
                else
                    resolution.FrictionImpulses[i] = -j * tangent * df;
            }
        }

        private void SolveDynamicVsStaticCollision(Entity ent, PhysicsCmp phy,
            ColliderCmp col1, StaticCollider col2, in Manifold m)
        {
            resolution.Contacts[0] = m.Contact1;
            resolution.Contacts[1] = m.Contact2;

            CalculateLinearImpulse(ent, phy, col1, col2, in m);
            //Apply linear impulse
            for (int i = 0; i < m.NumContacts; ++i)
            {
                Vector2 impulse = resolution.LinearImpulses[i] * m.Normal;

                phy.LinearVelocity  += impulse * phy.InverseMass;
                phy.AngularVelocity += phy.InverseIntertia *
                    MathUtil.Cross2D(resolution.R1Vectors[i], impulse);
            }

            CalculateFrictionImpulse(ent, phy, col1, col2, in m);
            //Apply the frictiom impulse
            for (int i = 0; i < m.NumContacts; ++i)
            {
                Vector2 impulse = resolution.FrictionImpulses[i];

                phy.LinearVelocity  += impulse * phy.InverseMass;
                phy.AngularVelocity += phy.InverseIntertia *
                    MathUtil.Cross2D(resolution.R1Vectors[i], impulse);
            }
        }

        //Calculates the linear impulse of the colision between a dynamic and a static body
        private void CalculateLinearImpulse(Entity ent, PhysicsCmp phy,
            ColliderCmp col1, StaticCollider col2, in Manifold m)
        {
            float e = (col1.Material.Restitution + 
                col2.Material.Restitution) * 0.5f;

            for (int i = 0; i < m.NumContacts; ++i)
            {
                resolution.LinearImpulses[i] = 0.0f;

                resolution.R1Vectors[i] = resolution.Contacts[i] - ent.Position;
                Vector2 r = MathUtil.Rotate90(resolution.R1Vectors[i]);

                Vector2 relativeVel = phy.LinearVelocity + r * phy.AngularVelocity;

                float velAlongNormal = Vector2.Dot(relativeVel, m.Normal);
                //The bodies are separating
                if (velAlongNormal > 0.0f)
                    continue;

                float j = -(1.0f + e) * velAlongNormal;
                float dot1 = Vector2.Dot(r, m.Normal);

                j /= (phy.InverseMass + dot1 * dot1 * phy.InverseIntertia);

                resolution.LinearImpulses[i] = j;
            }
        }

        //Calculates the friction impulse of the colision between a dynamic and a static body
        private void CalculateFrictionImpulse(Entity ent, PhysicsCmp phy,
            ColliderCmp col1, StaticCollider col2, in Manifold m)
        {
            float inverseContacts = 1.0f / (float)m.NumContacts;
            float sf = (col1.Material.StaticFriction + 
                col2.Material.StaticFriction) * 0.5f;
            float df = (col1.Material.DynamicFriction + 
                col2.Material.DynamicFriction) * 0.5f;

            for (int i = 0; i < m.NumContacts; ++i)
            {
                resolution.FrictionImpulses[i] = Vector2.Zero;

                Vector2 r = MathUtil.Rotate90(resolution.R1Vectors[i]);

                Vector2 relativeVel = phy.LinearVelocity + r * phy.AngularVelocity;
                Vector2 tangent = relativeVel - Vector2.Dot(relativeVel,
                    m.Normal) * m.Normal;

                if (tangent == Vector2.Zero)
                    continue;
                tangent.Normalize();

                float jf = -Vector2.Dot(relativeVel, tangent);
                float dot1 = Vector2.Dot(r, tangent);

                jf /= (phy.InverseMass + dot1 * dot1 * phy.InverseIntertia);
                jf *= inverseContacts;

                float j = resolution.LinearImpulses[i];
                if (MathF.Abs(jf) <= j * sf)
                    resolution.FrictionImpulses[i] = jf * tangent;
                else
                    resolution.FrictionImpulses[i] = -j * tangent * df;
            }
        }

        public void SeparateBodies(Entity ent1, Entity ent2, 
            PhysicsCmp phy1, PhysicsCmp phy2, in Manifold manifold)
        {
            float totalInvMass = 1.0f / (phy1.InverseMass +
                                         phy2.InverseMass);
            float t1 = phy1.InverseMass * totalInvMass;
            float t2 = phy2.InverseMass * totalInvMass;

            ent1.Position += (manifold.Normal * manifold.Depth * t1);
            ent2.Position -= (manifold.Normal * manifold.Depth * t2);
        }

        #endregion

        #region Raycast

        public RaycastResult Raycast(Vector2 rayStart, Vector2 rayDir, 
            ColliderType colliderType, CollisionBitmask layer = CollisionBitmask.All)
        {
            RaycastResult result = new RaycastResult();
            
            if((colliderType & ColliderType.Static) == ColliderType.Static)
            {
                RaycastVsStaticColliders(rayStart, rayDir, layer, ref result);
            }

            if ((colliderType & ColliderType.Dynamic) == ColliderType.Dynamic)
            {
                RaycastVsDynamicColliders(rayStart, rayDir, layer, ref result);
            }

            if ((colliderType & ColliderType.Trigger) == ColliderType.Trigger)
            {
                RaycastVsTriggerColliders(rayStart, rayDir, layer, ref result);
            }

            return result;
        }

        private void RaycastVsStaticColliders(Vector2 rayStart, Vector2 rayDir,
            CollisionBitmask layer, ref RaycastResult result)
        {
            foreach(StaticCollider col in staticColliders)
            {
                if ((layer & col.CollisionLayer) == CollisionBitmask.None) 
                    continue;

                if(CollisionTester.RayCollides(rayStart, rayDir, 
                    col.Position, col.Collider, out float distance))
                {
                    if(distance < result.Distance)
                    {
                        result.Body         = col;
                        result.Entity       = null;
                        result.Distance     = distance;
                        result.ColliderType = ColliderType.Static;
                        result.HasCollided  = true;
                    }
                }
            }
        }

        private void RaycastVsDynamicColliders(Vector2 rayStart, Vector2 rayDir,
            CollisionBitmask layer, ref RaycastResult result)
        {
            var cmps = entityManager.GetComponents<ColliderCmp>();

            for(int i = 0;i < cmps.Count; ++i)
            {
                ColliderCmp col = cmps[i];

                if ((layer & col.CollisionLayer) == CollisionBitmask.None)
                    continue;

                if(CollisionTester.RayCollides(rayStart, rayDir, 
                    col.Transform.CachedWorldPosition, col.Collider, 
                    out float distance))
                {
                    if(distance < result.Distance)
                    {
                        result.Body         = col;
                        result.Entity       = entityManager.GetEntity(cmps.GetKey(i));
                        result.Distance     = distance;
                        result.ColliderType = ColliderType.Dynamic;
                        result.HasCollided  = true;
                    }
                }
            }
        }

        private void RaycastVsTriggerColliders(Vector2 rayStart, Vector2 rayDir,
            CollisionBitmask layer, ref RaycastResult result)
        {
            var cmps = entityManager.GetComponents<TriggerColliderCmp>();

            for (int i = 0; i < cmps.Count; ++i)
            {
                TriggerColliderCmp col = cmps[i];

                if ((layer & col.CollisionLayer) == CollisionBitmask.None)
                    continue;

                if (CollisionTester.RayCollides(rayStart, rayDir,
                    col.Transform.CachedWorldPosition, col.Collider,
                    out float distance))
                {
                    if (distance < result.Distance)
                    {
                        result.Body         = col;
                        result.Entity       = entityManager.GetEntity(cmps.GetKey(i));
                        result.Distance     = distance;
                        result.ColliderType = ColliderType.Trigger;
                        result.HasCollided  = true;
                    }
                }
            }
        }
        #endregion

        #region Debug Draw

        [Conditional(DebugDraw.DEBUG_DEFINE)]
        private void DebugDrawEntityColliders()
        {
            if (!DebugDraw.IsLayerEnabled(DEBUG_DRAW_LAYER)) return;

            //Static colliders
            foreach(StaticCollider collider in staticColliders)
            {
                DebugDrawCollider(collider.Position, collider.Rotation,
                    collider.Scale, collider.Collider, Color.Gray);
            }

            //Rigid colliders
            entityManager.ForEachComponent((ColliderCmp col) =>
            {
                DebugDrawCollider(col.Transform.CachedWorldPosition,
                    col.Transform.CachedWorldRotation,
                    col.Transform.CachedWorldScale,
                    col.Collider, Color.Yellow);
            });

            //Trigger colliders
            entityManager.ForEachComponent((TriggerColliderCmp col) =>
            {
                DebugDrawCollider(col.Transform.CachedWorldPosition,
                    col.Transform.CachedWorldRotation,
                    col.Transform.CachedWorldScale,
                    col.Collider, Color.Cyan);
            });
        }

        [Conditional(DebugDraw.DEBUG_DEFINE)]
        private void DebugDrawCollider(Vector2 pos, float rotation, float scale,
            ColliderShape shape, Color color)
        {
            if(shape.Type == ColliderShapeType.Circle)
            {
                CircleCollider circle = (CircleCollider) shape;
                DebugDraw.Circle(DEBUG_DRAW_LAYER, pos, circle.CachedRadius, 
                    rotation, color);
            }
            else if(shape.Type == ColliderShapeType.Rectangle)
            {
                RectangleCollider rect = (RectangleCollider) shape;
                Vector2 size           = new Vector2(rect.Width * scale, 
                    rect.Height * scale);
                DebugDraw.CenteredRect(DEBUG_DRAW_LAYER, pos, size, 
                    rotation, color);
            }
        }

        [Conditional(DebugDraw.DEBUG_DEFINE)]
        private void DebugDrawCollisionsContactPoints()
        {
            if (!DebugDraw.IsLayerEnabled(DEBUG_DRAW_LAYER)) return;

            foreach (StaticCollisionData col in staticCollisions)
            {
                DebugDrawContactPoints(in col.Manifold, Color.Red);
            }

            foreach(CollisionData col in dynamicCollisions)
            {
                DebugDrawContactPoints(in col.Manifold, Color.Red);
            }

            foreach(TriggerCollisionEventData col in triggerEnterEvents)
            {
                DebugDrawContactPoints(in col.Manifold, Color.Blue);
            }

            foreach (TriggerCollisionEventData col in triggerStayEvents)
            {
                DebugDrawContactPoints(in col.Manifold, Color.Blue);
            }

            foreach (TriggerCollisionEventData col in triggerExitEvents)
            {
                DebugDrawContactPoints(in col.Manifold, Color.Blue);
            }
        }

        [Conditional(DebugDraw.DEBUG_DEFINE)]
        private void DebugDrawContactPoints(in Manifold m, Color color)
        {
            if(m.NumContacts > 0)
            {
                DebugDraw.Point(DEBUG_DRAW_LAYER, m.Contact1, color);
            }

            if(m.NumContacts > 1)
            {
                DebugDraw.Point(DEBUG_DRAW_LAYER, m.Contact2, color);
            }
        }

        #endregion
    }
}
