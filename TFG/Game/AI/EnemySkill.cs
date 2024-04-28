using Engine.Ecs;
using Cmps;
using Core;
using System.Linq;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using Engine.Core;

namespace AI
{
    public enum SkillState
    {
        Executing = 0,
        Finished  = 1
    }

    public abstract class EnemySkill : DecisionTreeNode
    {
        public override sealed DecisionTreeNode Run(GameWorld world, 
            Entity enemy, AICmp ai)
        {
            return this;
        }

        public abstract SkillState Execute(GameWorld world,
            Entity enemy, AICmp ai);
    }

    public class StraightDashESkill : EnemySkill
    {
        public float MaxDistance { get; set; }

        public StraightDashESkill(float maxDistance) 
        {
            MaxDistance = maxDistance;
        }

        public override SkillState Execute(GameWorld world, 
            Entity enemy, AICmp ai)
        {
            EntityManager<Entity> entityManager = world.EntityManager;

            if (entityManager.TryGetComponent(enemy, out PhysicsCmp physics) && 
                ai.CurrentTargets.Count > 0)
            {
                float totalRadius = 0.0f;
                Entity target     = ai.CurrentTargets.First();

                if(entityManager.TryGetComponent(target, 
                    out ColliderCmp targetCollider))
                {
                    totalRadius += targetCollider.Collider.BoundingAABB.Width * 0.5f;
                }

                if (entityManager.TryGetComponent(target,
                    out ColliderCmp enemyCollider))
                {
                    totalRadius += enemyCollider.Collider.BoundingAABB.Width * 0.5f;
                }

                Vector2 dir    = target.Position - enemy.Position;
                float distance = dir.Length() - totalRadius;
                float t        = MathF.Min(distance / MaxDistance, 1.0f);

                if(dir != Vector2.Zero)
                {
                    dir.Normalize();
                    physics.LinearVelocity += dir * physics.LinearDamping * MaxDistance * t;
                }
            }

            return SkillState.Finished;
        }
    }

    public class TeleportToTargetESkill : EnemySkill
    {
        public const float FADE_TIME = 1.0f;

        private enum InternalState
        {
            CalculatingTeleportPos,
            FadingOut,
            FadingIn
        }

        public float MaxDistance { get; set; }
        private InternalState internalState;
        private Vector2 teleportPosition;
        private float fadeTimer;

        public TeleportToTargetESkill(float maxDistance)
        {
            MaxDistance      = maxDistance;
            internalState    = InternalState.CalculatingTeleportPos;
            teleportPosition = Vector2.Zero;
            fadeTimer        = FADE_TIME;
        }

        public override SkillState Execute(GameWorld world,
            Entity enemy, AICmp ai)
        {
            if (internalState == InternalState.CalculatingTeleportPos)
            {
                CalculateTeleportPosition(world, enemy, ai);

                internalState = InternalState.FadingOut;
                fadeTimer     = FADE_TIME;
            }
            else if(internalState == InternalState.FadingOut)
            {
                FadeOut(world, enemy);

                if (fadeTimer <= 0.0f)
                {
                    fadeTimer      = FADE_TIME;
                    enemy.Position = teleportPosition;
                    internalState  = InternalState.FadingIn;
                }
            }
            else if(internalState == InternalState.FadingIn)
            {
                FadeIn(world, enemy);

                if (fadeTimer <= 0.0f)
                {
                    fadeTimer     = FADE_TIME;
                    internalState = InternalState.CalculatingTeleportPos;

                    return SkillState.Finished;
                }
            }
            
            return SkillState.Executing;
        }

        private void FadeOut(GameWorld world,
            Entity enemy)
        {
            EntityManager<Entity> entityManager = world.EntityManager;

            fadeTimer -= world.Dt;
            float t    = MathF.Max(fadeTimer, 0.0f) / FADE_TIME;
            byte alpha = (byte)(255 * t);

            CharacterCmp character         = entityManager.
                GetComponent<CharacterCmp>(enemy);
            MSAItemList<SpriteCmp> sprites = entityManager.
                GetComponents<SpriteCmp>(enemy);

            character.Color.A = alpha;
            for (int i = 0; i < sprites.Count; ++i)
            {
                sprites[i].Color.A = alpha;
            }
        }

        private void FadeIn(GameWorld world,
            Entity enemy)
        {
            EntityManager<Entity> entityManager = world.EntityManager;

            fadeTimer -= world.Dt;
            float t    = 1.0f - (MathF.Max(fadeTimer, 0.0f) / FADE_TIME);
            byte alpha = (byte)(255 * t);

            CharacterCmp character         = entityManager.
                GetComponent<CharacterCmp>(enemy);
            MSAItemList<SpriteCmp> sprites = entityManager.
                GetComponents<SpriteCmp>(enemy);

            character.Color.A = alpha;
            for (int i = 0; i < sprites.Count; ++i)
            {
                sprites[i].Color.A = alpha;
            }
        }

        private void CalculateTeleportPosition(GameWorld world,
            Entity enemy, AICmp ai)
        {
            EntityManager<Entity> entityManager = world.EntityManager;

            float totalRadius = 0.0f;
            Entity target     = ai.CurrentTargets.First();

            if (entityManager.TryGetComponent(target,
                out ColliderCmp targetCollider))
            {
                totalRadius += targetCollider.Collider.BoundingAABB.Width * 0.5f;
            }

            if (entityManager.TryGetComponent(target,
                out ColliderCmp enemyCollider))
            {
                totalRadius += enemyCollider.Collider.BoundingAABB.Width * 0.5f;
            }

            Vector2 dir    = target.Position - enemy.Position;
            float distance = dir.Length() - totalRadius;
            float t        = MathF.Min(distance / MaxDistance, 1.0f);

            if (dir.IsNearlyZero())
                dir = Vector2.UnitX;
            else
                dir.Normalize();

            teleportPosition = enemy.Position + dir * MaxDistance * t;
        }
    }

    public class PathFollowESkill : EnemySkill
    {
        public float Speed { get; set; }
        private List<Vector2> path;

        public PathFollowESkill(float speed)
        {
            Speed = speed;
            path  = new List<Vector2>();
        }

        public override SkillState Execute(GameWorld world,
            Entity enemy, AICmp ai)
        {
            const float TARGET_OFFSET = 8.0f;

            EntityManager<Entity> entityManager = world.EntityManager;
            Entity target                       = ai.CurrentTargets.First();

            world.Level.PathFindingMap.FindPath(path,
                enemy.Position, target.Position);

            //Already close enough
            if(path.Count <= 2)
                return SkillState.Finished;

            Vector2 closestPos = path[0];
            Vector2 nextPos    = path[1];

            Vector2 toEnemy    = enemy.Position - closestPos;
            Vector2 direction  = nextPos - closestPos;
            direction.Normalize();

            float proj            = Vector2.Dot(toEnemy, direction);
            Vector2 pointAlongDir = closestPos + direction * proj;
            Vector2 targetPos     = pointAlongDir + direction * TARGET_OFFSET;

            if(entityManager.TryGetComponent(enemy, out PhysicsCmp physics))
            {
                Vector2 forceDir = targetPos - enemy.Position;
                if (forceDir.IsNearlyZero())
                    forceDir = Vector2.UnitX;
                else
                    forceDir.Normalize();

                physics.LinearVelocity = forceDir * Speed;
            }

            return SkillState.Executing;
        }
    }
}
