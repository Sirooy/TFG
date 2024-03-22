using Engine.Ecs;
using Cmps;
using Core;

namespace AI
{
    public abstract class Condition
    {
        public abstract bool IsTrue(EntityManager<Entity> entityManager,
            Entity enemy, AICmp ai);
    }
}
