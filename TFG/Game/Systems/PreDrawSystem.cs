using Engine.Ecs;
using Core;
using Cmps;

namespace Systems
{
    public class PreDrawSystem : GameSystem
    {
        private EntityManager<Entity> entityManager;

        public PreDrawSystem(EntityManager<Entity> entityManager)
        {
            this.entityManager = entityManager;
        }

        public override void Update(float _)
        {
            entityManager.ForEachComponent((Entity e, SpriteCmp s) =>
            {
                s.Transform.CacheTransform(e);

                if(s.LayerOrder == LayerOrder.Ordered)
                {
                    float yPosition = s.Transform.CachedWorldPosition.Y +
                        s.Origin.Y;
                    s.LayerDepth = (yPosition / 10000.0f) * 0.5f + 0.5f;
                }
            });
        }
    }
}
