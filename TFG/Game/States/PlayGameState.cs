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
using TFG;
using AI;

namespace States
{
    public class PlayGameState : GameState
    {
        public enum State
        {
            PlayerRolling,
            PlayerTurn,
            ExecutingPlayerTurn,
            AITurn,
            ExecutingAITurn
        };

        const float FACE_SIZE = 32.0f;

        public class DiceRoll
        {
            public DiceFace Face; 
            public Vector2 Position;

            public DiceRoll(DiceFace face)
            {
                Face = face;
            }
        }

        public class PlayerData
        {
            public List<Dice> Dices;
            public List<DiceRoll> DiceRolls;
            public int MaxRolls;
            public int CurrentRolls;

            public PlayerData()
            {
                Dices        = new List<Dice>();
                DiceRolls    = new List<DiceRoll>();
                MaxRolls     = 4;
                CurrentRolls = 0;
            }
        }

        public class AIEntity
        {
            public Entity Entity;
            public AICmp AI;

            public AIEntity(Entity e, AICmp ai)
            {
                Entity = e;
                AI     = ai;
            }
        }

        public class AIData
        {
            public List<AIEntity> Enemies;
            public AIEntity CurrentEnemy;
            public EnemySkill CurrentEnemySkill;

            public AIData()
            {
                Enemies           = new List<AIEntity>();
                CurrentEnemy      = null;
                CurrentEnemySkill = null;
            }
        }

        private GameMain game;
        private SpriteBatch spriteBatch;
        private SystemManager updateSystems;
        private SystemManager drawSystems;
        private EntityManager<Entity> entityManager;
        private Camera2D camera;
        private CameraController cameraController;
        private DungeonLevel level;
        private EntityFactory entityFactory;
        private List<Entity> players;
        private SimpleStateMachine<State> stateMachine;

        private Texture2D diceFaceTexture;
        private Entity player;
        private PlayerData playerData;
        private AIData aiData;
        private DiceRoll selectedDiceRoll;
        private Entity selectedTarget;


        public PlayGameState(GameMain game)
        {
            this.game             = game;
            this.spriteBatch      = game.SpriteBatch;
            this.updateSystems    = new SystemManager();
            this.drawSystems      = new SystemManager();
            this.entityManager    = new EntityManager<Entity>();
            this.entityFactory    = new EntityFactory(entityManager, game.Content);
            this.camera           = new Camera2D(game.Screen);
            this.cameraController = new CameraController(camera);
            this.level            = new DungeonLevel(game.Content);
            this.players          = new List<Entity>();
            this.camera.RotationAnchor = new Vector2(0.5f, 0.5f);
            this.camera.PositionAnchor = new Vector2(0.5f, 0.5f);

            this.playerData = new PlayerData();
            this.aiData     = new AIData();

            playerData.Dices.Add(new Dice(new List<DiceFace>()
            {
                new DashDiceFace(1),
                new DashDiceFace(2),
                new DashDiceFace(3),
                new DashDiceFace(4),
            }));

            playerData.Dices.Add(new Dice(new List<DiceFace>()
            {
                new ProjectileDiceFace(),
                new ProjectileDiceFace(),
                new DashDiceFace(6),
            }));

            RegisterEntityComponents();
            RegisterUpdateSystems();
            RegisterDrawSystems();
            CreateStateMachine();
        }

        private void CreateStateMachine()
        {
            stateMachine = new SimpleStateMachine<State>();
            stateMachine.AddState(State.PlayerRolling, UpdatePlayerRolling, OnEnterPlayerRolling); ;
            stateMachine.AddState(State.PlayerTurn, UpdatePlayerTurn, OnEnterPlayerTurn);
            stateMachine.AddState(State.ExecutingPlayerTurn, UpdateExecutingPlayerTurn);
            stateMachine.AddState(State.AITurn, UpdateAITurn);
            stateMachine.AddState(State.ExecutingAITurn, UpdateExecutingAITurn);
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

        private void RegisterUpdateSystems()
        {
            updateSystems.RegisterSystem(new ScriptSystem(entityManager));
            updateSystems.RegisterSystem(new PhysicsSystem(entityManager, 
                Vector2.Zero, 1.0f / 60.0f));
            updateSystems.RegisterSystem(new SpriteAnimationSystem(entityManager));
            updateSystems.RegisterSystem(new PreDrawSystem(entityManager));
            updateSystems.RegisterSystem(new HealthSystem(entityManager));

            PhysicsSystem physiscSystem = updateSystems.GetSystem<PhysicsSystem>();
            physiscSystem.Iterations    = 1;

            updateSystems.EnableSystem<ScriptSystem>();
            updateSystems.EnableSystem<PhysicsSystem>();
            updateSystems.EnableSystem<SpriteAnimationSystem>();
            updateSystems.EnableSystem<PreDrawSystem>();
            updateSystems.EnableSystem<HealthSystem>();
        }

        private void RegisterDrawSystems()
        {
            drawSystems.RegisterSystem(new RenderSystem(entityManager, 
                spriteBatch, camera));
            drawSystems.EnableSystem<RenderSystem>();
        }

        public override void OnEnter()
        {
            ContentManager content = game.Content;
            diceFaceTexture = content.Load<Texture2D>("DiceFaceSpriteSheet");

            level.Load("../../../Content/levels/Map1.m",
                updateSystems.GetSystem<PhysicsSystem>(),
                entityFactory);

            int spawnsCount = level.SpawnPoints.Count;
            int spawnIndex  = Random.Shared.Next(spawnsCount);
            CreatePlayer(level.SpawnPoints[spawnIndex]);
            CreatePlayer(level.SpawnPoints[(spawnIndex + 1) % spawnsCount]);
            CreatePlayer(level.SpawnPoints[(spawnIndex + 2) % spawnsCount]);
            stateMachine.SetState(State.PlayerRolling);
            playerData.DiceRolls.Clear();
            playerData.CurrentRolls = 0;

            DebugDraw.Camera = camera;
            DebugLog.Info("OnEnter state: {0}", nameof(PlayGameState));
        }

        public override void OnExit()
        {
            DebugLog.Info("OnExit state: {0}", nameof(PlayGameState));
        }

        public override bool Update(GameTime gameTime)
        {
            float dt = (float) gameTime.ElapsedGameTime.TotalSeconds;

            cameraController.Update(dt);
            stateMachine.Update(dt);
            updateSystems.UpdateSystems(dt);
            
            //##### BORRAR #####
            //if (MouseInput.IsMiddleButtonPressed())
            //{
            //    CreatePlayer(MouseInput.GetPosition(camera));
            //}

            if (player != null)
            {
                PhysicsCmp phy = entityManager.GetComponent<PhysicsCmp>(player);

                Vector2 force = Vector2.Zero;
                if (KeyboardInput.IsKeyDown(Keys.Up))
                    force.Y -= 1.0f;
                if (KeyboardInput.IsKeyDown(Keys.Down))
                    force.Y += 1.0f;
                if (KeyboardInput.IsKeyDown(Keys.Right))
                    force.X += 1.0f;
                if (KeyboardInput.IsKeyDown(Keys.Left))
                    force.X -= 1.0f;

                if (force != Vector2.Zero)
                {
                    force.Normalize();
                    phy.Force += force * 1000.0f;
                }
            }
            //##### #####
            

            return false;
        }

        public override bool Draw(GameTime gameTime)
        {
            game.GraphicsDevice.Clear(Color.Black);
            float dt = (float) gameTime.ElapsedGameTime.TotalSeconds;

            spriteBatch.Begin(camera, samplerState: SamplerState.PointClamp,
                blendState: BlendState.Additive);
            level.TileMap.DrawPreEntitiesLayer(camera, spriteBatch);

            if (stateMachine.CurrentStateKey == State.ExecutingPlayerTurn)
            {
                if (selectedDiceRoll != null)
                {
                    selectedDiceRoll.Face.Draw(spriteBatch, selectedTarget);
                }
            }

            spriteBatch.End();

            drawSystems.UpdateSystems(dt);

            spriteBatch.Begin(camera, samplerState: SamplerState.PointClamp);
            level.TileMap.DrawPostEntitiesLayer(camera, spriteBatch);

            

            spriteBatch.End();

            //##### BORRAR ######
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            switch(stateMachine.CurrentStateKey)
            {
                case State.PlayerRolling:
                    {
                        DrawPlayerRolls();
                    }
                    break;
                case State.PlayerTurn:
                    {
                        DrawPlayerRolls();
                        if (selectedDiceRoll != null)
                            spriteBatch.Draw(diceFaceTexture, 
                                MouseInput.GetPosition(game.Screen),
                                selectedDiceRoll.Face.SourceRect, Color.White);
                    }
                    break;
                case State.ExecutingPlayerTurn:
                    {
                        DrawPlayerRolls();
                    }
                    break;
                case State.AITurn:              break;
                case State.ExecutingAITurn:     break;
                default:                        break;
            }
            spriteBatch.End();
            //##### BORRAR ######

            return false;
        }

        private void RecalculateDiceRollPositions()
        {
            const float MARGIN = 4.0f;
            int count = playerData.DiceRolls.Count;
            float totalWidth = count * FACE_SIZE + (count - 1) * MARGIN;
            float startX = game.Screen.HalfWidth - totalWidth * 0.5f;
            float startY = game.Screen.Height - FACE_SIZE - MARGIN * 4.0f;
            Vector2 startPos = new Vector2(startX, startY);

            for(int i = 0;i < count; ++i)
            {
                DiceRoll roll = playerData.DiceRolls[i];
                roll.Position = startPos;
                startPos.X   += FACE_SIZE + MARGIN;
            }
        }

        private void DrawPlayerRolls()
        {
            for (int i = 0;i < playerData.DiceRolls.Count; ++i)
            {
                DiceRoll roll = playerData.DiceRolls[i];

                spriteBatch.Draw(diceFaceTexture, roll.Position, 
                    roll.Face.SourceRect, Color.White);
            }
        }

        private void OnEnterPlayerRolling()
        {
            playerData.DiceRolls.Clear();
            playerData.CurrentRolls = 0;
        }

        private void UpdatePlayerRolling(float _)
        {
            if (KeyboardInput.IsKeyPressed(Keys.D1))
            {
                playerData.CurrentRolls++;
                playerData.DiceRolls.Add(new DiceRoll(playerData.Dices[0].Roll()));
                RecalculateDiceRollPositions();

                if (playerData.CurrentRolls == playerData.MaxRolls)
                {
                    stateMachine.ChangeState(State.PlayerTurn);
                }
            }
            else if (KeyboardInput.IsKeyPressed(Keys.D2))
            {
                playerData.CurrentRolls++;
                playerData.DiceRolls.Add(new DiceRoll(playerData.Dices[1].Roll()));
                RecalculateDiceRollPositions();

                if (playerData.CurrentRolls == playerData.MaxRolls)
                {
                    stateMachine.ChangeState(State.PlayerTurn);
                }
            }
        }

        private void OnEnterPlayerTurn()
        {
            selectedDiceRoll = null;
            selectedTarget   = null;
        }

        private void UpdatePlayerTurn(float _)
        {
            if (selectedDiceRoll == null)
            {
                for (int i = 0; i < playerData.DiceRolls.Count; ++i)
                {
                    DiceRoll roll = playerData.DiceRolls[i];
                    Vector2 mouse = MouseInput.GetPosition(game.Screen);

                    if (mouse.X >= roll.Position.X && mouse.X <= roll.Position.X + FACE_SIZE &&
                       mouse.Y >= roll.Position.Y && mouse.Y <= roll.Position.Y + FACE_SIZE)
                    {
                        if (MouseInput.IsLeftButtonPressed())
                        {
                            selectedDiceRoll = roll;
                            playerData.DiceRolls.RemoveAt(i);
                            RecalculateDiceRollPositions();
                            break;
                        }
                    }
                }
            }
            else
            {
                if (MouseInput.IsLeftButtonReleased())
                {
                    Vector2 mouse = MouseInput.GetPosition(camera);
                    entityManager.ForEachComponent((Entity e, CharacterCmp chara) =>
                    {
                        if (e.HasTag(EntityTags.Player))
                        {
                            ColliderCmp col = entityManager.GetComponent<ColliderCmp>(e);
                            CircleCollider c = (CircleCollider)col.Collider;

                            if (CollisionTester.PointVsCircle(mouse,
                                col.Transform.CachedWorldPosition, c.CachedRadius))
                            {
                                selectedTarget = e;
                                chara.SelectState = SelectState.Selected;
                                stateMachine.ChangeState(State.ExecutingPlayerTurn);
                                return;
                            }
                        }
                    });

                    if (selectedTarget == null)
                    {
                        playerData.DiceRolls.Add(selectedDiceRoll);
                        RecalculateDiceRollPositions();
                        selectedDiceRoll = null;
                    }
                }
            }

            if (selectedDiceRoll == null && (playerData.DiceRolls.Count == 0 ||
                KeyboardInput.IsKeyPressed(Keys.Enter)))
            {
                aiData.Enemies.Clear();
                entityManager.ForEachComponent((Entity e, AICmp ai) =>
                {
                    aiData.Enemies.Add(new AIEntity(e, ai));
                });

                stateMachine.ChangeState(State.AITurn);
            }
        }

        private void UpdateExecutingPlayerTurn(float dt)
        {
            if(selectedDiceRoll == null)
            {
                if (TurnHasFinished())
                {
                    stateMachine.ChangeState(State.PlayerTurn);
                }
            }
            else
            {
                bool hasFinished = selectedDiceRoll.Face.Update(dt,
                                    entityManager, cameraController.Camera, selectedTarget);

                if (hasFinished)
                {
                    CharacterCmp chara = entityManager.GetComponent<CharacterCmp>(selectedTarget);
                    chara.SelectState  = SelectState.None;
                    selectedDiceRoll   = null;
                    selectedTarget     = null;
                }
            }
        }

        private void UpdateAITurn(float _)
        {
            int numEnemies = aiData.Enemies.Count;

            if (numEnemies > 0)
            {
                int index = Random.Shared.Next(numEnemies);
                aiData.CurrentEnemy = aiData.Enemies[index];
                aiData.Enemies.RemoveAt(index);

                AICmp ai     = aiData.CurrentEnemy.AI;
                Entity enemy = aiData.CurrentEnemy.Entity;

                DecisionTreeNode skill   = ai.DecisionTree.Run(entityManager, enemy, ai);
                aiData.CurrentEnemySkill = skill as EnemySkill;

                CharacterCmp charCmp = entityManager.
                    GetComponent<CharacterCmp>(enemy);
                charCmp.SelectState = SelectState.Selected;

                stateMachine.ChangeState(State.ExecutingAITurn);
            }
            else
            {
                stateMachine.ChangeState(State.PlayerRolling);
            }
        }

        public void UpdateExecutingAITurn(float _)
        {
            AICmp ai         = aiData.CurrentEnemy.AI;
            Entity enemy     = aiData.CurrentEnemy.Entity;
            EnemySkill skill = aiData.CurrentEnemySkill;

            if(skill != null)
            {
                SkillState skillState = skill.Execute(entityManager, enemy, ai);

                if (skillState == SkillState.Finished)
                    aiData.CurrentEnemySkill = null;
            }
            else if(TurnHasFinished())
            {
                CharacterCmp charCmp = entityManager.
                    GetComponent<CharacterCmp>(enemy);
                charCmp.SelectState  = SelectState.None;

                stateMachine.ChangeState(State.AITurn);
            }
        }


        //BORRAR
        private void CreatePlayer(Vector2 position)
        {
            Texture2D platformTexture = game.Content.Load<Texture2D>("EntityPlatform");
            Texture2D playerTexture   = game.Content.Load<Texture2D>("PlayerSpriteSheet");

            Vector2 pos = position;
            Entity e = entityManager.CreateEntity();
            e.AddTag(EntityTags.Player);

            e.Position = pos;
            PhysicsCmp phy = entityManager.AddComponent(e, new PhysicsCmp());
            phy.Inertia = 0.0f;
            phy.LinearDamping = 1.5f;
            ColliderCmp col = entityManager.AddComponent(e, new ColliderCmp(
                new CircleCollider(16.0f), new Material(1.0f, 0.0f, 0.0f),
                CollisionBitmask.Player, CollisionBitmask.All));
            SpriteCmp playerSpr  = entityManager.AddComponent(e, 
                new SpriteCmp(playerTexture));
            playerSpr.SourceRect = new Rectangle(0, 0, 48, 40);
            playerSpr.LayerOrder = LayerOrder.Ordered;
            playerSpr.Origin     = new Vector2(
                playerSpr.SourceRect.Value.Width * 0.5f,
                playerTexture.Height);

            CharacterCmp charCmp = entityManager.AddComponent(e, 
                new CharacterCmp(platformTexture));
            charCmp.PlatformSourceRect = new Rectangle(0, 0, 32, 32);
            charCmp.SelectSourceRect   = new Rectangle(32, 0, 36, 36);

            AnimationControllerCmp anim = entityManager.AddComponent(e,
                new AnimationControllerCmp(0));
            anim.AddAnimation("Idle", new SpriteAnimation(new List<Rectangle>() 
            {
                new Rectangle(0, 0, 48, 40),
                new Rectangle(48, 0, 48, 40),
                new Rectangle(96, 0, 48, 40),
                new Rectangle(144, 0, 48, 40)
            }, 0.5f));
            anim.Play("Idle");

            entityManager.AddComponent(e, new HealthCmp(20.0f));

            if (player == null) player = e;
            else
            {
                Color[] colors = new Color[]
                    { Color.Red, Color.Green, Color.Blue, Color.Yellow };
                Color color       = colors[Random.Shared.Next(colors.Length)];
                playerSpr.Color   = color;
            }
        }

        private bool TurnHasFinished()
        {
            bool ret = true;

            entityManager.ForEachComponent((Entity e, PhysicsCmp physics) =>
            {
                const float MIN_VEL = 1.5f;
                float l = physics.LinearVelocity.LengthSquared();

                if(l > MIN_VEL * MIN_VEL)
                {
                    ret = false;
                    return;
                }
            });

            return ret;
        }
    }
}