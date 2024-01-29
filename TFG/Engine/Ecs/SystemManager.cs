using System.Collections.Generic;
using Engine.Debug;
using Microsoft.Xna.Framework;

namespace Engine.Ecs
{
    public abstract class GameSystem
    {
        public abstract void Update(float dt);
    }

    public class SystemManager
    {
        private class ActiveSystem
        {
            public int Id;
            public GameSystem System;

            public ActiveSystem(int id, GameSystem system)
            {
                Id = id;
                System = system;
            }
        }

        private readonly Dictionary<int, GameSystem> systems;
        private readonly List<ActiveSystem> activeSystems;

        public SystemManager() 
        {
            systems       = new Dictionary<int, GameSystem>();
            activeSystems = new List<ActiveSystem>();
        }

        public void RegisterSystem<TSystem>(TSystem system) 
            where TSystem : GameSystem
        {
            int id = IdMetadataGenerator<GameSystem, TSystem>.Id;

            DebugAssert.Success(!systems.ContainsKey(id),
                "System \"{0}\" has already been registered",
                typeof(TSystem).Name);

            systems.Add(id, system);
        }

        public TSystem GetSystem<TSystem>()
            where TSystem : GameSystem
        {
            int id = IdMetadataGenerator<GameSystem, TSystem>.Id;

            DebugAssert.Success(systems.ContainsKey(id),
                "System \"{0}\" has not been registered",
                typeof(TSystem).Name);

            return (TSystem)systems[id];
        }

        public void EnableSystem<TSystem>()
            where TSystem : GameSystem
        {
            int id = IdMetadataGenerator<GameSystem, TSystem>.Id;

            DebugAssert.Success(systems.ContainsKey(id),
                "System \"{0}\" has not been registered",
                typeof(TSystem).Name);

            activeSystems.Add(new ActiveSystem(id, systems[id]));
        }

        public void DisableSystem<TSystem>()
            where TSystem : GameSystem
        {
            int id = IdMetadataGenerator<GameSystem, TSystem>.Id;

            DebugAssert.Success(systems.ContainsKey(id),
                "System \"{0}\" has not been registered",
                typeof(TSystem).Name);

            int index = activeSystems.FindIndex(
                (ActiveSystem sys) => { return sys.Id == id; });

            DebugAssert.Success(index != -1,
                "System \"{0}\" has not been enabled",
                typeof(TSystem).Name);

            activeSystems.RemoveAt(index);
        }

        public void DisableAllSystems()
        {
            activeSystems.Clear();
        }

        public void UpdateSystems(float dt)
        {
            for(int i = 0;i < activeSystems.Count; ++i)
            {
                activeSystems[i].System.Update(dt);
            }
        }
    }
}
