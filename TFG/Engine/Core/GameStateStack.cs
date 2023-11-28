using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Engine.Ecs;
using Engine.Debug;

namespace Engine.Core
{
    public class GameState
    {
        public virtual void OnEnter()                 { }
        public virtual void OnExit()                  { }
        public virtual bool Update(GameTime gameTime) { return true; }
        public virtual bool Draw(GameTime gameTime, SpriteBatch spriteBatch) { return true; }
    };

    public class GameStateStack
    {
        private class ActiveGameState
        {
            public int Id;
            public GameState State;

            public ActiveGameState(int id, GameState state)
            {
                Id = id;
                State = state;
            }
        }

        private readonly Dictionary<int, GameState> states;
        private readonly List<ActiveGameState> activeStates;
        private readonly List<int> addedActiveStates;
        private readonly List<int> removedActiveStates;

        public GameStateStack()
        {
            states              = new Dictionary<int, GameState>();
            activeStates        = new List<ActiveGameState>();
            addedActiveStates   = new List<int>();
            removedActiveStates = new List<int>();
        }

        public void RegisterState<TState>(TState state)
            where TState : GameState
        {
            int id = IdMetadataGenerator<GameState, TState>.Id;

            DebugAssert.Success(!states.ContainsKey(id),
                "Game state \"{0}\" has already been registered",
                typeof(TState).Name);

            states.Add(id, state);
        }

        public TState GetState<TState>()
            where TState : GameState
        {
            int id = IdMetadataGenerator<GameState, TState>.Id;

            DebugAssert.Success(states.ContainsKey(id),
                "Game state \"{0}\" has not been registered",
                typeof(TState).Name);

            return (TState)states[id];
        }

        public void PushState<TState>()
            where TState : GameState
        {
            int id = IdMetadataGenerator<GameState, TState>.Id;

            DebugAssert.Success(states.ContainsKey(id),
                "Game state \"{0}\" has not been registered",
                typeof(TState).Name);

            addedActiveStates.Add(id);
        }

        public void PopState<TState>()
            where TState : GameState
        {
            int id = IdMetadataGenerator<GameState, TState>.Id;

            DebugAssert.Success(states.ContainsKey(id),
                "Game state \"{0}\" has not been registered",
                typeof(TState).Name);

            removedActiveStates.Add(id);
        }

        public void PopLastState()
        {
            DebugAssert.Success(activeStates.Count > 0, "There are no active states");

            removedActiveStates.Add(activeStates.Last().Id);
        }

        public void PopAllActiveStates()
        {
            int i = activeStates.Count - 1;
            while(i > -1)
            {
                removedActiveStates.Add(activeStates[i].Id);
                i--;
            }
        }

        public void Update()
        {
            for(int i = 0;i < removedActiveStates.Count; i++) 
            {
                int id    = removedActiveStates[i];
                int index = activeStates.FindIndex(
                    (ActiveGameState state) => { return state.Id == id; });

                DebugAssert.Success(index != -1, 
                    "Trying to pop inactive state with id {0}", id);

                activeStates[index].State.OnExit();
                activeStates.RemoveAt(index);
            }

            for(int i = 0;i < addedActiveStates.Count; i++)
            {
                int id = addedActiveStates[i];

                DebugAssert.Success(states.ContainsKey(id), 
                    "Trying to push unregistered state with id {0}", id);

                GameState state = states[id];
                state.OnEnter();
                activeStates.Add(new ActiveGameState(id, state));
            }

            removedActiveStates.Clear();
            addedActiveStates.Clear();
        }

        public void UpdateActiveStates(GameTime gameTime)
        {
            int i     = activeStates.Count - 1;
            bool next = true;

            while(i > -1 && next)
            {
                next = activeStates[i].State.Update(gameTime);
                i--;
            }
        }

        public void DrawActiveStates(GameTime gameTime, SpriteBatch spriteBatch)
        {
            int i     = activeStates.Count - 1;
            bool next = true;

            while(i > -1 && next)
            {
                next = activeStates[i].State.Draw(gameTime, spriteBatch);
                i--;
            }
        }
    }
}
