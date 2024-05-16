using System.Linq;
using Microsoft.Xna.Framework;
using Cmps;
using Core;
using Physics;

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
            if (ai.CurrentTargets.Count == 0) return false;

            Entity target = ai.CurrentTargets.First();

            return Vector2.DistanceSquared(enemy.Position, target.Position)
                <= MinDistance * MinDistance;
        }
    }

    public class HasVisionCondition : Condition
    {
        CollisionBitmask Mask;

        public HasVisionCondition(CollisionBitmask mask)
        {
            this.Mask = mask;
        }

        public override bool IsTrue(GameWorld world, 
            Entity enemy, AICmp ai)
        {
            if (ai.CurrentTargets.Count == 0) return false;

            Entity target = ai.CurrentTargets.First();
            Vector2 dir   = Vector2.Normalize(target.Position - enemy.Position);

            RaycastResult result = world.Level.Physics.Raycast(enemy.Position + dir * 10.0f,
                dir, ColliderType.Static | ColliderType.Dynamic, 
                CollisionBitmask.Wall | Mask);

            return result.ColliderType == ColliderType.Dynamic;
        }
    }
}
