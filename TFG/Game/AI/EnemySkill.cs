using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Engine.Ecs;
using Engine.Core;
using Physics;
using Cmps;
using Core;


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
            if (ai.CurrentTargets.Count == 0) return SkillState.Finished;

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

    public class HealESkill : EnemySkill
    {
        public float Amount;

        public HealESkill(float amount)
        {
            this.Amount = amount;
        }

        public override SkillState Execute(GameWorld world,
            Entity enemy, AICmp ai)
        {
            if (ai.CurrentTargets.Count == 0) return SkillState.Finished;

            Entity target = ai.CurrentTargets.First();

            if (world.EntityManager.TryGetComponent(target, out HealthCmp health))
            {
                health.AddHealth(Amount);
            }

            world.EntityFactory.CreateEffect(EffectType.Health, target.Position);

            return SkillState.Finished;
        }
    }

    public class AttackESkill : EnemySkill
    {
        public AttackType Type;
        public float Damage;
        public float Knockback;
        public float Radius;
        public bool ExecuteOnTarget;
        public CollisionBitmask Mask;

        public AttackESkill(AttackType type, float damage, float knockback, float radius,
            bool executeOnTarget = false, CollisionBitmask mask = CollisionBitmask.Player)
        {
            this.Type            = type;
            this.Damage          = damage;
            this.Knockback       = knockback;
            this.Radius          = radius;
            this.ExecuteOnTarget = executeOnTarget;
            this.Mask            = mask;
        }

        public override SkillState Execute(GameWorld world,
            Entity enemy, AICmp ai)
        {
            if (ai.CurrentTargets.Count == 0) return SkillState.Finished;

            Entity target = ai.CurrentTargets.First();
            Vector2 direction = Vector2.Normalize(target.Position - enemy.Position);
            Vector2 position = enemy.Position;
            if (ExecuteOnTarget)
                position = target.Position;

            Entity attack = world.EntityFactory.CreateAttack(Type, position,
                Damage, Knockback, Mask);
            TriggerColliderCmp collider = world.EntityManager.
                    GetComponent<TriggerColliderCmp>(attack);
            CircleCollider circle = (CircleCollider)collider.Collider;
            attack.Scale = Radius / circle.Radius;

            AIUtil.SetProjectileDirection(world.EntityManager, attack, direction);

            return SkillState.Finished;
        }
    }

    public class ProjectileAttackESkill : EnemySkill
    {
        public AttackType Type;
        public float Damage;
        public float Knockback;
        public float Speed;
        public CollisionBitmask Mask;

        public ProjectileAttackESkill(AttackType type,  float damage, float knockback, 
            float speed, CollisionBitmask mask = CollisionBitmask.Player)
        {
            this.Type      = type;
            this.Damage    = damage;
            this.Knockback = knockback;
            this.Speed     = speed;
            this.Mask      = mask;
        }

        public override SkillState Execute(GameWorld world,
            Entity enemy, AICmp ai)
        {
            if(ai.CurrentTargets.Count == 0) return SkillState.Finished;

            Entity target     = ai.CurrentTargets.First();
            Vector2 direction = Vector2.Normalize(target.Position - enemy.Position);

            Entity attack = world.EntityFactory.CreateAttack(Type, target.Position, 
                Damage, Knockback, Mask);

            AIUtil.SetProjectilePosAndVel(world.EntityManager, attack,
                enemy.Position, direction, Speed);

            return SkillState.Finished;
        }
    }

    public class DashESkill : EnemySkill
    {
        public float Distance;
        public bool DashAway;

        public DashESkill(float distance, bool dashAway = false)
        {
            Distance = distance;
            DashAway = dashAway;
        }

        public override SkillState Execute(GameWorld world,
            Entity enemy, AICmp ai)
        {
            if (ai.CurrentTargets.Count == 0) return SkillState.Finished;


            if (enemy.HasCmp<PhysicsCmp>())
            {
                Entity target = ai.CurrentTargets.First();
                Vector2 dir   = target.Position - enemy.Position;
                if (DashAway)
                    dir = -dir;

                if (dir != Vector2.Zero)
                {
                    dir.Normalize();
                    AIUtil.SetEntityDashVelocity
                        (world.EntityManager, enemy, dir, Distance);
                }
            }

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
            if (ai.CurrentTargets.Count == 0) return SkillState.Finished;

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
            if (ai.CurrentTargets.Count == 0) return SkillState.Finished;

            if (internalState == InternalState.CalculatingTeleportPos)
            {
                CalculateTeleportPosition(world, enemy, ai);

                internalState = InternalState.FadingOut;
                fadeTimer     = FADE_TIME;
            }
            else if(internalState == InternalState.FadingOut)
            {
                AIUtil.FadeOut(world, enemy, FADE_TIME, ref fadeTimer);

                if (fadeTimer <= 0.0f)
                {
                    fadeTimer      = FADE_TIME;
                    enemy.Position = teleportPosition;
                    internalState  = InternalState.FadingIn;
                }
            }
            else if(internalState == InternalState.FadingIn)
            {
                AIUtil.FadeIn(world, enemy, FADE_TIME, ref fadeTimer);

                if (fadeTimer <= 0.0f)
                {
                    fadeTimer     = FADE_TIME;
                    internalState = InternalState.CalculatingTeleportPos;

                    return SkillState.Finished;
                }
            }
            
            return SkillState.Executing;
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
            if (ai.CurrentTargets.Count == 0) return SkillState.Finished;

            if (world.EntityManager.TryGetComponent(enemy, 
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
                    physics.LinearVelocity *= 0.1f;
                    return SkillState.Finished;
                }

                AIUtil.PathFollowing(enemy, physics, path, Speed);

                return SkillState.Executing;
            }

            return SkillState.Finished;
        }
    }
}
