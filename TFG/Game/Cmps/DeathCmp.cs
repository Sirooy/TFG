using Engine.Ecs;
using System;
using Core;

namespace Cmps
{
    public enum DeathState
    {
         Alive,
         EnteringDeath,
         Dying,
         ExitingDeath
    }

    public enum DyingState
    {
        KeepAlive,
        Kill
    }

    public class DeathCmp
    {
        public Action<GameWorld, Entity> OnEnterDeath;
        public Func<GameWorld, Entity, float, DyingState> OnDying;
        public Action<GameWorld, Entity> OnExitDeath;

        public DeathState State;

        public DeathCmp()
        {
            State = DeathState.Alive;
        }

        public void Kill()
        {
            if (State == DeathState.Alive)
                State = DeathState.EnteringDeath;
        }
    }
}
