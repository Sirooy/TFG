using System;
using Engine.Ecs;
using Core;
using Cmps;

namespace Systems
{
    public class DeathSystem : GameSystem
    {
        private GameWorld gameWorld;
        private EntityManager<Entity> entityManager;

        public DeathSystem(GameWorld gameWorld) 
        {
            this.gameWorld     = gameWorld;
            this.entityManager = gameWorld.EntityManager;
        }

        public override void Update(float dt)
        {
            entityManager.ForEachComponent((Entity e, DeathCmp deathCmp) =>
            {
                if (deathCmp.State == DeathState.Alive) return;

                if (deathCmp.State == DeathState.EnteringDeath)
                {
                    if (deathCmp.OnEnterDeath != null)
                        deathCmp.OnEnterDeath(gameWorld, e);
                    deathCmp.State = DeathState.Dying;
                }

                if (deathCmp.State == DeathState.Dying)
                {
                    if (deathCmp.OnDying != null)
                    {
                        DyingState state = deathCmp.OnDying(gameWorld, e, dt);
                        if (state == DyingState.Kill)
                            deathCmp.State = DeathState.ExitingDeath;
                    }
                    else
                        deathCmp.State = DeathState.ExitingDeath;
                }

                if (deathCmp.State == DeathState.ExitingDeath)
                {
                    if(deathCmp.OnExitDeath != null)
                        deathCmp.OnExitDeath(gameWorld, e);
                    entityManager.RemoveEntity(e);
                }
            });
        }
    }
}
