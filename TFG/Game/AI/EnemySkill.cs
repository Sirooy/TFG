using Engine.Ecs;
using Cmps;
using Core;
using System.Linq;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using Engine.Core;
using TFG.Game.AI;

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

    public class MeleeAttackESkill : EnemySkill
    {
        public float Damage;
        public Color Color;

        public MeleeAttackESkill(float damage, Color color) 
        {
            this.Damage = damage;
            this.Color  = color;
        }

        public override SkillState Execute(GameWorld world,
            Entity enemy, AICmp ai)
        {
            Entity target = ai.CurrentTargets.First();

            if(world.EntityManager.TryGetComponent(target, out HealthCmp health))
            {
                health.AddHealth(-Damage);
            }

            Entity effect    = world.EntityFactory.CreateEffect(
                EffectType.Slash, target.Position);
            SpriteCmp sprite = world.EntityManager.GetComponent
                <SpriteCmp>(effect);
            sprite.Color     = Color;

            return SkillState.Finished;
        }
    }

    public class StraightDashESkill : EnemySkill
    {
        public float MaxDistance;

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

        public float MaxDistance;
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
        public float Speed;
        public float MaxDistance;
        private List<Vector2> path;
        private float travelTime;

        public PathFollowESkill(float speed, float maxDistance)
        {
            Speed       = speed;
            MaxDistance = maxDistance;
            path        = new List<Vector2>();
            travelTime  = 0.0f;
        }

        public override SkillState Execute(GameWorld world,
            Entity enemy, AICmp ai)
        {
            if(world.EntityManager.TryGetComponent(enemy, 
                out PhysicsCmp physics))
            {
                Entity target = ai.CurrentTargets.First();

                world.Level.PathFindingMap.FindPath(path,
                    enemy.Position, target.Position);

                travelTime          += world.Dt;
                float travelDistance = travelTime * Speed;
                if (travelDistance >= MaxDistance || path.Count <= 1)
                {
                    travelTime              = 0.0f;
                    physics.LinearVelocity *= 0.2f;
                    return SkillState.Finished;
                }

                AIUtil.PathFollowing(enemy, physics, path, Speed);

                return SkillState.Executing;
            }

            return SkillState.Finished;
        }
    }
}
