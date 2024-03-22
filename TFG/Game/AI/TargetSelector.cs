using Microsoft.Xna.Framework;
using Engine.Ecs;
using Cmps;
using Core;
using System;

namespace AI
{
    public abstract class TargetSelector
    {
        public abstract void Select(EntityManager<Entity> entityManager,
            Entity enemy, AICmp ai);
    }

    public class NearestEntitySelector : TargetSelector
    {
        private EntityTags tags;

        public NearestEntitySelector(EntityTags tags)
        {
            this.tags = tags;
        }

        public override void Select(EntityManager<Entity> entityManager, 
            Entity enemy, AICmp ai)
        {
            ai.CurrentTargets.Clear();

            float minDist = float.MaxValue;
            Entity target = null;
            entityManager.ForEachEntity((Entity e) =>
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
}
