using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Engine.Core;
using Engine.Ecs;
using Cmps;
using Core;

namespace TFG.Game.AI
{
    public class AIUtil
    {
        public static void PathFollowing(Entity e, PhysicsCmp physics, 
            List<Vector2> path, float speed)
        {
            const float TARGET_OFFSET = 8.0f;

            Vector2 targetPos;
            if(path.Count == 1)
            {
                targetPos = path[0];
            }
            else
            {
                Vector2 closestPos = path[0];
                Vector2 nextPos    = path[1];

                Vector2 toEntity   = e.Position - closestPos;
                Vector2 direction  = nextPos - closestPos;
                direction.Normalize();

                float proj            = Vector2.Dot(toEntity, direction);
                Vector2 pointAlongDir = closestPos + direction * proj;
                targetPos             = pointAlongDir + direction * TARGET_OFFSET;
            }

            Vector2 forceDir = targetPos - e.Position;
            if (forceDir.IsNearlyZero())
                forceDir = Vector2.UnitX;
            else
                forceDir.Normalize();

            physics.LinearVelocity = forceDir * speed;
        }
    }
}
