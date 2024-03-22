using Engine.Ecs;
using Cmps;
using Core;
using System.Linq;
using Microsoft.Xna.Framework;
using System;

namespace AI
{
    public enum SkillState
    {
        Executing = 0,
        Finished  = 1
    }

    public abstract class EnemySkill : DecisionTreeNode
    {
        public override sealed DecisionTreeNode Run(EntityManager<Entity> entityManager, 
            Entity enemy, AICmp ai)
        {
            return this;
        }

        public abstract SkillState Execute(EntityManager<Entity> entityManager,
            Entity enemy, AICmp ai);
    }

    public class StraightDashSkill : EnemySkill
    {
        public float MaxDistance { get; set; }

        public StraightDashSkill(float maxDistance) 
        {
            MaxDistance = maxDistance;
        }

        public override SkillState Execute(EntityManager<Entity> entityManager, 
            Entity enemy, AICmp ai)
        {
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
}
