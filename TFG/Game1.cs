using System;
using System.Collections.Generic;
using Engine.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Engine.Graphics;
using Engine.Ecs;
using Cmps;
using Core;
using Systems;
using Physics;
using Engine.Debug;

namespace TFG
{
    struct Tile
    {
        public Vector2 Position;
        public Rectangle Source;

        public Tile(Vector2 position, Rectangle source)
        {
            Position = position;
            Source   = source;
        }
    }

    public class Game1 : Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private GameStateStack gameStates;

        private RenderScreen screen;
        private Camera2D camera;
        private Texture2D tileSetTexture;
        private List<Tile> tiles;
        private ShapeBatch shapeBatch;
        private EntityManager<Entity> entityManager;
        private SystemManager updateSystems;
        private SystemManager drawSystems;
        private SpriteFont font;

        public const int WindowWidth  = 1024;
        public const int WindowHeight = 720;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible  = true;
            IsFixedTimeStep = true;
            

            graphics.PreferredBackBufferWidth  = WindowWidth;
            graphics.PreferredBackBufferHeight = WindowHeight;
            graphics.HardwareModeSwitch = false;
            graphics.DeviceReset += Graphics_DeviceReset;
            
            //TODO: REMOVE
            Window.ClientSizeChanged += (object sender, EventArgs args) =>
            {
                screen.UpdateDestinationRect();
                DebugLog.Warning("Resize {0}:{1}", Window.ClientBounds.Width, 
                    Window.ClientBounds.Height);
            };

        }

        protected override void Initialize()
        {
            DebugLog.Info("Initializing");

            gameStates = new GameStateStack();
            screen = new RenderScreen(GraphicsDevice, 800, 600);
            camera = new Camera2D(screen);
            entityManager = new EntityManager<Entity>();
            drawSystems = new SystemManager();
            updateSystems = new SystemManager();

            entityManager.RegisterComponent<ScriptCmp>();
            entityManager.RegisterComponent<SpriteCmp>();
            entityManager.RegisterComponent<PhysicsCmp>();
            entityManager.RegisterComponent<ColliderCmp>();
            entityManager.RegisterComponent<TriggerColliderCmp>();

            DebugTimer.Register("Update", 120);
            DebugTimer.Register("Draw",   50);
            DebugTimer.Register("Physics", 120);
            DebugDraw.Init(GraphicsDevice);
            DebugDraw.RegisterLayer(PhysicsSystem.DEBUG_DRAW_LAYER, 4.0f, 2.0f, 16);

            camera.RotationAnchor = new Vector2(0.5f, 0.5f);
            camera.PositionAnchor = new Vector2(0.5f, 0.5f);
            PrintSizes();

            Random rnd = new Random();
            tiles = new List<Tile>();
            int numTiles = screen.Width / 16;

            for (int x = 0; x < numTiles; ++x)
            {
                for(int y = 0; y < numTiles; ++y)
                {
                    Tile tile = new Tile();
                    tile.Position = new Vector2(x * 16.0f, y * 16.0f);
                    tile.Source = new Rectangle(rnd.Next(30) * 16,
                        rnd.Next(27) * 16, 16, 16);
                    //tiles.Add(tile);
                }
            }

            base.Initialize();
        }

        protected override void LoadContent()
        {
            DebugLog.Info("Loading content");

            spriteBatch = new SpriteBatch(GraphicsDevice);
            shapeBatch = new ShapeBatch(GraphicsDevice);
            tileSetTexture = Content.Load<Texture2D>("TileSet");
            //blankTexture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            //blankTexture.SetData(new[] { Color.White });
            font = Content.Load<SpriteFont>("DebugFont");

            drawSystems.RegisterSystem(new RenderSystem(entityManager, spriteBatch, shapeBatch, camera));
            drawSystems.EnableSystem<RenderSystem>();

            updateSystems.RegisterSystem(new PhysicsSystem(entityManager,
                new Vector2(0.0f, 250.0f), 1.0f / 60.0f));
            updateSystems.RegisterSystem(new ScriptSystem(entityManager));
            updateSystems.EnableSystem<ScriptSystem>();
            updateSystems.EnableSystem<PhysicsSystem>();
            PhysicsSystem physiscSystem = updateSystems.GetSystem<PhysicsSystem>();
            physiscSystem.Iterations = 1;
            physiscSystem.Gravity = new Vector2(0.0f, 200.0f);

            Entity e = entityManager.CreateEntity();
            PhysicsCmp phy = entityManager.AddComponent(e, new PhysicsCmp());
            phy.Inertia = PhysicsCmp.CalculateRectangleInertia(32.0f, 32.0f, 1.0f);
            ColliderCmp cm  =entityManager.AddComponent(e, 
                new ColliderCmp(new CircleCollider(32.0f), CollisionBitmask.Player,
                CollisionBitmask.All));
            cm.Transform.LocalRotation = MathUtil.PI_OVER_2 * 0.4f;

            Random rnd = new Random();
            const int RECT_WIDTH  = 40;
            const int RECT_HEIGHT = 30;
            for(int i = 0;i < RECT_HEIGHT; ++i)
            {
                for(int j = 0;j < RECT_WIDTH; ++j)
                {
                    float x = (j - ((RECT_WIDTH / 2))) * 16.0f;
                    float y = (i - ((RECT_HEIGHT / 2))) * 16.0f;
                    Vector2 pos = new Vector2(x, y);

                    if (j == 0 || i == 0 || 
                        j == RECT_WIDTH - 1 || 
                        i == RECT_HEIGHT - 1)
                    {
                        tiles.Add(new Tile(pos, new Rectangle(1 * 16, 4 * 16, 16, 16)));
                    }
                    else
                    {
                        tiles.Add(new Tile(pos, new Rectangle(5 * 16, 1 * 16, 16, 16)));
                    }
                }
            }

            //TOP WALL
            StaticCollider col = physiscSystem.AddStaticCollider(
                new RectangleCollider(RECT_WIDTH * 16.0f, 16.0f), Material.Rubber,
                CollisionBitmask.Wall, CollisionBitmask.All);
            col.Position = new Vector2(0.0f, -RECT_HEIGHT * 16.0f * 0.5f + 16.0f * 0.5f);
            //BOTTOM WALL
            col = physiscSystem.AddStaticCollider(
                new RectangleCollider(RECT_WIDTH * 16.0f, 16.0f), Material.Rubber,
                CollisionBitmask.Wall, CollisionBitmask.All);
            col.Position = new Vector2(0.0f, +RECT_HEIGHT * 16.0f * 0.5f - 16.0f * 0.5f);
            //LEFT WALL
            col = physiscSystem.AddStaticCollider(
                new RectangleCollider(16.0f, RECT_HEIGHT * 16.0f), Material.Rubber,
                CollisionBitmask.Wall, CollisionBitmask.All);
            col.Position = new Vector2(-RECT_WIDTH * 16.0f * 0.5f + 16.0f * 0.5f, 0.0f);
            //RIGHT WALL
            col = physiscSystem.AddStaticCollider(
                new RectangleCollider(16.0f, RECT_HEIGHT * 16.0f), Material.Rubber,
                CollisionBitmask.Wall, CollisionBitmask.All);
            col.Position = new Vector2(+RECT_WIDTH * 16.0f * 0.5f - 16.0f * 0.5f, 0.0f);
        }

        protected override void Update(GameTime gameTime)
        {
            DebugTimer.Start("Update");

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || 
                Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            float dt = (float) gameTime.ElapsedGameTime.TotalSeconds;
            Input.Update();
            gameStates.Update();
            gameStates.UpdateActiveStates(gameTime);
            HandleCameraMovement(dt);

            updateSystems.UpdateSystems();
            DebugTimer.Stop("Update");

            base.Update(gameTime);
        }

        private void Graphics_DeviceReset(object sender, EventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Reset");
            PrintSizes();
            Console.WriteLine("################");
        }

        public void HandleCameraMovement(float dt)
        {
            if (KeyboardInput.IsKeyPressed(Keys.F))
            {
                graphics.HardwareModeSwitch = false;

                graphics.ToggleFullScreen();
                PrintSizes();
            }

            if (KeyboardInput.IsKeyPressed(Keys.D5))
            {
                if (screen.Width == 800)
                    screen.Resize(1024, 720);
                else
                    screen.Resize(800, 600);
            }

            if (MouseInput.ScrollHasChanged())
            {
                int sign = -MathF.Sign(MouseInput.ScrollValueDiff);
                camera.Zoom += 0.1f * sign;
            }

            const float CAM_SPEED = 300.0f;
            Vector2 vel = Vector2.Zero;
            if (KeyboardInput.IsKeyDown(Keys.W))
                vel.Y -= 1.0f;
            if (KeyboardInput.IsKeyDown(Keys.S))
                vel.Y += 1.0f;
            if (KeyboardInput.IsKeyDown(Keys.A))
                vel.X -= 1.0f;
            if (KeyboardInput.IsKeyDown(Keys.D))
                vel.X += 1.0f;

            if (vel != Vector2.Zero)
            {
                vel.Normalize();
                vel *= CAM_SPEED * dt;
                camera.Position += vel;
            }
        }

        public void PrintSizes(ConsoleColor color = ConsoleColor.Cyan)
        {
            int w1 = GraphicsDevice.Viewport.Width;
            int h1 = GraphicsDevice.Viewport.Height;
            int w2 = graphics.PreferredBackBufferWidth;
            int h2 = graphics.PreferredBackBufferHeight;
            int w3 = GraphicsDevice.PresentationParameters.BackBufferWidth;
            int h3 = GraphicsDevice.PresentationParameters.BackBufferHeight;
            int w4 = GraphicsDevice.PresentationParameters.Bounds.Width;
            int h4 = GraphicsDevice.PresentationParameters.Bounds.Height;

            Console.ForegroundColor = color;
            Console.WriteLine("Viewport (W:{0},H{1})", w1, h1);
            Console.WriteLine("Graphics (W:{0},H{1})", w2, h2);
            Console.WriteLine("Backbuffer (W:{0},H{1})", w3, h3);
            Console.WriteLine("Bounds (W:{0},H{1})", w4, h4);
        }

        protected override void Draw(GameTime gameTime)
        {
            DebugTimer.Start("Draw");
            screen.Attach();
            GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin(camera, samplerState: SamplerState.PointClamp);
            foreach (Tile t in tiles)
            {
                spriteBatch.Draw(tileSetTexture, t.Position, 
                    t.Source, Color.White);
            }

            spriteBatch.End();


            shapeBatch.Begin(camera);

            shapeBatch.DrawLine(new Vector2(0.0f, 0.0f),
                new Vector2(0.0f, 50.0f), 4.0f, new Color(0, 255, 0));
            shapeBatch.DrawLine(new Vector2(0.0f, 0.0f),
                new Vector2(50.0f, 0.0f), 4.0f, Color.Red);


            shapeBatch.End();
            drawSystems.UpdateSystems();
            shapeBatch.Begin(camera);

            DebugDraw.Draw(camera);
            DebugTimer.Stop("Draw");
            DebugTimer.Draw(spriteBatch, font);
            shapeBatch.End();

            //gameStates.DrawActiveStates(gameTime, spriteBatch);

            screen.Present(spriteBatch, SamplerState.PointClamp);

            base.Draw(gameTime);
        }

        protected override void OnActivated(object sender, EventArgs args)
        {
            DebugLog.Info("Game is focused");

            base.OnActivated(sender, args);
        }

        protected override void OnDeactivated(object sender, EventArgs args)
        {
            DebugLog.Info("Game lost focus");
            
            base.OnDeactivated(sender, args);
        }
    }
}