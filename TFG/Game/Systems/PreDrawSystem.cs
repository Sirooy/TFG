using Engine.Ecs;
using Core;
using Cmps;

namespace Systems
{
    public class PreDrawSystem : GameSystem
    {
        private EntityManager<Entity> entityManager;
        private DungeonLevel level;

        public PreDrawSystem(EntityManager<Entity> entityManager, 
            DungeonLevel level)
        {
            this.entityManager = entityManager;
            this.level         = level;
        }

        public override void Update(float _)
        {
            entityManager.ForEachComponent((Entity e, SpriteCmp s) =>
            {
                s.Transform.CacheTransform(e);

                if(s.LayerOrder == LayerOrder.Ordered)
                {
                    float yPosition = s.Transform.CachedWorldPosition.Y;
                    s.LayerDepth = (yPosition / (level.Height * 2.0f)) * 0.5f + 0.5f;
                }
            });
        }
    }
}
