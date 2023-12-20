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
        private Entity mouseEntity;
        private Vector2 point;
        private Color rayColor;

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
            parent = new Transform();
            child  = new Transform();
            entityManager = new EntityManager<Entity>();
            drawSystems = new SystemManager();
            updateSystems = new SystemManager();

            entityManager.RegisterComponent<SpriteCmp>();
            entityManager.RegisterComponent<PhysicsCmp>();
            entityManager.RegisterComponent<ColliderCmp>();
            entityManager.RegisterComponent<TriggerColliderCmp>();

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
            DebugLog.Info("Loading content");

            spriteBatch = new SpriteBatch(GraphicsDevice);
            shapeBatch = new ShapeBatch(GraphicsDevice);
            tileSetTexture = Content.Load<Texture2D>("TileSet");
            font = Content.Load<SpriteFont>("DebugFont");
            // TODO: use this.Content to load your game content here

            drawSystems.RegisterSystem(new RenderSystem(entityManager, spriteBatch, shapeBatch, camera));
            drawSystems.EnableSystem<RenderSystem>();

            updateSystems.RegisterSystem(new PhysicsSystem(entityManager,
                new Vector2(0.0f, 250.0f), 1.0f / 60.0f));
            updateSystems.EnableSystem<PhysicsSystem>();
            updateSystems.GetSystem<PhysicsSystem>().Iterations = 5;

            float playerWidth = 256.0f;
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
            physicsCmp.GravityMultiplier = 1.0f;
            physicsCmp.Mass = 0.0f;


            //physicsCmp.Inertia = (1.0f / 12.0f) * 1.0f * 
            //    (playerWidth * playerWidth + playerHeight * playerHeight);
            physicsCmp.Inertia = 0.0f;
            physicsCmp.AngularVelocity = 0.5f;

            ColliderCmp collisionCmp2 = entityManager.AddComponent(player,
                new ColliderCmp(new RectangleCollider(playerWidth, playerHeight)));
            collisionCmp2.AddCollisionLayer((CollisionBitmask)0x01);
            collisionCmp2.AddCollisionMask((CollisionBitmask)0x01);
            //physicsCmp.Inertia = 0.0f;

            //CollisionCmp collisionCmp2 = entityManager.AddComponent(player,
            //    new CollisionCmp(new CircleCollider(8.0f), Material.Friction));
            //physicsCmp.Inertia = (1.0f / 4.0f) * 1.0f * 8.0f * 8.0f;
            //collisionCmp2.Transform.LocalPosition = new Vector2(64.0f, 0.0f);
            //CollisionCmp collisionCmp3 = entityManager.AddComponent(player,
            //    new CollisionCmp(new RectangleCollider(playerWidth, playerHeight)));
            //collisionCmp3.Transform.LocalPosition = new Vector2(-64.0f, 0.0f);

            Entity triggerEntity = entityManager.CreateEntity();
            triggerEntity.Position = new Vector2(-500.0f, 0.0f);
            triggerEntity.Rotation = MathF.PI * 45.0f / 180.0f;
            TriggerColliderCmp triggerCol = entityManager.AddComponent(triggerEntity,
                new TriggerColliderCmp(new RectangleCollider(128.0f, 256.0f)));
            triggerCol.AddCollisionLayer((CollisionBitmask)0x01);
            triggerCol.AddCollisionMask((CollisionBitmask)0x01);
            triggerCol.OnTriggerEnter += (Entity e1, TriggerColliderCmp c1,
                Entity e2, ColliderBody c2, ColliderType type, in Manifold manifold) =>
            {
                string entity2 = "Static";
                if (e2 != null)
                    entity2 = e2.Id.ToString();
                Console.WriteLine(manifold.Normal);
                DebugLog.Success("Trigger ENTER: {0}, type: {1}, Num collisions: {2}", 
                    entity2, type, c1.CurrentCollisions.Count);
            };

            triggerCol.OnTriggerStay += (Entity e1, TriggerColliderCmp c1,
                Entity e2, ColliderBody c2, ColliderType type, in Manifold manifold) =>
            {
                string entity2 = "Static";
                if (e2 != null)
                    entity2 = e2.Id.ToString();
                Console.WriteLine(manifold.Normal * manifold.Depth);
                e2.Position -= manifold.Normal * manifold.Depth;
                //DebugLog.Warning("Trigger STAY: {0}, type: {1}, Num collisions: {2}",
                //    entity2, type, c1.CurrentCollisions.Count);
            };

            triggerCol.OnTriggerExit += (Entity e1, TriggerColliderCmp c1,
                Entity e2, ColliderBody c2, ColliderType type) =>
            {
                string entity2 = "Static";
                if (e2 != null)
                    entity2 = e2.Id.ToString();
                DebugLog.Error("Trigger EXIT: {0}, type: {1}, Num collisions: {2}",
                    entity2, type, c1.CurrentCollisions.Count);
            };

            /*
            Entity block   = entityManager.CreateEntity();
            block.Position = new Vector2(0.0f, 16.0f * 20.0f);
            PhysicsCmp blockPhysics = entityManager.AddComponent(block, new PhysicsCmp());
            blockPhysics.GravityMultiplier = 0.0f;
            blockPhysics.Mass = 0.0f;
            blockPhysics.Inertia = 0.0f;
            CollisionCmp blockCollision = entityManager.AddComponent(block,
                new CollisionCmp(new RectangleCollider(1024.0f, 128.0f),
                //Material.Friction));
            blockCollision.Material.Restitution = 0.8f;
            blockCollision.AddCollisionLayer((CollisionBitmask)0x01);
            blockCollision.AddCollisionMask((CollisionBitmask)0x01);
            */
            StaticCollider block = updateSystems.GetSystem<PhysicsSystem>().
                AddStaticCollider(new RectangleCollider(1024.0f, 128.0f), 
                Material.One, (CollisionBitmask)0x01, (CollisionBitmask)0x01);
            block.Position = new Vector2(0.0f, 16.0f * 20.0f);

            Random rnd = new Random();
            const int COLUMNS = 0;
            const int ROWS    = 0;
            for(int i = 0;i < COLUMNS; ++i)
            {
                for(int j = 0;j < ROWS; ++j)
                {
                    float x = (i - ((COLUMNS / 2) - 1)) * 32.0f;
                    float y = (j - ((ROWS / 2) - 1)) * 32.0f;

                    Entity e = entityManager.CreateEntity();
                    e.Position = new Vector2(x, y);
                    PhysicsCmp phy = entityManager.AddComponent(e, new PhysicsCmp());
                    phy.Inertia = (1.0f / 12.0f) * phy.Mass * 
                        (16.0f * 16.0f + 16.0f * 16.0f);
                    //phy.Inertia = 2000.0f;
                    //phy.Inertia = 0.0f;

                    SpriteCmp spr = entityManager.AddComponent(e,
                        new SpriteCmp(tileSetTexture, new Rectangle(
                            rnd.Next(3) * 16,
                            rnd.Next(15) * 16, 16, 16)));
                    spr.Origin = new Vector2(8.0f, 8.0f);
                    ColliderCmp col = entityManager.AddComponent(e,
                        new ColliderCmp(new RectangleCollider(16.0f, 16.0f),
                        Material.One)); //new CircleCollider(8.0f)
                    //col.Material.Restitution = 0.8f;
                    col.AddCollisionLayer((CollisionBitmask)0x01);
                    col.AddCollisionMask((CollisionBitmask)0x01);
                }
            }
        }

        private void CreateMouseEntity()
        {
            if(mouseEntity == null)
            {
                mouseEntity = entityManager.CreateEntity();
                mouseEntity.Rotation = MathF.PI * 45.0f / 180.0f;
                TriggerColliderCmp mouseCol = entityManager.AddComponent(mouseEntity,
                    new TriggerColliderCmp(new CircleCollider(16.0f)));
                mouseCol.AddCollisionLayer((CollisionBitmask)0x01);
                mouseCol.AddCollisionMask((CollisionBitmask)0x01);
                /*
                mouseCol.OnTriggerStay += (Entity e1, TriggerCollisionCmp c1,
                    Entity e2, ColliderBody c2, CollisionType type, in Manifold manifold) =>
                {
                    string entity2 = "Static";
                    if (e2 != null)
                        entity2 = e2.Id.ToString();
                    Console.WriteLine(manifold.Normal);
                    Console.WriteLine("Mouse enter: {0}, type: {1}", entity2, type);
                };
                */
            }
        }

        private void DestroyMouseEntity()
        {
            if(mouseEntity != null)
            {
                entityManager.RemoveEntity(mouseEntity);
                mouseEntity = null;
            }
        }

        private void CreateCircleEntity()
        {
            const float radius = 8.0f;
            Entity e = entityManager.CreateEntity();
            e.Position = MouseInput.GetPosition(camera);

            PhysicsCmp phy = entityManager.AddComponent(e, new PhysicsCmp());
            phy.Inertia = (1.0f / 4.0f) * phy.Mass * radius * radius;
            phy.LinearDamping = 0.0f;

            ColliderCmp col = entityManager.AddComponent(e,
                new ColliderCmp(new CircleCollider(radius), Material.Wood));
            col.Material.Restitution = 0.8f;
            col.AddCollisionLayer((CollisionBitmask)0x01);
            col.AddCollisionMask((CollisionBitmask)0x01);
            //col.OnCollision += (Entity e1, CollisionCmp c1, Entity e2, CollisionCmp c2,
            //    in Manifold manifold) =>
            //{
            //    Console.WriteLine(e1.Id + " " + e2.Id);
            //};
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
            e.Rotation = MathF.PI * 45.0f / 180.0f;

            PhysicsCmp phy = entityManager.AddComponent(e, new PhysicsCmp());
            phy.Inertia = (1.0f / 12.0f) * phy.Mass * (width * width + height * height);
            phy.LinearDamping = 0.99f;
            phy.AngularDamping = 0.99f;

            ColliderCmp col = entityManager.AddComponent(e,
                new ColliderCmp(new RectangleCollider(width, height),
                Material.One));
            col.AddCollisionLayer((CollisionBitmask)0x01);
            col.AddCollisionMask((CollisionBitmask)0x01);
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
                CreateMouseEntity();
            }

            if (MouseInput.IsRightButtonPressed())
            {
                DestroyMouseEntity();
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
                physicsCmp.Torque += 1000.0f;
                //player.Rotation += MathF.PI / 2.0f * dt;
            }
            if (KeyboardInput.IsKeyDown(Keys.M))
            {
                physicsCmp.Torque -= 1000.0f;
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
                CreateCircleEntity();
                Console.WriteLine(entityManager.GetEntities().Count);
            }

            if (KeyboardInput.IsKeyPressed(Keys.D4))
            {
                CreateRectangleEntity();
                Console.WriteLine(entityManager.GetEntities().Count);
            }

            if (mouseEntity != null)
            {
                mouseEntity.Position = MouseInput.GetPosition(camera);
            }

            Vector2 rayStart = new Vector2(-200.0f, 0.0f);
            Vector2 rayDir   = Vector2.Normalize(MouseInput.GetPosition(camera) - rayStart);
            RaycastResult raycast = updateSystems.GetSystem<PhysicsSystem>()
                .Raycast(rayStart, rayDir, ColliderType.All, CollisionBitmask.All);

            if(raycast.HasCollided)
            {
                point = rayStart + rayDir * raycast.Distance;

                switch(raycast.ColliderType)
                {
                    case ColliderType.Static:  rayColor = Color.Red; break;
                    case ColliderType.Dynamic: rayColor = Color.Cyan; break;
                    case ColliderType.Trigger: rayColor = Color.Yellow; break;

                }
            }
            else
            {
                point = Vector2.Zero;
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
            GraphicsDevice.Clear(Color.Black);

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
               new Vector2(50.0f, screen.Height * 0.5f), 4.0f, new Color(255, 255, 255));
            //if(mouseEntity != null)
            //shapeBatch.DrawRectangle(MouseInput.GetPosition(camera), new Vector2(16.0f, 16.0f),
            //    2.0f, mouseEntity.Rotation, new Vector2(8.0f), Color.Gray);
            shapeBatch.DrawCircle(MouseInput.GetPosition(camera), 16.0f, 16, 2.0f, Color.Gray);

            shapeBatch.End();

            /* RayVsCircle Test
            shapeBatch.Begin();
            Vector2 circleCenter = new Vector2(400.0f, 300.0f);
            float circleRadius = 32.0f;
            Vector2 rayStart = new Vector2(100.0f, 300.0f);
            Vector2 rayDir = Vector2.Normalize(MouseInput.GetPosition(screen) - rayStart);
            shapeBatch.DrawCircle(circleCenter, circleRadius, 16, 2, Color.Red);
            shapeBatch.DrawLine(rayStart, MouseInput.GetPosition(screen), new Color(0, 255, 0));

            if (CollisionTester.RayVsCircle(rayStart, rayDir, circleCenter, circleRadius,
                out float dist))
            {
                Console.WriteLine("ASD");
                shapeBatch.DrawFilledRectangle(rayStart + rayDir * dist -
                    new Vector2(2.0f, 2.0f), new Vector2(4.0f, 4.0f), Color.Yellow);
            }
            shapeBatch.End();
            */

            drawSystems.UpdateSystems();

            shapeBatch.Begin(camera);
            Vector2 rayStart = new Vector2(-200.0f, 0.0f);
            Vector2 rayDir = Vector2.Normalize(MouseInput.GetPosition(camera) - rayStart);

            shapeBatch.DrawLine(rayStart, MouseInput.GetPosition(camera), 
                new Color(0, 255, 0));
            if(point != Vector2.Zero)
            {
                shapeBatch.DrawFilledRectangle(point - new Vector2(2.0f),
                    new Vector2(4.0f), rayColor);
            }

            DebugTimer.Stop("Draw");
            DebugTimer.Draw(spriteBatch, font);
            shapeBatch.End();

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