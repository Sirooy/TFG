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
using System.Runtime.ExceptionServices;

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

    public class DiceAction
    {
        public Rectangle SourceRect { get; set; }
        public Color Color { get; set; }
        public Action<Game1> Action { get; set; }
        public Action<Game1, ShapeBatch> DrawCommand { get; set; }

        public DiceAction(Rectangle sourceRect, Color color, Action<Game1> action,
            Action<Game1, ShapeBatch> drawCommand)
        {
            this.SourceRect = sourceRect;
            this.Color = color;
            this.Action = action;
            this.DrawCommand = drawCommand;
        }
    }

    public class DiceQueue
    {
        private List<int> dice;
        private List<DiceAction> actions;
        private float currentCooldown;
        public Vector2 StartPosition;
        public Keys ActionKey { get; set; }
        public bool IsEnabled { get; set; }
        public bool CanBeActivated { get; set; }
        public float TotalWidth { get; private set; }
        public float Cooldown { get; set; }

        public DiceQueue(List<DiceAction> actions, Keys actionKey, Vector2 startPosition, int numDice, float cooldown)
        {
            this.actions = actions;
            this.ActionKey = actionKey;
            this.StartPosition = startPosition;
            this.dice = new List<int>();
            this.IsEnabled = false;
            this.CanBeActivated = false;
            this.currentCooldown = 0.0f;
            this.Cooldown = cooldown;

            Random rnd = new Random();
            for (int i = 0; i < numDice; ++i)
                dice.Add(rnd.Next(actions.Count));

            DiceAction firstAction = actions[0];
            TotalWidth = firstAction.SourceRect.Width * numDice + 5.0f * numDice;
        }

        public void Update(Game1 game, float dt)
        {
            if (currentCooldown < Cooldown)
            {
                currentCooldown = Math.Min(currentCooldown + dt, Cooldown);
            }
            else
            {
                CanBeActivated = true;
            }
        }

        public void ExecuteAction(Game1 game)
        {
            if (currentCooldown < Cooldown)
                return;

            int index = dice[0];

            actions[index].Action(game);

            Random rnd = new Random();
            dice.RemoveAt(0);
            dice.Add(rnd.Next(actions.Count));

            currentCooldown = 0.0f;
            CanBeActivated = false;
        }

        public void Draw(Game1 game, SpriteBatch spriteBatch,
            Texture2D blankTexture, Texture2D diceTexture)
        {
            byte alpha = (IsEnabled) ? (byte)255 : (byte)127;

            float rectWidth = (currentCooldown / Cooldown) * TotalWidth;
            spriteBatch.Draw(blankTexture, StartPosition,null, 
                new Color(Color.Gold.R, Color.Gold.G, Color.Gold.B, alpha), 0.0f, 
                Vector2.Zero, new Vector2(rectWidth, 32.0f),
                SpriteEffects.None, 0.0f);

            for (int i = 0; i < dice.Count; ++i)
            {
                DiceAction action = actions[dice[i]];
                spriteBatch.Draw(diceTexture, StartPosition + new Vector2(i * 32.0f + i * 5.0f, 0.0f),
                    action.SourceRect,
                    new Color(action.Color.R, action.Color.G, action.Color.B, alpha));
            }
        }

        public void DrawAction(Game1 game, ShapeBatch shapeBatch)
        {
            if (IsEnabled)
            {
                int index = dice[0];
                actions[index].DrawCommand(game, shapeBatch);
            }
        }

    }

    public class HealthCmp
    {

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

        public const int WindowWidth  = 1024;
        public const int WindowHeight = 720;

        public List<DiceAction> actions = new List<DiceAction>();
        public List<DiceQueue> queues = new List<DiceQueue>();
        public DiceQueue currentQueue = null;
        private Texture2D mainTexture;
        private Texture2D blankTexture;

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

        protected DiceAction CreateDiceMoveAction(float distance, Rectangle source)
        {
            return new DiceAction(source, Color.White,
                (Game1 game) =>
                {
                    PhysicsCmp physicsCmp = entityManager.GetComponent<PhysicsCmp>(player);

                    Vector2 dir = MouseInput.GetPosition(camera) - player.Position;
                    dir.Normalize();
                    float d = physicsCmp.LinearDamping;
                    physicsCmp.LinearVelocity = dir * (distance * d);
                },
                (Game1 game, ShapeBatch shapeBatch) =>
                {
                    Vector2 dir = MouseInput.GetPosition(camera) - player.Position;
                    dir.Normalize();
                    RaycastResult raycast = updateSystems.GetSystem<PhysicsSystem>()
                        .Raycast(player.Position, dir, ColliderType.Static);

                    if (raycast.HasCollided && raycast.Distance < distance)
                    {
                        shapeBatch.DrawLine(player.Position, player.Position + dir * raycast.Distance,
                            2.0f, Color.Yellow);
                    }
                    else
                    {
                        dir *= distance;
                        shapeBatch.DrawLine(player.Position, player.Position + dir, 2.0f, Color.Yellow);
                    }
                });
        }

        protected override void LoadContent()
        {
            DebugLog.Info("Loading content");

            spriteBatch = new SpriteBatch(GraphicsDevice);
            shapeBatch = new ShapeBatch(GraphicsDevice);
            tileSetTexture = Content.Load<Texture2D>("TileSet");
            mainTexture = Content.Load<Texture2D>("MainSprite");
            blankTexture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            blankTexture.SetData(new[] { Color.White });
            font = Content.Load<SpriteFont>("DebugFont");
            // TODO: use this.Content to load your game content here

            drawSystems.RegisterSystem(new RenderSystem(entityManager, spriteBatch, shapeBatch, camera));
            drawSystems.EnableSystem<RenderSystem>();

            updateSystems.RegisterSystem(new PhysicsSystem(entityManager,
                new Vector2(0.0f, 250.0f), 1.0f / 60.0f));
            updateSystems.RegisterSystem(new ScriptSystem(entityManager));
            updateSystems.EnableSystem<ScriptSystem>();
            updateSystems.EnableSystem<PhysicsSystem>();
            PhysicsSystem physiscSystem = updateSystems.GetSystem<PhysicsSystem>();


            player = entityManager.CreateEntity();
            player.Position = new Vector2(0, 0.0f);
            SpriteCmp spriteCmp = entityManager.AddComponent(player, 
                new SpriteCmp(tileSetTexture, new Rectangle(0, 11 * 16, 16, 16)));
            spriteCmp.Origin = new Vector2(8.0f, 8.0f);
            PhysicsCmp physicsCmp = entityManager.AddComponent(player, new PhysicsCmp());
            physicsCmp.GravityMultiplier = 0.0f;
            physicsCmp.LinearDamping = 2.0f;
            physicsCmp.Mass = 1.0f;
            physicsCmp.Inertia = 0.0f;
            ColliderCmp collisionCmp = entityManager.AddComponent(player,
                new ColliderCmp(new CircleCollider(8.0f)));
            collisionCmp.AddCollisionLayer(CollisionBitmask.Player);
            collisionCmp.AddCollisionMask(CollisionBitmask.All);
            collisionCmp.Material.Restitution = 1.0f;


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

            physiscSystem.Iterations = 1;
            physiscSystem.Gravity = Vector2.Zero;
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

            for (int i = 0;i < 6; ++i)
            {
                actions.Add(CreateDiceMoveAction(32.0f * (i + 1), 
                    new Rectangle(64 + 32 * i, 0, 32, 32)));
            }
            actions.Add(new DiceAction(new Rectangle(32 * 6 + 64, 0, 32, 32), Color.White,
                (Game1 game) =>
                {
                    Vector2 dir = MouseInput.GetPosition(camera) - player.Position;
                    dir.Normalize();
                    dir *= 100.0f;

                    CreateProjectileAttack(CollisionBitmask.Enemy | CollisionBitmask.Wall,
                        dir, player.Position, new Color(0, 255, 0));
                },
                (Game1 game, ShapeBatch shapeBatch) =>
                {
                    Vector2 dir = MouseInput.GetPosition(camera) - player.Position;
                    dir.Normalize();
                    RaycastResult raycast = updateSystems.GetSystem<PhysicsSystem>()
                        .Raycast(player.Position, dir, ColliderType.Static);

                    if (raycast.HasCollided && raycast.Distance < 100.0f)
                    {
                        shapeBatch.DrawLine(player.Position, player.Position + dir * raycast.Distance,
                            2.0f, Color.LightBlue);
                    }
                    else
                    {
                        dir *= 200.0f;
                        shapeBatch.DrawLine(player.Position, player.Position + dir, 
                            2.0f, Color.LightBlue);
                    }

                }));

            int width = screen.Width;
            int height = screen.Height;

            float halfW = width * 0.5f;
            DiceQueue queue1 = new DiceQueue(actions, Keys.D1, new Vector2(0.0f, 0.0f), 4, 2.0f);
            queue1.StartPosition = new Vector2(halfW - halfW * 0.5f - queue1.TotalWidth * 0.5f, height - 48.0f);
            DiceQueue queue2 = new DiceQueue(actions, Keys.D2, new Vector2(0.0f, 0.0f), 4, 2.0f);
            queue2.StartPosition = new Vector2(halfW + halfW * 0.5f - queue1.TotalWidth * 0.5f, height - 48.0f);

            queues.Add(queue1);
            queues.Add(queue2);
        }

        public Entity CreateProjectileAttack(CollisionBitmask mask, Vector2 dir, 
            Vector2 position, Color color)
        {
            Entity ent = entityManager.CreateEntity();
            ent.Position = position;

            PhysicsCmp phy = entityManager.AddComponent(ent, new PhysicsCmp());
            phy.Inertia        = 0.0f;
            phy.LinearVelocity = dir;

            TriggerColliderCmp col = entityManager.AddComponent(ent,
                new TriggerColliderCmp(new CircleCollider(8.0f), 
                CollisionBitmask.Attack, mask));
            col.OnTriggerEnter += (Entity e1, TriggerColliderCmp c1,
                Entity e2, ColliderBody c2, ColliderType type, in Manifold m)
                =>
            {
                if(type == ColliderType.Dynamic)
                {
                    if(entityManager.TryGetComponent(e2, out PhysicsCmp phy2))
                    {
                        phy2.Force += -m.Normal * 5000.0f;
                    }
                }

                entityManager.RemoveEntity(e1);
            };

            SpriteCmp sprite = entityManager.AddComponent(ent,
                new SpriteCmp(tileSetTexture, new Rectangle(0, 7 * 16, 16, 16)));
            sprite.Color = color;
            sprite.Origin = new Vector2(8.0f, 8.0f);

            return ent;
        }

        protected void CreateEnemy(Vector2 pos)
        {
            Entity enemy = entityManager.CreateEntity();
            enemy.Position = pos;
            SpriteCmp spriteCmp = entityManager.AddComponent(enemy,
                new SpriteCmp(tileSetTexture, new Rectangle(3 * 16, 11 * 16, 16, 16)));
            spriteCmp.Origin = new Vector2(8.0f, 8.0f);
            PhysicsCmp physicsCmp = entityManager.AddComponent(enemy, new PhysicsCmp());
            physicsCmp.GravityMultiplier = 0.0f;
            physicsCmp.LinearDamping = 2.0f;
            physicsCmp.Mass = 1.0f;
            physicsCmp.Inertia = 0.0f;
            ColliderCmp collisionCmp = entityManager.AddComponent(enemy,
                new ColliderCmp(new CircleCollider(8.0f)));
            collisionCmp.AddCollisionLayer(CollisionBitmask.Enemy);
            collisionCmp.AddCollisionMask(CollisionBitmask.All);
            collisionCmp.Material.Restitution = 1.0f;
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

            if(MouseInput.ScrollHasChanged())
            {
                int sign = -MathF.Sign(MouseInput.ScrollValueDiff);
                camera.Zoom += 0.1f * sign;
            }

            if(KeyboardInput.IsKeyPressed(Keys.D5))
            {
                if (screen.Width == 800)
                    screen.Resize(1024, 720);
                else
                    screen.Resize(800, 600);
            }

            if (KeyboardInput.IsKeyDown(Keys.Space))
                camera.Position = player.Position;
            

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

            foreach (DiceQueue queue in queues)
            {
                queue.Update(this, dt);
            }

            if (currentQueue == null)
            {
                for (int i = 0; i < queues.Count; ++i)
                {
                    DiceQueue diceQueue = queues[i];
                    if (KeyboardInput.IsKeyPressed(diceQueue.ActionKey) &&
                        diceQueue.CanBeActivated)
                    {
                        currentQueue = diceQueue;
                        currentQueue.IsEnabled = true;
                        break;
                    }
                }
            }
            else
            {
                if (MouseInput.IsLeftButtonPressed())
                {
                    currentQueue.ExecuteAction(this);
                    currentQueue.IsEnabled = false;
                    currentQueue = null;
                }
            }

            if(KeyboardInput.IsKeyPressed(Keys.D3))
            {
                CreateEnemy(MouseInput.GetPosition(camera));
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

            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            foreach (DiceQueue queue in queues)
            {
                queue.Draw(this, spriteBatch, blankTexture, mainTexture);
            }
            spriteBatch.End();

            shapeBatch.Begin(camera);

            shapeBatch.DrawLine(new Vector2(0.0f, 0.0f),
                new Vector2(0.0f, 50.0f), 4.0f, new Color(0, 255, 0));
            shapeBatch.DrawLine(new Vector2(0.0f, 0.0f),
                new Vector2(50.0f, 0.0f), 4.0f, Color.Red);



            foreach (DiceQueue queue in queues)
            {
                queue.DrawAction(this, shapeBatch);
            }

            shapeBatch.End();
            drawSystems.UpdateSystems();
            shapeBatch.Begin(camera);

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