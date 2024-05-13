using Microsoft.Xna.Framework;
using Engine.Ecs;
using Cmps;
using Core;
using System;

namespace AI
{
    public abstract class TargetSelector
    {
        public abstract void Select(GameWorld world,
            Entity enemy, AICmp ai);
    }

    public class NearestEntitySelector : TargetSelector
    {
        private EntityTags tags;

        public NearestEntitySelector(EntityTags tags)
        {
            this.tags = tags;
        }

        public override void Select(GameWorld world, 
            Entity enemy, AICmp ai)
        {
            ai.CurrentTargets.Clear();

            float minDist = float.MaxValue;
            Entity target = null;
            world.EntityManager.ForEachEntity((Entity e) =>
            {
                if(e.HasTag(tags))
                {
                    float dist = Vector2.DistanceSquared(enemy.Position, e.Position);
                    if (dist <= minDist)
                    {
                        target  = e;
                        minDist = dist;
                    }
                }
            });

            ai.CurrentTargets.Add(target);
        }
    }

    public class LessHealthEntitySelector : TargetSelector
    {
        private EntityTags tags;

        public LessHealthEntitySelector(EntityTags tags)
        {
            this.tags = tags;
        }

        public override void Select(GameWorld world,
            Entity enemy, AICmp ai)
        {
            ai.CurrentTargets.Clear();

            float minHealth = float.MaxValue;
            Entity target   = null;
            world.EntityManager.ForEachComponent((Entity e, HealthCmp health) =>
            {
                if (e.HasTag(tags))
                {
                    if(health.CurrentHealth < minHealth)
                    {
                        target    = e;
                        minHealth = health.CurrentHealth;
                    }
                }
            });

            ai.CurrentTargets.Add(target);
        }
    }
}
