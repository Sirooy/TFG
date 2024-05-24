using AI;
using Cmps;
using Core;
using Engine.Core;
using Engine.Debug;
using Engine.Ecs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Physics;
using System;
using System.Collections.Generic;
using Systems;
using UI;
using static States.PlayGameState;

namespace States
{
    public class PlayGameDungeonState : GameState
    {
        public enum State
        {
            PlayerRolling,
            PlayerTurn,
            ExecutingPlayerTurn,
            StartingAITurn,
            AITurn,
            ExecutingAITurn
        };

        public class AIEntity
        {
            public Entity Entity;
            public AICmp AI;

            public AIEntity(Entity e, AICmp ai)
            {
                Entity = e;
                AI = ai;
            }
        }

        public class AIData
        {
            public const float SELECT_MAX_WAIT_TIME = 1.0f;

            public List<AIEntity> Enemies;
            public AIEntity CurrentEnemy;
            public EnemySkill CurrentEnemySkill;
            public float SelectWaitTime;

            public AIData()
            {
                Enemies           = new List<AIEntity>();
                CurrentEnemy      = null;
                CurrentEnemySkill = null;
                SelectWaitTime    = SELECT_MAX_WAIT_TIME;
            }
        }

        private GameMain game;
        private PlayGameState parentState;
        private SpriteBatch spriteBatch;
        private EntityManager<Entity> entityManager;
        private EntityFactory entityFactory;
        private SystemManager updateSystems;
        private SystemManager drawSystems;
        private CameraController cameraController;
        private DungeonLevel level;
        private SimpleStateMachine<State> stateMachine;
        private UIContext ui;
        private PlayerGameData playerData;
        private AIData aiData;
        private Entity selectedTarget;
        private PlayerSkill selectedDiceRoll;
        private GameWorld gameWorld;
        private Song music;

        public PlayGameDungeonState(GameMain game, PlayGameState parentState)
        {
            this.game = game;
            this.parentState = parentState;
            this.spriteBatch = game.SpriteBatch;
            this.entityManager = parentState.EntityManager;
            this.entityFactory = parentState.EntityFactory;
            this.updateSystems = new SystemManager();
            this.drawSystems = new SystemManager();
            this.cameraController = new CameraController(parentState.Camera);
            this.level = new DungeonLevel(game.Content);
            this.music = game.Content.Load<Song>(
                GameContent.MusicPath("PlayGameMusic"));

            this.playerData = parentState.PlayerData;
            this.aiData = new AIData();
            this.gameWorld = new GameWorld();
            gameWorld.EntityManager = entityManager;
            gameWorld.EntityFactory = entityFactory;
            gameWorld.Level         = level;
            gameWorld.Camera        = parentState.Camera;
            
            RegisterUpdateSystems();
            RegisterDrawSystems();
            CreateStateMachine();
            CreateUI();
        }

        private void CreateUI()
        {
            ui = new UIContext(game.Screen);

            //Dice Layout
            CreateUIDiceLayoutAndNumRolls();

            //Bottom bar layout
            Constraints bottomBarConstraints = new Constraints(
                new CenterConstraint(),
                new PercentConstraint(0.9f),
                new PercentConstraint(1.0f),
                new PercentConstraint(0.1f));
            UILayout bottomBarLayout = new UILayout(ui, bottomBarConstraints,
                LayoutType.Horizontal, LayoutAlign.Start);
            ui.AddElement(bottomBarLayout, "BottomBar");

            CreateUIDiceRollsLayout(bottomBarLayout);
            CreateUIEndTurnButton(bottomBarLayout);
        }

        private void CreateUIDiceLayoutAndNumRolls()
        {
            Texture2D diceIconTexture = game.Content.Load<Texture2D>
                (GameContent.TexturePath("DiceIconSpriteSheet"));
            SpriteFont uiFont = game.Content.Load<SpriteFont>
                (GameContent.FontPath("MainFont"));

            Constraints diceLayoutConstraints = new Constraints(
                new CenterConstraint(),
                new PercentConstraint(0.2f),
                new PercentConstraint(0.6f),
                new PercentConstraint(0.6f));
            UILayout diceLayout = new UILayout(ui, diceLayoutConstraints,
                LayoutType.Vertical, LayoutAlign.Start, 
                new PercentConstraint(0.025f));
            ui.AddElement(diceLayout, "DiceLayout");

            Constraints numRollsImageConstraints = new Constraints(
                new CenterConstraint(),
                new PixelConstraint(0.0f),
                new AspectConstraint(1.0f),
                new PercentConstraint(0.2f));
            UIImage numRollsImage = new UIImage(ui, numRollsImageConstraints,
                diceIconTexture, new Rectangle(0, 0, 32, 32));
            diceLayout.AddElement(numRollsImage);

            Constraints numRollsStringConstraints = new Constraints(
                new CenterConstraint(),
                new CenterConstraint(),
                new AspectConstraint(1.0f),
                new PercentConstraint(0.6f));
            UIString numRollsString = new UIString(ui, numRollsStringConstraints,
                uiFont, "0", Color.Black);
            numRollsImage.AddElement(numRollsString, "NumRolls");

            Constraints diceContainerConstraints = new Constraints(
                new CenterConstraint(),
                new PixelConstraint(0.0f),
                new PercentConstraint(1.0f),
                new PercentConstraint(0.75f));
            UILayout diceContainer = new UILayout(ui, diceContainerConstraints,
                new Color(0.7f, 0.7f, 0.7f, 0.5f), LayoutType.Vertical, 
                LayoutAlign.Center, new PercentConstraint(0.025f));
            diceLayout.AddElement(diceContainer, "DiceContainer");

            UICheckboxEventHandler numRollsEventHandler = new UICheckboxEventHandler(true);
            numRollsEventHandler.OnValueChange += (UIElement element, bool value) =>
            {
                diceContainer.IsVisible = value;
            };
            numRollsImage.EventHandler = numRollsEventHandler;
        }

        private void CreateUIDices()
        {
            Texture2D diceIconTexture = game.Content.Load<Texture2D>
                (GameContent.TexturePath("DiceIconSpriteSheet"));
            Texture2D diceFaceTexture = game.Content.Load<Texture2D>
                (GameContent.TexturePath("DiceFaceSpriteSheet"));
            SpriteFont uiFont = game.Content.Load<SpriteFont>
                (GameContent.FontPath("MainFont"));
            UILayout diceContainer = ui.GetElement<UILayout>
                ("DiceContainer");

            for (int i = 0; i < playerData.Dices.Count; ++i)
            {
                Dice dice = playerData.Dices[i];
                Constraints diceImageConstraints = new Constraints(
                    new PercentConstraint(0.025f),
                    new PixelConstraint(0.0f),
                    new AspectConstraint(1.0f),
                    new PercentConstraint(0.2f));
                UIImage diceImage = new UIImage(ui, diceImageConstraints,
                    diceIconTexture, dice.SourceRect);
                diceImage.Color   = dice.Color;
                diceImage.Color.A = 128;
                diceContainer.AddElement(diceImage);

                Constraints diceFacesLayoutConstraints = new Constraints(
                    new PercentConstraint(1.2f),
                    new CenterConstraint(),
                    new PercentConstraint(6.0f),
                    new PercentConstraint(0.8f));
                UILayout diceFacesLayout = new UILayout(ui, diceFacesLayoutConstraints,
                    LayoutType.Horizontal, LayoutAlign.Start, 
                    new PercentConstraint(0.1f));
                diceFacesLayout.IsVisible = false;
                diceImage.AddElement(diceFacesLayout);


                UIButtonEventHandler diceEventHandler = new UIButtonEventHandler();
                diceEventHandler.OnEnterHover += (UIElement element) =>
                {
                    diceFacesLayout.IsVisible = true;
                    element.Color.A = 255;
                };

                diceEventHandler.OnExitHover += (UIElement element) =>
                {
                    diceFacesLayout.IsVisible = false;
                    element.Color.A = 128;
                };

                diceEventHandler.OnPress += (UIElement element) =>
                {
                    RollDice(dice);
                };
                diceImage.EventHandler = diceEventHandler;

                foreach (PlayerSkill face in dice.Faces)
                {
                    Constraints diceFaceImageConstraints = new Constraints(
                        new PixelConstraint(0.0f),
                        new CenterConstraint(),
                        new AspectConstraint(1.0f),
                        new PercentConstraint(0.9f));

                    UIImage diceFaceImage = new UIImage(ui, diceFaceImageConstraints,
                        diceFaceTexture, face.SourceRect);
                    diceFacesLayout.AddElement(diceFaceImage);
                }
            }
        }

        private void ResetUIDices()
        {
            UILayout diceContainer = ui.GetElement<UILayout>
                ("DiceContainer");
            diceContainer.ClearElements();
        }

        private void CreateUIDiceRollsLayout(UIElement container)
        {
            Constraints diceRollsConstraints = new Constraints(
                new PixelConstraint(0.0f),
                new CenterConstraint(),
                new PercentConstraint(0.75f),
                new PercentConstraint(1.0f));
            UICardLayout diceRolls = new UICardLayout(ui, diceRollsConstraints);
            diceRolls.OnDropEvent += OnDropSkill;
            container.AddElement(diceRolls, "DiceRolls");
        }

        private void CreateUIEndTurnButton(UIElement container)
        {
            Texture2D uiTexture = game.Content.Load<Texture2D>(
                GameContent.TexturePath("UI"));
            SpriteFont uiFont   = game.Content.Load<SpriteFont>(
                GameContent.FontPath("MainFont"));

            Constraints endTurnContainerConstraints = new Constraints(
                new PixelConstraint(0.0f),
                new CenterConstraint(),
                new PercentConstraint(0.25f),
                new PercentConstraint(1.0f));
            UIElement endTurnContainer = new UIElement(ui, endTurnContainerConstraints);
            container.AddElement(endTurnContainer);

            Constraints endTurnButtonConstraints = new Constraints(
                new CenterConstraint(),
                new CenterConstraint(),
                new PercentConstraint(0.8f),
                new AspectConstraint(1.0f));
            UIImage endTurnButton = UIUtil.CreateCommonButton(ui,
                endTurnButtonConstraints, "End Turn", game.Content);
            endTurnContainer.AddElement(endTurnButton);

            UIButtonEventHandler endTurnButtonEvents = (UIButtonEventHandler)
                endTurnButton.EventHandler;
            endTurnButtonEvents.OnPress += (UIElement element) =>
            {
                stateMachine.ChangeState(State.StartingAITurn);
            };
        }

        private void CreateStateMachine()
        {
            stateMachine = new SimpleStateMachine<State>();
            stateMachine.AddState(State.PlayerRolling, 
                UpdatePlayerRolling, OnEnterPlayerRolling, OnExitPlayerRolling);
            stateMachine.AddState(State.PlayerTurn, 
                UpdatePlayerTurn, OnEnterPlayerTurn);
            stateMachine.AddState(State.ExecutingPlayerTurn, 
                UpdateExecutingPlayerTurn, OnEnterExecutingPlayerTurn);
            stateMachine.AddState(State.StartingAITurn,
                UpdateStartingAITurn);
            stateMachine.AddState(State.AITurn, 
                UpdateAITurn);
            stateMachine.AddState(State.ExecutingAITurn, 
                UpdateExecutingAITurn);
        }

        private void RegisterUpdateSystems()
        {
            updateSystems.RegisterSystem(new ScriptSystem(entityManager));
            updateSystems.RegisterSystem(new PhysicsSystem(entityManager,
                Vector2.Zero, 1.0f / 60.0f));
            updateSystems.RegisterSystem(new SpriteAnimationSystem(entityManager));
            updateSystems.RegisterSystem(new PreDrawSystem(entityManager, level));
            updateSystems.RegisterSystem(new HealthSystem(entityManager));
            updateSystems.RegisterSystem(new DeathSystem(gameWorld));

            PhysicsSystem physiscSystem = updateSystems.GetSystem<PhysicsSystem>();
            physiscSystem.Iterations = 1;

            updateSystems.EnableSystem<ScriptSystem>();
            updateSystems.EnableSystem<PhysicsSystem>();
            updateSystems.EnableSystem<SpriteAnimationSystem>();
            updateSystems.EnableSystem<PreDrawSystem>();
            updateSystems.EnableSystem<HealthSystem>();
            updateSystems.EnableSystem<DeathSystem>();
        }

        private void RegisterDrawSystems()
        {
            SpriteFont mainFont = game.Content.Load<SpriteFont>(
                GameContent.FontPath("MainFont"));

            drawSystems.RegisterSystem(new RenderSystem(entityManager,
                spriteBatch, parentState.Camera, mainFont));
            drawSystems.EnableSystem<RenderSystem>();
        }

        public override void OnEnter()
        {
            ContentManager content = game.Content;
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Play(music);

            string levelPath = parentState.GetNextLevel();
            level.Load(levelPath, updateSystems.GetSystem<PhysicsSystem>(),
                entityFactory);
            parentState.Camera.Position = new Vector2(
                level.Width * 0.5f,
                level.Height * 0.5f);

            ResetUIDices();
            CreateUIDices();
            SetPlayersSpawnPositions();
            stateMachine.SetState(State.PlayerRolling);

            DebugDraw.Camera = parentState.Camera;
            DebugLog.Info("OnEnter state: {0}", nameof(PlayGameDungeonState));
        }

        public override void OnExit()
        {
            MediaPlayer.Stop();
            DebugLog.Info("OnExit state: {0}", nameof(PlayGameDungeonState));
        }

        public override StateResult Update(GameTime gameTime)
        {
            float dt     = (float)gameTime.ElapsedGameTime.TotalSeconds;
            gameWorld.Dt = dt;

            if(KeyboardInput.IsKeyPressed(Keys.Escape))
                parentState.GameStates.PushState<PlayGamePauseState>();
            

            ui.Update();
            cameraController.Update(dt);
            stateMachine.Update(dt);
            updateSystems.UpdateSystems(dt);

            return StateResult.StopExecuting;
        }

        public override StateResult Draw(GameTime gameTime)
        {
            game.GraphicsDevice.Clear(new Color(24, 20, 37));
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            //Draw pre entities layer
            spriteBatch.Begin(parentState.Camera, samplerState: SamplerState.PointClamp);
            level.TileMap.DrawPreEntitiesLayer(parentState.Camera, spriteBatch);
            spriteBatch.End();

            //Draw all the entities
            drawSystems.UpdateSystems(dt);

            //Draw post entities layer and the current attack
            spriteBatch.Begin(parentState.Camera, samplerState: SamplerState.PointClamp,
                blendState: BlendState.NonPremultiplied);
            level.TileMap.DrawPostEntitiesLayer(parentState.Camera, spriteBatch);

            if (stateMachine.CurrentStateKey == State.ExecutingPlayerTurn &&
                selectedDiceRoll != null)
            {
                selectedDiceRoll.Draw(gameWorld, spriteBatch, selectedTarget);
            }
            spriteBatch.End();

            //Draw the UI
            spriteBatch.Begin(samplerState: SamplerState.PointWrap,
                blendState: BlendState.NonPremultiplied);
            ui.Draw(spriteBatch);
            spriteBatch.End();

            //var a = level.PathFindingMap.GetStartAndEndNodes( Vector2.Zero, 
            //    MouseInput.GetPosition(cameraController.Camera));
            //level.PathFindingMap.SolveDijkstra(null, a.Item1, null);
            //level.PathFindingMap.Draw();

            return StateResult.StopExecuting;
        }

        private void SetPlayersSpawnPositions()
        {
            int spawnsCount = level.SpawnPoints.Count;
            int spawnIndex  = Random.Shared.Next(spawnsCount);

            entityManager.ForEachEntity((Entity player) =>
            {
                if(player.HasTag(EntityTags.Player))
                {
                    player.Position = level.SpawnPoints[spawnIndex];
                    spawnIndex      = (spawnIndex + 1) % spawnsCount;
                }
            });
        }

        private void OnEnterPlayerRolling()
        {
            playerData.DiceRolls.Clear();
            playerData.NumRollsLeft = playerData.MaxRolls;

            UIString numRollsString = ui.GetElement<UIString>
                ("NumRolls");
            numRollsString.Text = playerData.NumRollsLeft.ToString();

            UILayout diceLayout = ui.GetElement<UILayout>
                ("DiceLayout");
            diceLayout.IsVisible = true;

            UIElement rollsLayout = ui.GetElement<UIElement>
                ("DiceRolls");
            rollsLayout.ClearElements();

            UIElement bottomBar = ui.GetElement<UIElement>
                ("BottomBar");
            bottomBar.IsEnabled = false;
            bottomBar.IsVisible = true;
        }

        private void OnExitPlayerRolling()
        {
            UIElement diceLayout = ui.GetElement<UIElement>
                ("DiceLayout");
            diceLayout.IsVisible = false;
        }

        private void RollDice(Dice dice)
        {
            AddDiceRoll(dice.Roll());

            playerData.NumRollsLeft--;

            UIString numRollsString = ui.GetElement<UIString>
                ("NumRolls");
            numRollsString.Text     = playerData.NumRollsLeft.ToString();
        }

        private void UpdatePlayerRolling(float _)
        {
            if (playerData.NumRollsLeft == 0)
            {
                stateMachine.ChangeState(State.PlayerTurn);
            }
        }

        private void AddDiceRoll(PlayerSkill roll)
        {
            Texture2D diceFaceTexture = game.Content.Load<Texture2D>
                (GameContent.TexturePath("DiceFaceSpriteSheet"));
            UICardLayout rollsLayout   = ui.GetElement<UICardLayout>
                ("DiceRolls");

            Constraints rollConstraints = new Constraints(
                new PixelConstraint(0.0f),
                new CenterConstraint(),
                new AspectConstraint(1.0f),
                new PercentConstraint(0.9f));
            UIImage rollImage = new UIImage(ui, rollConstraints, 
                diceFaceTexture, roll.SourceRect);

            rollsLayout.AddElement(rollImage);
            playerData.DiceRolls.Add(roll);
        }

        private void OnEnterPlayerTurn()
        {
            selectedTarget = null;

            UIElement bottomBar = ui.GetElement<UIElement>
                ("BottomBar");
            bottomBar.IsEnabled = true;
            bottomBar.IsVisible = true;
        }

        private void UpdatePlayerTurn(float _)
        {
            UICardLayout rollsLayout = ui.GetElement<UICardLayout>
                ("DiceRolls");

            if(rollsLayout.SelectedElementChanged)
            {
                if(rollsLayout.SelectedElement == null)
                    ClearCharactersCmpSelectState();
                else
                    SetCharactersCmpSelectState();
            }
        }

        private void ClearCharactersCmpSelectState()
        {
            entityManager.ForEachComponent((Entity e, CharacterCmp chara) =>
            {
                chara.SelectState = SelectState.None;
            });
        }

        private void OnDropSkill(UIElement droppedElement)
        {
            UICardLayout diceRolls = ui.GetElement<UICardLayout>
                ("DiceRolls");
            int index = diceRolls.GetElementIndex(droppedElement);

            selectedDiceRoll = playerData.DiceRolls[index];
            selectedTarget = GetDiceRollSelectedTarget();

            if (selectedTarget != null)
            {
                ClearCharactersCmpSelectState();
                selectedDiceRoll.Init();
                playerData.DiceRolls.RemoveAt(index);
                diceRolls.RemoveElement(droppedElement);

                stateMachine.ChangeState(State.ExecutingPlayerTurn);
            }
            else
            {
                selectedDiceRoll = null;
            }
        }

        private void SetCharactersCmpSelectState()
        {
            UICardLayout rollsLayout = ui.GetElement<UICardLayout>
                ("DiceRolls");

            int index = rollsLayout.GetElementIndex
                        (rollsLayout.SelectedElement);
            PlayerSkill skill = playerData.DiceRolls[index];

            entityManager.ForEachComponent((Entity e, CharacterCmp chara) =>
            {
                if (skill.CanUseSkill(chara))
                {
                    chara.SelectState = SelectState.Hover;
                }
            });
        }

        private Entity GetDiceRollSelectedTarget()
        {
            Entity target = null;
            Vector2 mouse = MouseInput.GetPosition(parentState.Camera);

            entityManager.ForEachComponent((Entity e, CharacterCmp chara) =>
            {
                if (selectedDiceRoll.CanUseSkill(chara))
                {
                    ColliderCmp col  = entityManager.GetComponent<ColliderCmp>(e);
                    CircleCollider c = (CircleCollider)col.Collider;

                    if (CollisionTester.PointVsCircle(mouse,
                        col.Transform.CachedWorldPosition, c.CachedRadius))
                    {
                        target = e;
                        return;
                    }
                }
            });

            return target;
        }

        private void OnEnterExecutingPlayerTurn()
        {
            UIElement bottomBar = ui.GetElement<UIElement>
                        ("BottomBar");
            bottomBar.IsVisible = false;
        }

        private void UpdateExecutingPlayerTurn(float dt)
        {
            if (selectedDiceRoll == null)
            {
                if (TurnHasFinished())
                {
                    if (AllEntitiesWithTagsAreDead(EntityTags.Player))
                    {
                        parentState.GameStates.PopAllActiveStates();
                        parentState.GameStates.PushState<PlayGameLoseState>();
                    }
                    else if (AllEntitiesWithTagsAreDead(EntityTags.Enemy))
                    {
                        parentState.GameStates.PopAllActiveStates();
                        if (parentState.HasNextLevel())
                            parentState.GameStates.PushState<PlayGameDungeonState>();
                        else
                            parentState.GameStates.PushState<PlayGameWinState>();
                    }
                    if (playerData.DiceRolls.Count == 0)
                    {
                        stateMachine.ChangeState(State.StartingAITurn);
                    }
                    else
                    {
                        stateMachine.ChangeState(State.PlayerTurn);
                    }
                }
            }
            else
            {
                SkillState state = selectedDiceRoll.Update(gameWorld, 
                    selectedTarget);

                CharacterCmp chara = entityManager.GetComponent<CharacterCmp>
                    (selectedTarget);
                if (state == SkillState.Finished)
                {
                    chara.SelectState = SelectState.None;
                    selectedDiceRoll  = null;
                    selectedTarget    = null;
                }
                else
                {
                    chara.SelectState = SelectState.Selected;
                }
            }
        }

        private void UpdateStartingAITurn(float _)
        {
            aiData.Enemies.Clear();
            entityManager.ForEachComponent((Entity e, AICmp ai) =>
            {
                aiData.Enemies.Add(new AIEntity(e, ai));
            });

            UIElement bottomBar = ui.GetElement<UIElement>
                ("BottomBar");
            bottomBar.IsVisible = false;

            stateMachine.ChangeState(State.AITurn);
        }

        private void UpdateAITurn(float _)
        {
            int numEnemies = aiData.Enemies.Count;

            if (numEnemies > 0)
            {
                int index = Random.Shared.Next(numEnemies);
                aiData.CurrentEnemy = aiData.Enemies[index];
                aiData.Enemies.RemoveAt(index);
                aiData.SelectWaitTime = AIData.SELECT_MAX_WAIT_TIME;

                AICmp ai = aiData.CurrentEnemy.AI;
                Entity enemy = aiData.CurrentEnemy.Entity;

                DecisionTreeNode skill = ai.DecisionTree.Run(gameWorld, enemy, ai);
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

        public void UpdateExecutingAITurn(float dt)
        {
            AICmp ai = aiData.CurrentEnemy.AI;
            Entity enemy = aiData.CurrentEnemy.Entity;
            EnemySkill skill = aiData.CurrentEnemySkill;

            if(aiData.SelectWaitTime > 0.0f)
            {
                aiData.SelectWaitTime -= dt;
                return;
            }

            if (skill != null)
            {
                SkillState skillState = skill.Execute(gameWorld, enemy, ai);

                if (skillState == SkillState.Finished)
                    aiData.CurrentEnemySkill = null;
            }
            else if (TurnHasFinished())
            {
                CharacterCmp charCmp = entityManager.
                    GetComponent<CharacterCmp>(enemy);
                charCmp.SelectState = SelectState.None;

                if(AllEntitiesWithTagsAreDead(EntityTags.Player))
                {
                    parentState.GameStates.PopAllActiveStates();
                    parentState.GameStates.PushState<PlayGameLoseState>();
                }
                else if(AllEntitiesWithTagsAreDead(EntityTags.Enemy))
                {
                    parentState.GameStates.PopAllActiveStates();
                    if(parentState.HasNextLevel())
                        parentState.GameStates.PushState<PlayGameDungeonState>();
                    else
                        parentState.GameStates.PushState<PlayGameWinState>();
                }
                else
                {
                    stateMachine.ChangeState(State.AITurn);
                }
            }
        }

        private bool AllEntitiesWithTagsAreDead(EntityTags tags)
        {
            int count = 0;

            entityManager.ForEachEntity((Entity e) =>
            {
                if (e.HasTag(tags)) ++count;
            });

            return count == 0;
        }

        private bool TurnHasFinished()
        {
            bool ret = true;

            entityManager.ForEachComponent((Entity e, PhysicsCmp physics) =>
            {
                const float MIN_VEL = 1.5f;
                float l = physics.LinearVelocity.LengthSquared();

                if (l > MIN_VEL * MIN_VEL)
                {
                    ret = false;
                    return;
                }
                else
                {
                    physics.LinearVelocity = Vector2.Zero;
                }
            });

            entityManager.ForEachComponent((Entity e, DeathCmp death) =>
            {
                if(death.State != DeathState.Alive)
                {
                    ret = false;
                    return;
                }
            });

            return ret;
        }
    }
}
