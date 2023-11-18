using Cmps;
using Engine.Ecs;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TFG;

namespace Systems
{
    public class PhysicsSystem : Engine.Ecs.System
    {
        EntityManager<Entity> entityManager;
        private float dt;

        public PhysicsSystem(EntityManager<Entity> entityManager, float dt)
        {
            this.entityManager = entityManager;
            this.dt = dt;
        }

        public override void Update()
        {
            entityManager.ForEachComponent((Entity e, PhysicsCmp physics) =>
            {
                physics.Acceleration    = physics.Force * physics.InverseMass;
                physics.LinearVelocity += physics.Acceleration * dt;

                //Clamp the velocity before updating the position
                physics.LinearVelocity.X = Math.Clamp(physics.LinearVelocity.X,
                    -physics.MaxLinearVelocity.X, physics.MaxLinearVelocity.X);
                physics.LinearVelocity.Y = Math.Clamp(physics.LinearVelocity.Y,
                    -physics.MaxLinearVelocity.Y, physics.MaxLinearVelocity.Y);

                e.Position             += physics.LinearVelocity * dt;

                physics.Force = Vector2.Zero;
            });
        }
    }
}
