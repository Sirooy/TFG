using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Engine.Core;
using Engine.Ecs;
using Cmps;
using Core;
using System;

namespace AI
{
    public class AIUtil
    {
        public static void PathFollowing(Entity e, PhysicsCmp physics, 
            List<Vector2> path, float speed)
        {
            const float TARGET_OFFSET = 8.0f;

            Vector2 targetPos;
            if(path.Count == 1)
            {
                targetPos = path[0];
            }
            else
            {
                Vector2 closestPos = path[0];
                Vector2 nextPos    = path[1];

                Vector2 toEntity   = e.Position - closestPos;
                Vector2 direction  = nextPos - closestPos;
                direction.Normalize();

                float proj            = Vector2.Dot(toEntity, direction);
                Vector2 pointAlongDir = closestPos + direction * proj;
                targetPos             = pointAlongDir + direction * TARGET_OFFSET;
            }

            Vector2 forceDir = targetPos - e.Position;
            if (forceDir.IsNearlyZero())
                forceDir = Vector2.UnitX;
            else
                forceDir.Normalize();

            physics.LinearVelocity = forceDir * speed;
        }

        public static void SetEntityDashVelocity(EntityManager<Entity> entityManager, 
            Entity e, Vector2 direction, float distance)
        {
            PhysicsCmp phy = entityManager.GetComponent<PhysicsCmp>(e);
            phy.LinearVelocity += direction * distance *
                phy.LinearDamping;
        }

        public static void SetProjectilePosAndVel(EntityManager<Entity> entityManager,
            Entity e, Vector2 basePosition, Vector2 direction, 
            float speed, float offset = 8.0f)
        {
            SetProjectilePosition(entityManager, e, basePosition, direction, 8.0f);
            SetProjectileVelocity(entityManager, e, direction * speed);
        }

        public static void SetProjectileVelocity(EntityManager<Entity> entityManager,
            Entity e, Vector2 velocity)
        {
            PhysicsCmp physics     = entityManager.GetComponent<PhysicsCmp>(e);
            physics.LinearVelocity = velocity;

            e.Rotation = MathF.Atan2(velocity.Y, velocity.X);
        }

        public static void SetProjectilePosition(EntityManager<Entity> entityManager,
            Entity e, Vector2 basePosition, Vector2 direction, float offset = 8.0f)
        {
            if(entityManager.TryGetComponent(e, out TriggerColliderCmp trigger))
            {
                trigger.Transform.CacheTransform(e);
                trigger.Collider.RecalculateBoundingAABBAndTransform(trigger.Transform);
                e.Position = basePosition + direction * 
                    (trigger.Collider.BoundingAABB.Width * 0.5f + offset);
            }
            else if(entityManager.TryGetComponent(e, out ColliderCmp col))
            {
                col.Transform.CacheTransform(e);
                col.Collider.RecalculateBoundingAABBAndTransform(col.Transform);
                e.Position = basePosition + direction *
                    (col.Collider.BoundingAABB.Width * 0.5f + offset);
            }
        }

        public static void FadeOut(GameWorld world,
            Entity enemy, float fadeTime, ref float fadeTimer)
        {
            fadeTimer -= world.Dt;
            float t    = MathF.Max(fadeTimer, 0.0f) / fadeTime;
            byte alpha = (byte)(255 * t);

            AIUtil.SetEntityAlpha(world.EntityManager, enemy, alpha);
        }

        public static void FadeIn(GameWorld world,
            Entity enemy, float fadeTime, ref float fadeTimer)
        {
            fadeTimer -= world.Dt;
            float t = 1.0f - (MathF.Max(fadeTimer, 0.0f) / fadeTime);
            byte alpha = (byte)(255 * t);

            AIUtil.SetEntityAlpha(world.EntityManager, enemy, alpha);
        }

        public static void SetEntityAlpha(EntityManager<Entity> entityManager,
            Entity e, byte alpha)
        {
            CharacterCmp character = entityManager.
                GetComponent<CharacterCmp>(e);
            MSAItemList<SpriteCmp> sprites = entityManager.
                GetComponents<SpriteCmp>(e);

            character.Color.A = alpha;
            for (int i = 0; i < sprites.Count; ++i)
            {
                sprites[i].Color.A = alpha;
            }
        }
    }
}
