using System.Collections.Generic;
using Engine.Ecs;

namespace Core
{
    public class GameWorld
    {
        public EntityManager<Entity> EntityManager;
        public List<Entity> Players;
        public List<Entity> Enemies;
        
    }
}
