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


namespace States
{
    public class PlayGameState : GameState
    {
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

        private Texture2D playerTexture;
        private Texture2D playerPlatformTexture;
        private Entity player;


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

            RegisterEntityComponents();
            RegisterUpdateSystems();
            RegisterDrawSystems();
        }

        private void RegisterEntityComponents() 
        {
            entityManager.RegisterComponent<ScriptCmp>();
            entityManager.RegisterComponent<SpriteCmp>();
            entityManager.RegisterComponent<PhysicsCmp>();
            entityManager.RegisterComponent<ColliderCmp>();
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

            PhysicsSystem physiscSystem = updateSystems.GetSystem<PhysicsSystem>();
            physiscSystem.Iterations    = 1;

            updateSystems.EnableSystem<ScriptSystem>();
            updateSystems.EnableSystem<PhysicsSystem>();
            updateSystems.EnableSystem<SpriteAnimationSystem>();
            updateSystems.EnableSystem<PreDrawSystem>();
        }

        private void RegisterDrawSystems()
        {
            drawSystems.RegisterSystem(new RenderSystem(entityManager, 
                spriteBatch, camera));
            drawSystems.EnableSystem<RenderSystem>();
        }

        public override bool Update(GameTime gameTime)
        {
            float dt = (float) gameTime.ElapsedGameTime.TotalSeconds;

            //##### BORRAR #####
            if (MouseInput.IsRightButtonPressed())
            {
                CreatePlayer();
            }

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

                AnimationControllerCmp animController = 
                    entityManager.GetComponent<AnimationControllerCmp>(player);

                if(KeyboardInput.IsKeyReleased(Keys.L))
                {
                    bool isLooped = (animController.PlayState & AnimationPlayState.Loop) != 0;
                    animController.SetLooping(!isLooped);
                }

                if (KeyboardInput.IsKeyReleased(Keys.R))
                {
                    bool isReversed = (animController.PlayState & AnimationPlayState.Reverse) != 0;
                    animController.SetReverse(!isReversed);
                }

                if(KeyboardInput.IsKeyPressed(Keys.P))
                {
                    animController.IsPaused = !animController.IsPaused;
                }

                if(KeyboardInput.IsKeyPressed(Keys.I))
                {
                    animController.PlaySpeedMult += 0.5f;
                }

                if (KeyboardInput.IsKeyPressed(Keys.K))
                {
                    animController.PlaySpeedMult -= 0.5f;
                }
            }
            //##### #####

            cameraController.Update(dt);
            updateSystems.UpdateSystems(dt);

            return false;
        }

        public override bool Draw(GameTime gameTime)
        {
            game.GraphicsDevice.Clear(Color.Black);
            float dt = (float) gameTime.ElapsedGameTime.TotalSeconds;

            spriteBatch.Begin(camera, samplerState: SamplerState.PointClamp);
            level.TileMap.DrawPreEntitiesLayer(camera, spriteBatch);
            spriteBatch.End();

            drawSystems.UpdateSystems(dt);

            spriteBatch.Begin(camera, samplerState: SamplerState.PointClamp);
            level.TileMap.DrawPostEntitiesLayer(camera, spriteBatch);
            spriteBatch.End();

            return false;
        }

        public override void OnEnter()
        {
            ContentManager content = game.Content;

            level.Load("../../../Content/levels/Map1.m",
                updateSystems.GetSystem<PhysicsSystem>(), 
                entityFactory);

            playerPlatformTexture = content.Load<Texture2D>("EntityPlatform");
            playerTexture         = content.Load<Texture2D>("PlayerSpriteSheet");


            DebugDraw.Camera = camera;
            DebugLog.Info("OnEnter state: {0}", nameof(PlayGameState));
        }

        public override void OnExit()
        {
            DebugLog.Info("OnExit state: {0}", nameof(PlayGameState));
        }

        //BORRAR
        private void CreatePlayer()
        {
            Vector2 pos = MouseInput.GetPosition(camera);
            Entity e = entityManager.CreateEntity();

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

            SpriteCmp platformSpr = entityManager.AddComponent(e, new SpriteCmp(playerPlatformTexture));
            platformSpr.Transform.LocalPosition = new Vector2(
                -playerPlatformTexture.Width * 0.5f,
                -playerPlatformTexture.Height * 0.5f);
            platformSpr.LayerOrder = LayerOrder.AlwaysBottom;

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

            if (player == null) player = e;
            else
            {
                Color[] colors = new Color[]
                    { Color.Red, Color.Green, Color.Blue, Color.Yellow };
                Color color       = colors[Random.Shared.Next(colors.Length)];
                playerSpr.Color   = color;
                platformSpr.Color = color;
            }
        }
    }
}