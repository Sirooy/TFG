﻿using Engine.Graphics;
using Engine.Ecs;

namespace Core
{
    public class GameWorld
    {
        public EntityManager<Entity> EntityManager;
        public DungeonLevel Level;
        public Camera2D Camera;
        public float Dt;
    }
}
