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
    class Transform
    {
        public Vector2 LocalPosition;
        public Vector2 LocalScale;
        public float LocalRotation;

        private Vector2 worldPosition;
        private Vector2 worldScale;
        private float worldRotation;
        
        public Vector2 WorldPosition { get { return worldPosition; } }
        public Vector2 WorldScale { get { return worldScale; } }
        public float WorldRotation { get { return worldRotation; } }

        public Transform()
        {
            LocalPosition = worldPosition = new Vector2(0.0f, 0.0f);
            LocalScale = worldScale = new Vector2(1.0f, 1.0f);
            LocalRotation = worldRotation = 0.0f;
        }

        public void TransformBy(Transform other)
        {
            float cos = MathF.Cos(other.LocalRotation);
            float sin = MathF.Sin(other.LocalRotation);

            worldScale = LocalScale * other.LocalScale;
            worldRotation = LocalRotation + other.LocalRotation;

            float scaledX = other.LocalScale.X * LocalPosition.X;
            float scaledY = other.LocalScale.Y * LocalPosition.Y;

            worldPosition = new Vector2(
                (scaledX * cos - scaledY * sin) + other.LocalPosition.X,
                (scaledX * sin + scaledY * cos) + other.LocalPosition.Y); 
        }
    }

    struct Tile
    {
        public Vector2 Position;
        public Rectangle Source;
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
        private Entity player;
        private SpriteFont font;

        Transform child;
        Transform parent;

        const float ParentSize = 64.0f;
        const float ChildSize = 32.0f;

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
                DebugLog.LogWarning("Resize {0}:{1}", Window.ClientBounds.Width, 
                    Window.ClientBounds.Height);
            };

        }

        protected override void Initialize()
        {
            DebugLog.LogInfo("Initializing");

            gameStates = new GameStateStack();
            screen = new RenderScreen(GraphicsDevice, 800, 600);
            camera = new Camera2D(screen);
            parent = new Transform();
            child  = new Transform();
            entityManager = new EntityManager<Entity>();
            drawSystems = new SystemManager();
            updateSystems = new SystemManager();

            entityManager.RegisterComponent<SpriteCmp>();
            entityManager.RegisterComponent<PhysicsCmp>();
            entityManager.RegisterComponent<CollisionCmp>();

            DebugTimer.Register("Update", 120);
            DebugTimer.Register("Draw",   50);
            DebugTimer.Register("Physics", 120);

            //camera.ViewportPosition = new Vector2(0.0f, 0.0f);
            //camera.ViewportSize = new Vector2(1.0f, 1.0f);
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
            DebugLog.LogInfo("Loading content");

            spriteBatch = new SpriteBatch(GraphicsDevice);
            shapeBatch = new ShapeBatch(GraphicsDevice);
            tileSetTexture = Content.Load<Texture2D>("TileSet");
            font = Content.Load<SpriteFont>("DebugFont");
            // TODO: use this.Content to load your game content here

            float playerWidth = 64.0f;
            float playerHeight = 32.0f;
            player = entityManager.CreateEntity();
            player.Position = new Vector2(0, -256.0f);
            SpriteCmp spriteCmp1 = entityManager.AddComponent(player, 
                new SpriteCmp(tileSetTexture, new Rectangle(16, 16, 16, 16)));
            spriteCmp1.Transform.LocalPosition = new Vector2(0.0f,  32.0f);
            spriteCmp1.Origin = new Vector2(8.0f, 8.0f);
            SpriteCmp spriteCmp2 = entityManager.AddComponent(player,
                new SpriteCmp(tileSetTexture, new Rectangle(16, 32, 16, 16)));
            spriteCmp2.Transform.LocalPosition = new Vector2(0.0f, -32.0f);
            spriteCmp2.Origin = new Vector2(8.0f, 8.0f);
            PhysicsCmp physicsCmp = entityManager.AddComponent(player, new PhysicsCmp());
            physicsCmp.MaxLinearVelocity = new Vector2(300.0f, 300.0f);
            physicsCmp.GravityMultiplier = 0.0f;
            //CollisionCmp collisionCmp1 = entityManager.AddComponent(player,
            //    new CollisionCmp(new CircleCollider(32.0f)));
            //collisionCmp1.Transform.LocalPosition = new Vector2(50.0f, 0.0f);
            physicsCmp.Restitution = 1.0f;
            physicsCmp.Mass = 0.0f;
            
            physicsCmp.Inertia = (1.0f / 12.0f) * 1.0f * 
                (playerWidth * playerWidth + playerHeight * playerHeight);

            CollisionCmp collisionCmp2 = entityManager.AddComponent(player,
                new CollisionCmp(new RectangleCollider(64.0f, 32.0f)));
            collisionCmp2.Transform.LocalPosition = new Vector2(0.0f, 0.0f);
            

            Entity block   = entityManager.CreateEntity();
            block.Position = new Vector2(0.0f, 16.0f * 20.0f);
            PhysicsCmp blockPhysics = entityManager.AddComponent(block, new PhysicsCmp());
            blockPhysics.GravityMultiplier = 0.0f;
            blockPhysics.Mass = 0.0f;
            blockPhysics.Inertia = 0.0f;
            CollisionCmp blockCollision = entityManager.AddComponent(block,
                new CollisionCmp(new RectangleCollider(1024.0f, 128.0f)));
            blockPhysics.Restitution = 0.2f;

            Random rnd = new Random();
            const int COLUMNS = 0;// 20;
            const int ROWS    = 0;// 5;
            for(int i = 0;i < COLUMNS; ++i)
            {
                for(int j = 0;j < ROWS; ++j)
                {
                    float x = (i - ((COLUMNS / 2) - 1)) * 32.0f;
                    float y = (j - ((ROWS / 2) - 1)) * 32.0f;

                    Entity e = entityManager.CreateEntity();
                    e.Position = new Vector2(x, y);
                    PhysicsCmp phy = entityManager.AddComponent(e, new PhysicsCmp());
                    phy.Restitution = 0.0f;
                    //phy.Inertia = 2000.0f;

                    SpriteCmp spr = entityManager.AddComponent(e,
                        new SpriteCmp(tileSetTexture, new Rectangle(
                            rnd.Next(3) * 16,
                            rnd.Next(15) * 16, 16, 16)));
                    spr.Origin = new Vector2(8.0f, 8.0f);
                    entityManager.AddComponent(e,
                        new CollisionCmp(new RectangleCollider(16.0f, 16.0f)));
                }
            }
           

            drawSystems.RegisterSystem(new RenderSystem(entityManager, spriteBatch, shapeBatch, camera));
            drawSystems.EnableSystem<RenderSystem>();

            updateSystems.RegisterSystem(new PhysicsSystem(entityManager, 
                new Vector2(0.0f, 250.0f), 1.0f / 60.0f));
            updateSystems.EnableSystem<PhysicsSystem>();
            updateSystems.GetSystem<PhysicsSystem>().Iterations = 1;
        }

        private void CreateCircleEntity()
        {
            Entity e = entityManager.CreateEntity();
            e.Position = MouseInput.GetPosition(camera);

            PhysicsCmp phy = entityManager.AddComponent(e, new PhysicsCmp());
            phy.Restitution = 0.5f;

            entityManager.AddComponent(e,
                new CollisionCmp(new CircleCollider(8.0f)));
        }

        private void CreateRectangleEntity()
        {
            Random rnd = new Random();
            const float MIN_SIZE = 16.0f;
            const float MAX_SIZE = 32.0f;
            float width = (float)rnd.NextDouble() * (MAX_SIZE - MIN_SIZE) + MIN_SIZE;
            float height = (float)rnd.NextDouble() * (MAX_SIZE - MIN_SIZE) + MIN_SIZE;
            Entity e = entityManager.CreateEntity();
            e.Position = MouseInput.GetPosition(camera);

            PhysicsCmp phy = entityManager.AddComponent(e, new PhysicsCmp());
            phy.Restitution = 0.5f;
            phy.Inertia = (1.0f / 12.0f) * phy.Mass * (width * width + height * height);

            entityManager.AddComponent(e,
                new CollisionCmp(new RectangleCollider(width, height)));
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
            
            if(KeyboardInput.IsKeyPressed(Keys.F))
            {
                graphics.HardwareModeSwitch = false;

                graphics.ToggleFullScreen();
                PrintSizes();
            }

            if (KeyboardInput.IsKeyPressed(Keys.Q))
            {
                Vector2 pos = MouseInput.GetPosition();
                PrintSizes(ConsoleColor.Yellow);
            }

            if(MouseInput.ScrollHasChanged())
            {
                int sign = -MathF.Sign(MouseInput.ScrollValueDiff);
                camera.Zoom += 0.1f * sign;
            }

            if(KeyboardInput.IsKeyPressed(Keys.D1))
            {
                camera.Zoom += 1f;
                Console.WriteLine( camera.Zoom);
            }

            if (KeyboardInput.IsKeyPressed(Keys.D2))
            {
                camera.Zoom -= 0.1f;
                Console.WriteLine(camera.Zoom);

            }

            if(KeyboardInput.IsKeyDown(Keys.Z))
            {
                camera.Rotation -= (MathF.PI / 180.0f) * 1.0f;
            }

            if (KeyboardInput.IsKeyDown(Keys.C))
            {
                camera.Rotation += (MathF.PI / 180.0f) * 1.0f;
            }

            if(KeyboardInput.IsKeyPressed(Keys.D5))
            {
                if (screen.Width == 800)
                    screen.Resize(1024, 720);
                else
                    screen.Resize(800, 600);
            }

            if(MouseInput.IsLeftButtonPressed())
            {
                CreateCircleEntity();
                Console.WriteLine(entityManager.GetEntities().Count);
            }

            if (MouseInput.IsRightButtonPressed())
            {
                CreateRectangleEntity();
                Console.WriteLine(entityManager.GetEntities().Count);
            }

            PhysicsCmp physicsCmp = entityManager.GetComponent<PhysicsCmp>(player);
            const float PLAYER_FORCE = 100.0f;
            Vector2 force = Vector2.Zero;
            if (KeyboardInput.IsKeyDown(Keys.Up))
                force.Y -= 1.0f;
            if (KeyboardInput.IsKeyDown(Keys.Down))
                force.Y += 1.0f;
            if (KeyboardInput.IsKeyDown(Keys.Left))
                force.X -= 1.0f;
            if (KeyboardInput.IsKeyDown(Keys.Right))
                force.X += 1.0f;

            if (force != Vector2.Zero)
            {
                force.Normalize();
                force *= PLAYER_FORCE;
                physicsCmp.Force += force;
            }

            if (KeyboardInput.IsKeyDown(Keys.Space))
                camera.Position = player.Position;

            if(KeyboardInput.IsKeyDown(Keys.B))
            {
                physicsCmp.Torque += 10000.0f;
                //player.Rotation += MathF.PI / 2.0f * dt;
            }
            if (KeyboardInput.IsKeyDown(Keys.M))
            {
                physicsCmp.Torque -= 10000.0f;
                //player.Rotation -= MathF.PI / 2.0f * dt;
            }

            if (KeyboardInput.IsKeyDown(Keys.H))
            {
                player.Scale += 1.0f * dt;
            }
            if (KeyboardInput.IsKeyDown(Keys.K))
            {
                player.Scale -= 1.0f * dt;
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

            if(vel != Vector2.Zero)
            {
                vel.Normalize();
                vel *= CAM_SPEED * dt;
                camera.Position += vel;
            }

            Rectangle bounds = camera.GetBounds();
            //if (bounds.X < 0) Console.WriteLine("Left"); // camera.Position = new Vector2(0.0f, camera.Position.Y);
            //if (bounds.Y < 0) Console.WriteLine("Top"); // camera.Position = new Vector2(camera.Position.X, 0.0f);
            if (bounds.Right > screen.Width)
            {
                //Console.WriteLine("Right");
                //camera.Position =
                //    new Vector2(renderTarget2D.Width - bounds.Width, camera.Position.Y);
            }
                
            if (bounds.Bottom > screen.Width)
            {
                //Console.WriteLine("Bottom");
                //camera.Position =
                //    new Vector2(camera.Position.X, renderTarget2D.Height - bounds.Height);
            }

            if(KeyboardInput.IsKeyPressed(Keys.D3))
            {
                Console.WriteLine(camera.Position.X + " " + camera.Position.Y);
                Console.WriteLine(bounds.X + " " + bounds.Y + " " +
                    bounds.Width + " " + bounds.Height);
            }

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
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin(camera, samplerState: SamplerState.PointClamp);
            foreach (Tile t in tiles)
            {
                spriteBatch.Draw(tileSetTexture, t.Position, 
                    t.Source, Color.White);
            }
            spriteBatch.End();



            shapeBatch.Begin(camera);
            //shapeBatcher.DrawFilledRectangle(new Rectangle(32, 32, 16, 16),
            //    MathF.PI / 4.0f, new Vector2(8.0f, 8.0f), Color.Green);
            /*
            shapeBatcher.DrawRectangle(new Vector2(32, 32), new Vector2(32, 64),
                1.0f, MathF.PI / 4, new Vector2(16.0f, 32.0f), Color.Red);

            Vector2 center = new Vector2(screen.HalfWidth, screen.HalfHeight);
            Vector2 mouse = MouseInput.GetPosition(camera);

            shapeBatcher.DrawCircle(new Vector2(0.0f, 0.0f), 128.0f, 32, 4.0f, Color.Gold);
            shapeBatcher.DrawLine(center, mouse, 4.0f, Color.Fuchsia);
            */

            shapeBatch.DrawLine(new Vector2(0.0f, 0.0f),
                new Vector2(0.0f, 50.0f), 4.0f, new Color(0, 255, 0));
            shapeBatch.DrawLine(new Vector2(0.0f, 0.0f),
                new Vector2(50.0f, 0.0f), 4.0f, Color.Red);
            shapeBatch.DrawLine(new Vector2(0.0f, screen.Height * 0.5f),
               new Vector2(50.0f, screen.Height * 0.5f), 4.0f, new Color(0, 255, 255));

            //shapeBatcher.DrawFilledRectangle(new Rectangle(100, 100, 200, 300), Color.Blue);
            //shapeBatcher.DrawLine(new Vector2(100, 100), new Vector2(300, 400), Color.Red);
            shapeBatch.End();
            drawSystems.UpdateSystems();

            DebugTimer.Stop("Draw");
            DebugTimer.Draw(spriteBatch, font);

            shapeBatch.Begin(camera);
            foreach(Vector2 v in PhysicsSystem.contactPoints)
            {
                shapeBatch.DrawFilledRectangle(v - new Vector2(2.0f, 2.0f),
                    new Vector2(4.0f, 4.0f), Color.Red);
            }

            shapeBatch.End();

            

            //gameStates.DrawActiveStates(gameTime, spriteBatch);


            screen.Present(spriteBatch, SamplerState.PointClamp);


            base.Draw(gameTime);
        }

        protected override void OnActivated(object sender, EventArgs args)
        {
            DebugLog.LogInfo("Game is focused");

            base.OnActivated(sender, args);
        }

        protected override void OnDeactivated(object sender, EventArgs args)
        {
            DebugLog.LogInfo("Game lost focus");
            
            base.OnDeactivated(sender, args);
        }
    }
}