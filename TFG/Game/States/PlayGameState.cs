using System;
using Microsoft.Xna.Framework;
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
using System.Reflection.Metadata;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;

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
        private TileMap tileMap;

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
            this.camera           = new Camera2D(game.Screen);
            this.cameraController = new CameraController(camera);
            this.tileMap          = new TileMap();

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
        }

        private void RegisterUpdateSystems()
        {
            updateSystems.RegisterSystem(new ScriptSystem(entityManager));
            updateSystems.RegisterSystem(new PhysicsSystem(entityManager, 
                Vector2.Zero, 1.0f / 60.0f));

            PhysicsSystem physiscSystem = updateSystems.GetSystem<PhysicsSystem>();
            physiscSystem.Iterations    = 1;

            updateSystems.EnableSystem<ScriptSystem>();
            updateSystems.EnableSystem<PhysicsSystem>();
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
            }
            //##### #####

            cameraController.Update(dt);
            updateSystems.UpdateSystems();

            return false;
        }

        public override bool Draw(GameTime gameTime)
        {
            game.GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin(camera, samplerState: SamplerState.PointClamp);
            tileMap.DrawPreEntitiesLayer(camera, spriteBatch);
            spriteBatch.End();

            drawSystems.UpdateSystems();

            spriteBatch.Begin(camera, samplerState: SamplerState.PointClamp);
            tileMap.DrawPostEntitiesLayer(camera, spriteBatch);
            spriteBatch.End();

            return false;
        }

        public override void OnEnter()
        {
            ContentManager content = game.Content;

            tileMap.Load(game.Content.Load<Texture2D>("Dungeon_Tileset"),
                "../../../Content/levels/Map1.json", 
                updateSystems.GetSystem<PhysicsSystem>());

            playerPlatformTexture = content.Load<Texture2D>("PlayerPlatform");
            playerTexture         = content.Load<Texture2D>("Player");


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
            phy.LinearDamping = 1.0f;
            ColliderCmp col = entityManager.AddComponent(e, new ColliderCmp(
                new CircleCollider(16.0f), new Material(1.0f, 0.0f, 0.0f),
                CollisionBitmask.Player, CollisionBitmask.All));
            SpriteCmp spr2 = entityManager.AddComponent(e, new SpriteCmp(playerTexture));
            spr2.Transform.LocalPosition = new Vector2(
                -playerTexture.Width * 0.5f,
                -playerTexture.Height);

            SpriteCmp spr1 = entityManager.AddComponent(e, new SpriteCmp(playerPlatformTexture));
            spr1.Transform.LocalPosition = new Vector2(
                -playerPlatformTexture.Width * 0.5f,
                -playerPlatformTexture.Height * 0.5f);

            if (player == null) player = e;
            else
            {
                Color[] colors = new Color[]
                    { Color.Red, Color.Green, Color.Blue, Color.Yellow };
                Color color = colors[Random.Shared.Next(colors.Length)];
                spr1.Color = color;
                spr2.Color = color;
            }
        }
    }
}