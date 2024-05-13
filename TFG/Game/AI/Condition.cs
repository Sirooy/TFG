using System.Linq;
using Microsoft.Xna.Framework;
using Cmps;
using Core;


namespace AI
{
    public abstract class Condition
    {
        public abstract bool IsTrue(GameWorld world,
            Entity enemy, AICmp ai);
    }

    public class IsNearCondition : Condition
    {
        public float MinDistance;

        public IsNearCondition(float minDistance)
        {
            this.MinDistance = minDistance;
        }

        public override bool IsTrue(GameWorld world, 
            Entity enemy, AICmp ai)
        {
            Entity target = ai.CurrentTargets.First();

            return Vector2.DistanceSquared(enemy.Position, target.Position)
                <= MinDistance * MinDistance;
        }
    }
}
