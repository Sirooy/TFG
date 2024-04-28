using Engine.Ecs;
using Core;
using Cmps;

namespace Systems
{
    public class HealthSystem : GameSystem
    {
        private EntityManager<Entity> entityManager;

        public HealthSystem(EntityManager<Entity> entityManager)
        {
            this.entityManager = entityManager;
        }

        public override void Update(float _)
        {
            entityManager.ForEachComponent((Entity e, HealthCmp health) =>
            {
                if(health.CurrentHealth <= 0.0f)
                {
                    if(entityManager.TryGetComponent(e, out DeathCmp death))
                    {
                        entityManager.RemoveComponent<HealthCmp>(e);
                        death.Kill();
                    }
                    else
                        entityManager.RemoveEntity(e);
                }
            });
        }
    }
}
