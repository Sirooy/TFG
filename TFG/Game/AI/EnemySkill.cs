using Engine.Ecs;
using Cmps;
using Core;
using System.Linq;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

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
        public float MaxDistance { get; set; }

        public TeleportToTargetESkill(float maxDistance)
        {
            MaxDistance = maxDistance;
        }

        public override SkillState Execute(GameWorld world,
            Entity enemy, AICmp ai)
        {
            
            return SkillState.Finished;
        }
    }

    public class PathFollowESkill : EnemySkill
    {
        public float Speed { get; set; }
        private List<Vector2> path;
        private bool hasCalculatedPath;

        public PathFollowESkill(float speed)
        {
            Speed             = speed;
            path              = new List<Vector2>();
            hasCalculatedPath = false;
        }

        public override SkillState Execute(GameWorld world,
            Entity enemy, AICmp ai)
        {
            Entity target = ai.CurrentTargets.First();

            if(!hasCalculatedPath)
            {
                world.Level.PathFindingMap.FindPath(path,
                    enemy.Position, target.Position);

                //Path not found
                if(path.Count == 0)
                    return SkillState.Finished;
                else
                    hasCalculatedPath = true;
            }



            return SkillState.Finished;
        }
    }
}
