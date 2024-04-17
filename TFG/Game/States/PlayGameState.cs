using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Engine.Core;
using Engine.Debug;
using Engine.Ecs;
using Engine.Graphics;
using Core;
using Cmps;
using Physics;
using Systems;
using AI;
using System.IO;

namespace States
{
    public class PlayGameState : GameState
    {
        public class PlayerGameData
        {
            public List<Dice> Dices;
            public List<DiceFace> DiceRolls;
            public int MaxRolls;
            public int NumRollsLeft;

            public PlayerGameData()
            {
                Dices        = new List<Dice>();
                DiceRolls    = new List<DiceFace>();
                MaxRolls     = 4;
                NumRollsLeft = MaxRolls;
            }
        }

        private GameMain game;
        private Camera2D camera;
        private GameStateStack gameStates;
        private PlayerGameData playerData;
        private EntityFactory entityFactory;
        private EntityManager<Entity> entityManager;
        private List<string> levels;

        public Camera2D Camera                     { get { return camera; } }
        public GameStateStack GameStates           { get { return gameStates; } }
        public EntityManager<Entity> EntityManager { get { return entityManager; } }
        public PlayerGameData PlayerData           { get { return playerData; } }

        public PlayGameState(GameMain game)
        {
            this.game                  = game;
            this.gameStates            = new GameStateStack();
            this.playerData            = new PlayerGameData();
            this.entityManager         = new EntityManager<Entity>();
            this.entityFactory         = new EntityFactory(entityManager, game.Content);
            this.levels                = new List<string>();
            this.camera                = new Camera2D(game.Screen);
            this.camera.RotationAnchor = new Vector2(0.5f, 0.5f);
            this.camera.PositionAnchor = new Vector2(0.5f, 0.5f);

            CreateGameStates();
            RegisterEntityComponents();
        }

        private void CreateGameStates()
        {
            gameStates.RegisterState(new PlayGameDungeonState(game, this));
            gameStates.RegisterState(new PlayGameWinState(game, this));
            gameStates.RegisterState(new PlayGameLoseState(game, this));
        }

        private void RegisterEntityComponents() 
        {
            entityManager.RegisterComponent<AICmp>();
            entityManager.RegisterComponent<HealthCmp>();
            entityManager.RegisterComponent<ScriptCmp>();
            entityManager.RegisterComponent<SpriteCmp>();
            entityManager.RegisterComponent<PhysicsCmp>();
            entityManager.RegisterComponent<ColliderCmp>();
            entityManager.RegisterComponent<CharacterCmp>();
            entityManager.RegisterComponent<TriggerColliderCmp>();
            entityManager.RegisterComponent<AnimationControllerCmp>();
        }

        public override void OnEnter()
        {
            ContentManager content = game.Content;
            DebugDraw.Camera       = camera;

            LoadLevelPaths();
            gameStates.PushState<PlayGameDungeonState>();

            DebugLog.Info("OnEnter state: {0}", nameof(PlayGameState));
        }

        public override void OnExit()
        {
            gameStates.PopAllActiveStates();
            gameStates.Update();

            DebugLog.Info("OnExit state: {0}", nameof(PlayGameState));
        }

        public override StateResult Update(GameTime gameTime)
        {
            gameStates.Update();
            gameStates.UpdateActiveStates(gameTime);

            return StateResult.StopExecuting;
        }

        public override StateResult Draw(GameTime gameTime)
        {
            gameStates.DrawActiveStates(gameTime);

            return StateResult.StopExecuting;
        }

        private void LoadLevelPaths()
        {
            levels.Clear();
            foreach (string level in Directory.GetFiles("../../../Content/levels"))
            {
                if (level.EndsWith(".m"))
                {
                    levels.Add(level);
                }
            }
        }

        public string GetNextLevel()
        {
            if(levels.Count == 0)
            {
                LoadLevelPaths();
            }

            int index = Random.Shared.Next(levels.Count);
            string level = levels[index];
            levels.RemoveAt(index);

            return level;
        }
    }
}