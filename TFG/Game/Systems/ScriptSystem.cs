using Engine.Ecs;
using Core;
using Cmps;

namespace Systems
{
    public class ScriptSystem : Engine.Ecs.System
    {
        private EntityManager<Entity> entityManager;

        public ScriptSystem(EntityManager<Entity> entityManager)
        {
            this.entityManager = entityManager;
        }

        public override void Update()
        {
            entityManager.ForEachComponent<ScriptCmp>((Entity e, ScriptCmp s) =>
            {
                s.Execute(e);
            });
        }
    }
}
