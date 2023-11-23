using System;
using Engine.Ecs;
using Microsoft.Xna.Framework;
using Core;
using Cmps;
using System.Collections.Generic;
using Physics;
using Engine.Core;

namespace Systems
{
    public class PhysicsSystem : Engine.Ecs.System
    {
        private delegate bool CheckCollisionFunc(
            Entity e1, CollisionCmp cmp1, 
            Entity e2, CollisionCmp cmp2, 
            out CollisionManifold manifold);

        private static readonly CheckCollisionFunc[,] CheckCollisionTable 
            = new CheckCollisionFunc
            [(int)ColliderShapeType.MaxTypes, 
            (int)ColliderShapeType.MaxTypes]
        {
            { CircleVsCircle,    CircleVsRectangle    },
            { RectangleVsCircle, RectangleVsRectangle }
        };

        public Vector2 Gravity;

        EntityManager<Entity> entityManager;
        private List<CollisionManifold> collisions;
        private float deltaTime;
        private int iterations;

        public int Iterations 
        { 
            get { return iterations; } 
            set { iterations = Math.Clamp(value, 1, 64); }
        }

        public PhysicsSystem(EntityManager<Entity> entityManager, 
            Vector2 gravity, float dt)
        {
            this.entityManager = entityManager;
            this.collisions    = new List<CollisionManifold>();
            this.Gravity       = gravity;
            this.iterations    = 1;
            this.deltaTime     = dt;
        }

        public override void Update()
        {
            DebugTimer.Start("Physics");
            for(int i = 0; i < iterations; ++i)
            {
                StepPhysics();
                CheckCollisions();
                SolveCollisions();
            }
            entityManager.ForEachComponent((Entity e, PhysicsCmp physics) =>
            {
                physics.Force = Vector2.Zero;
            });

            DebugTimer.Stop("Physics");
        }

        public void StepPhysics()
        {
            float dt = deltaTime / iterations;

            entityManager.ForEachComponent((Entity e, PhysicsCmp physics) =>
            {
                physics.Force += Gravity * physics.GravityMultiplier;
                Vector2 acceleration = physics.Force * physics.InverseMass;
                physics.LinearVelocity += acceleration * dt;

                //Clamp the velocity before updating the position
                physics.LinearVelocity.X = Math.Clamp(physics.LinearVelocity.X,
                    -physics.MaxLinearVelocity.X, physics.MaxLinearVelocity.X);
                physics.LinearVelocity.Y = Math.Clamp(physics.LinearVelocity.Y,
                    -physics.MaxLinearVelocity.Y, physics.MaxLinearVelocity.Y);

                e.Position += physics.LinearVelocity * dt;
            });
        }

        public void CheckCollisions()
        {
            var collisionCmps = entityManager.GetComponents<CollisionCmp>();
            for (int i = 0; i < collisionCmps.Count - 1; ++i)
            {
                CollisionCmp collision1 = collisionCmps[i];
                Entity e1 = entityManager.
                    GetEntity(collisionCmps.GetKey(i));

                for (int j = i + 1; j < collisionCmps.Count; ++j)
                {
                    CollisionCmp collision2 = collisionCmps[j];
                    Entity e2 = entityManager.
                        GetEntity(collisionCmps.GetKey(j));

                    if (entityManager.TryGetComponent(e1, out PhysicsCmp physics1) &&
                        entityManager.TryGetComponent(e2, out PhysicsCmp physics2))
                    {
                        if (Collides(e1, collision1, e2, collision2,
                            out CollisionManifold manifold))
                        {
                            manifold.Entity1 = e1;
                            manifold.Entity2 = e2;
                            manifold.Physics1 = physics1;
                            manifold.Physics2 = physics2;

                            if (physics1.Mass != 0.0f ||
                                physics2.Mass != 0.0f)
                            {
                                collisions.Add(manifold);
                            }
                        }
                    }
                }
            }
        }

        public void SolveCollisions()
        {
            for (int i = 0; i < collisions.Count; ++i)
            {
                CollisionManifold manifold = collisions[i];
                SolveCollision(ref manifold);
                SeparateBodies(ref manifold);
            }

            collisions.Clear();
        }

        public void SolveCollision(ref CollisionManifold manifold)
        {
            Vector2 relativeVel = manifold.Physics1.LinearVelocity -
                manifold.Physics2.LinearVelocity;

            //The bodies are separating
            float velAlongNormal = Vector2.Dot(relativeVel, manifold.Normal);
            if (velAlongNormal >= 0.0f)
                return;

            float e = (manifold.Physics1.Restitution + manifold.Physics2.Restitution)
                * 0.5f;
            float j = -(1.0f + e) * velAlongNormal;
            j      /= (manifold.Physics1.InverseMass + manifold.Physics2.InverseMass);

            manifold.Physics1.LinearVelocity += j * manifold.Physics1.InverseMass *
                manifold.Normal;
            manifold.Physics2.LinearVelocity -= j * manifold.Physics2.InverseMass *
                manifold.Normal;
        }

        public void SeparateBodies(ref CollisionManifold manifold)
        {
            float totalInvMass = manifold.Physics1.InverseMass +
                manifold.Physics2.InverseMass;
            float t1          = manifold.Physics1.InverseMass / totalInvMass;
            float t2          = manifold.Physics2.InverseMass / totalInvMass;

            manifold.Entity1.Position += (manifold.Normal * manifold.Depth * t1);
            manifold.Entity2.Position -= (manifold.Normal * manifold.Depth * t2);
        }

        public bool Collides(Entity e1, CollisionCmp cmp1, 
            Entity e2, CollisionCmp cmp2, 
            out CollisionManifold manifold)
        {
            int index1 = (int) cmp1.Collider.Type;
            int index2 = (int) cmp2.Collider.Type;

            return CheckCollisionTable[index1, index2]
                (e1, cmp1, e2, cmp2, out manifold);
        }

        public static bool CircleVsCircle(Entity e1, CollisionCmp cmp1, 
            Entity e2, CollisionCmp cmp2, out CollisionManifold manifold)
        {
            CircleCollider circle1 = (CircleCollider) cmp1.Collider;
            CircleCollider circle2 = (CircleCollider) cmp2.Collider;

            return CollisionHandler.CircleVsCircle(
                cmp1.Transform.GetWorldPosition(e1),
                circle1.Radius * cmp1.Transform.GetWorldScale(e1),
                cmp2.Transform.GetWorldPosition(e2),
                circle2.Radius * cmp2.Transform.GetWorldScale(e2),
                out manifold);
        }

        public static bool CircleVsRectangle(Entity e1, CollisionCmp cmp1, 
            Entity e2, CollisionCmp cmp2, 
            out CollisionManifold manifold)
        {
            CircleCollider circle  = (CircleCollider)cmp1.Collider;
            RectangleCollider rect = (RectangleCollider)cmp2.Collider;

            bool ret = CollisionHandler.RectangleVSCircle(
                rect.GetVertices(e2, cmp2),
                cmp1.Transform.GetWorldPosition(e1), 
                circle.Radius * cmp1.Transform.GetWorldScale(e1),
                out manifold);
            manifold.Normal = -manifold.Normal;

            return ret;
        }

        public static bool RectangleVsCircle(Entity e1, CollisionCmp cmp1, 
            Entity e2, CollisionCmp cmp2,
            out CollisionManifold manifold)
        {
            RectangleCollider rect = (RectangleCollider)cmp1.Collider;
            CircleCollider circle  = (CircleCollider)cmp2.Collider;

            bool ret = CollisionHandler.RectangleVSCircle(
                rect.GetVertices(e2, cmp2),
                cmp1.Transform.GetWorldPosition(e1), 
                circle.Radius * cmp2.Transform.GetWorldScale(e2),
                out manifold);

            manifold.Normal = -manifold.Normal;

            return ret;
        }

        public static bool RectangleVsRectangle(Entity e1, CollisionCmp cmp1,
            Entity e2, CollisionCmp cmp2,
            out CollisionManifold manifold)
        {
            RectangleCollider rect1 = (RectangleCollider)cmp1.Collider;
            RectangleCollider rect2 = (RectangleCollider)cmp2.Collider;
            manifold = default;

            return CollisionHandler.RectangleVsRectangle(
                cmp1.Transform.GetWorldPosition(e1),rect1.GetVertices(e1, cmp1),
                cmp2.Transform.GetWorldPosition(e2),rect2.GetVertices(e2, cmp2), 
                out manifold);
        }
    }
}
