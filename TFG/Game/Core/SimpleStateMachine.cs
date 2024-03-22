using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Engine.Debug;

namespace Core
{
    public class SimpleStateMachine<T> where T : Enum
    {
        public class State
        {
            public T Key;
            public Action<float> Update;
            public Action        OnEnter;
            public Action        OnExit;

            public State(T key, 
                Action<float> update, 
                Action onEnter, 
                Action onExit)
            {
                Key     = key;
                Update  = update;
                OnEnter = onEnter;
                OnExit  = onExit;
            }

        }

        public T CurrentStateKey { get { return currentState.Key; } }

        private Dictionary<T, State> states;
        private State currentState;
        private State newState;

        public SimpleStateMachine()
        {
            states          = new Dictionary<T, State>();
            currentState    = null;
            newState        = null;
        }

        public void AddState(T key, 
            Action<float> update  = null, 
            Action        onEnter = null,
            Action        onExit  = null)
        {
            DebugAssert.Success(!states.ContainsKey(key),
                "State with key {0} has already been added", key);

            State state  = new State(key, update, onEnter, onExit);
            states.Add(key, state);

            currentState = state;
        }

        public void ChangeState(T key)
        {
            DebugAssert.Success(states.ContainsKey(key),
                "State with key {0} not found", key);

            newState = states[key];
        }

        public void SetState(T key)
        {
            DebugAssert.Success(states.ContainsKey(key),
                "State with key {0} not found", key);

            currentState = states[key];
        }

        public void Update(float value) 
        { 
            if(newState != null)
            {
                if (currentState.OnExit != null)
                    currentState.OnExit();

                if (newState.OnEnter != null)
                    newState.OnEnter();

                currentState = newState;
                newState     = null;
            }

            if (currentState.Update != null)
                currentState.Update(value);
        }
    }
}
