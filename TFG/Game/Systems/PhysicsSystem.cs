using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Engine.Ecs;
using Core;
using Cmps;
using Physics;
using Engine.Debug;
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

        //BORRAR
        public static List<Vector2> contactPoints = new List<Vector2>();

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
            contactPoints.Clear();

            float dt = deltaTime / (float)iterations;

            for (int i = 1; i < iterations; ++i)
            {
                entityManager.ForEachComponent((Entity e, PhysicsCmp physics) =>
                {
                    IntegrateEntity(e, physics, dt);
                });

                CheckCollisions();
                SolveCollisions();
            }

            //Reset the force on the last iteration
            entityManager.ForEachComponent((Entity e, PhysicsCmp physics) =>
            {
                IntegrateEntity(e, physics, dt);

                physics.Force  = Vector2.Zero;
                physics.Torque = 0.0f;
            });
            CheckCollisions();
            SolveCollisions();

            DebugTimer.Stop("Physics");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IntegrateEntity(Entity e, PhysicsCmp physics, float dt)
        {
            physics.Force          += Gravity * physics.GravityMultiplier;

            physics.LinearVelocity += physics.Force * physics.InverseMass * dt;
            physics.AngularVelocity += physics.Torque * physics.InverseIntertia * dt;

            //Clamp the linear velocity before updating the position
            physics.LinearVelocity.X = Math.Clamp(physics.LinearVelocity.X,
                -physics.MaxLinearVelocity.X, physics.MaxLinearVelocity.X);
            physics.LinearVelocity.Y = Math.Clamp(physics.LinearVelocity.Y,
                -physics.MaxLinearVelocity.Y, physics.MaxLinearVelocity.Y);

            //Clamp the angular velocity before updating the rotation
            physics.AngularVelocity = Math.Clamp(physics.AngularVelocity,
                -physics.MaxAngularVelocity, physics.MaxAngularVelocity);

            e.Position += physics.LinearVelocity * dt;
            e.Rotation += physics.AngularVelocity * dt;
        }

        public void CheckCollisions()
        {
            UpdateColliderTransforms();

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

                    if (!CollisionHandler.AABBVsAABB(
                        collision1.Collider.BoundingAABB,
                        collision2.Collider.BoundingAABB))
                        continue;

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

        public void UpdateColliderTransforms()
        {
            entityManager.ForEachComponent(
                (Entity e, CollisionCmp cmp) =>
            {
                cmp.CacheTransform(e);
            });
        }

        public void SolveCollisions()
        {
            for (int i = 0; i < collisions.Count; ++i)
            {
                CollisionManifold manifold = collisions[i];
                SolveCollision(ref manifold);
                SeparateBodies(ref manifold);

                if (collisions[i].NumContacts == 1)
                    contactPoints.Add(collisions[i].Contact1);
                else if(collisions[i].NumContacts == 2)
                {
                    contactPoints.Add(collisions[i].Contact1);
                    contactPoints.Add(collisions[i].Contact2);
                }
            }

            collisions.Clear();
        }

        public void SolveCollision(ref CollisionManifold manifold)
        {
            Vector2[] contacts   = new Vector2[2] { manifold.Contact1, manifold.Contact2 };
            Vector2[] impulses = new Vector2[2];
            Vector2[] r1List = new Vector2[2];
            Vector2[] r2List = new Vector2[2];
            float inverseContacts = 1.0f / (float)manifold.NumContacts;

            for (int i = 0;i < manifold.NumContacts; ++i)
            {
                Vector2 r1 = contacts[i] - manifold.Entity1.Position;
                Vector2 r2 = contacts[i] - manifold.Entity2.Position;
                r1List[i] = r1;
                r2List[i] = r2;
                r1 = new Vector2(-r1.Y, r1.X);
                r2 = new Vector2(-r2.Y, r2.X);

                Vector2 vel1 = manifold.Physics1.LinearVelocity +
                    r1 * manifold.Physics1.AngularVelocity;
                Vector2 vel2 = manifold.Physics2.LinearVelocity +
                    r2 * manifold.Physics2.AngularVelocity;

                Vector2 relativeVel = vel1 - vel2;

                //The bodies are separating
                float velAlongNormal = Vector2.Dot(relativeVel, manifold.Normal);
                if (velAlongNormal > 0.0f)
                    continue;

                float e = (manifold.Physics1.Restitution + manifold.Physics2.Restitution)
                                * 0.5f;
                float j = -(1.0f + e) * velAlongNormal;
                float dot1 = Vector2.Dot(r1, manifold.Normal);
                float dot2 = Vector2.Dot(r2, manifold.Normal);

                j /= (manifold.Physics1.InverseMass + manifold.Physics2.InverseMass +
                    (dot1 * dot1 * manifold.Physics1.InverseIntertia) + 
                    (dot2 * dot2 * manifold.Physics2.InverseIntertia));
                j *= inverseContacts;

                impulses[i] = j * manifold.Normal;

                //angulars[i] = dot1 * j * manifold.Physics1.InverseIntertia;
                //angulars[i + 2] = dot2 * j * manifold.Physics2.InverseIntertia;
                /*
                manifold.Physics1.LinearVelocity += j * manifold.Physics1.InverseMass *
                    manifold.Normal * inverseContacts;
                manifold.Physics2.LinearVelocity -= j * manifold.Physics2.InverseMass *
                    manifold.Normal * inverseContacts;

                manifold.Physics1.AngularVelocity += dot1 * j *
                     manifold.Physics1.InverseIntertia * inverseContacts;
                manifold.Physics2.AngularVelocity -= dot2 * j *
                     manifold.Physics2.InverseIntertia * inverseContacts;
                */
            }

            for(int i = 0;i < manifold.NumContacts; ++i)
            {
                Vector2 impulse = impulses[i];

                manifold.Physics1.LinearVelocity += impulse *
                    manifold.Physics1.InverseMass;
                manifold.Physics2.LinearVelocity -= impulse *
                    manifold.Physics2.InverseMass;

                manifold.Physics1.AngularVelocity += manifold.Physics1.InverseIntertia * 
                    Util.Cross2D(r1List[i], impulse);
                manifold.Physics2.AngularVelocity -= manifold.Physics2.InverseIntertia *
                    Util.Cross2D(r2List[i], impulse);
            }
            
        }

        public void SolveCollision1(ref CollisionManifold manifold)
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
            float t1           = manifold.Physics1.InverseMass / totalInvMass;
            float t2           = manifold.Physics2.InverseMass / totalInvMass;

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
                cmp1.Transform.CachedWorldPosition,
                circle1.CachedRadius,
                cmp2.Transform.CachedWorldPosition,
                circle2.CachedRadius,
                out manifold);
        }

        public static bool CircleVsRectangle(Entity e1, CollisionCmp cmp1, 
            Entity e2, CollisionCmp cmp2, 
            out CollisionManifold manifold)
        {
            CircleCollider circle  = (CircleCollider)cmp1.Collider;
            RectangleCollider rect = (RectangleCollider)cmp2.Collider;

            bool ret = CollisionHandler.RectangleVSCircle(
                rect.Vertices,
                cmp1.Transform.CachedWorldPosition, 
                circle.CachedRadius,
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
                rect.Vertices,
                cmp2.Transform.CachedWorldPosition, 
                circle.CachedRadius,
                out manifold);

            //manifold.Normal = -manifold.Normal;

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
                cmp1.Transform.CachedWorldPosition,
                rect1.Vertices, rect1.Normals,
                cmp2.Transform.CachedWorldPosition,
                rect2.Vertices, rect2.Normals, 
                out manifold);
        }
    }
}
