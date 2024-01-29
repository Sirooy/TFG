using Engine.Ecs;
using Core;
using Cmps;

namespace Systems
{
    public class ScriptSystem : GameSystem
    {
        private EntityManager<Entity> entityManager;

        public ScriptSystem(EntityManager<Entity> entityManager)
        {
            this.entityManager = entityManager;
        }

        public override void Update(float _)
        {
            entityManager.ForEachComponent((Entity e, ScriptCmp script) =>
            {
                script.Execute(e);
            });
        }
    }
}
