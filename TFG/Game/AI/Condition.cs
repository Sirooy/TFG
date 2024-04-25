using Cmps;
using Core;

namespace AI
{
    public abstract class Condition
    {
        public abstract bool IsTrue(GameWorld world,
            Entity enemy, AICmp ai);
    }
}
