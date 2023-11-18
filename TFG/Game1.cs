using System;
using System.Collections.Generic;
using Engine.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Engine.Graphics;
using Engine.Ecs;
using Cmps;
using Systems;

public class MultiSparseArray<TDataType>
{
    public struct Slot //Cambiar a privado
    {
        public int Index;
        public int Count;
    }

    public const int NullKey = -1;

    public Slot[] slots; //Cambiar a privados
    public List<TDataType> data;
    private List<int> remove;

    public int Count { get { return data.Count; } }

    public MultiSparseArray(int initialCapacity = 1)
    {
        slots = new Slot[initialCapacity];
        data = new List<TDataType>(initialCapacity);
        remove = new List<int>(initialCapacity);

        Array.Fill(slots, new Slot { Index = -1, Count = 0 });
    }

    public TDataType Add(int key, TDataType item)
    {
        if (key >= slots.Length)
            Resize(slots.Length + key + 1);

        Slot slot = slots[key];

        //Is new data
        if (slot.Index == NullKey)
        {
            slot.Index = data.Count;
            slot.Count = 1;

            slots[key] = slot;
            data.Add(item);
            remove.Add(key);
        }
        else
        {
            //Increase the index by one of every slot that is next to the one that is
            //being modified
            int insertIndex = slot.Index + slot.Count;
            int removeIndex = insertIndex;
            while (removeIndex < remove.Count)
            {
                int slotIndex = remove[removeIndex];
                Slot nextSlot = slots[slotIndex];
                nextSlot.Index++;
                slots[slotIndex] = nextSlot;

                removeIndex += nextSlot.Count;
            }


            data.Insert(insertIndex, item);
            remove.Insert(insertIndex, key);

            slot.Count++;
            slots[key] = slot;

        }

        return item;
    }

    private void Resize(int newCapacity)
    {
        int oldCapacity = slots.Length;

        data.Capacity = newCapacity;
        remove.Capacity = newCapacity;

        Array.Resize(ref slots, newCapacity);
        Array.Fill(slots, new Slot { Index = -1, Count = 0 }, oldCapacity, newCapacity - oldCapacity);
    }
}

namespace TFG
{
    [Flags]
    public enum EntityTags
    {
        None   = 0x00,
        Player = 0x01,
        Enemy  = 0x02
    }

    public class Entity : EntityBase
    {
        public Vector2 Position;
        public float Rotation;
        public float Scale;
        public EntityTags Tags;

        public Entity() : base()
        {
            Position = Vector2.Zero;
            Rotation = 0.0f;
            Scale    = 1.0f;
        }
    }

    public struct EntityTransformChild
    {
        public Vector2 LocalPosition;
        public float   LocalRotation;
        public float   LocalScale;

        public EntityTransformChild()
        {
            LocalPosition = Vector2.Zero;
            LocalRotation = 0.0f;
            LocalScale = 1.0f;
        }

        public Vector2 GetWorldPosition(Entity entity)
        {
            float cos = MathF.Cos(entity.Rotation);
            float sin = MathF.Sin(entity.Rotation);
            float x   = entity.Scale * LocalPosition.X;
            float y   = entity.Scale * LocalPosition.Y;

            return new Vector2(
                (x * cos - y * sin) + entity.Position.X,
                (x * sin + y * cos) + entity.Position.Y);
        }

        public float GetWorldRotation(Entity entity)
        {
            return LocalRotation + entity.Rotation;
        }

        public float GetWorldScale(Entity entity)
        {
            return LocalScale * entity.Scale;
        }
    }

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
                Debug.LogWarning("Resize {0}:{1}", Window.ClientBounds.Width, 
                    Window.ClientBounds.Height);
            };

        }

        protected override void Initialize()
        {
            Debug.LogInfo("Initializing");

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
                    tiles.Add(tile);
                }
            }

            base.Initialize();
        }

        protected override void LoadContent()
        {
            Debug.LogInfo("Loading content");

            spriteBatch = new SpriteBatch(GraphicsDevice);
            shapeBatch = new ShapeBatch(GraphicsDevice);
            tileSetTexture = Content.Load<Texture2D>("TileSet");
            // TODO: use this.Content to load your game content here

            player = entityManager.CreateEntity();
            SpriteCmp spriteCmp1 = entityManager.AddComponent(player, 
                new SpriteCmp(tileSetTexture, new Rectangle(16, 16, 16, 16)));
            spriteCmp1.Transform.LocalPosition = new Vector2(0.0f,  32.0f);
            spriteCmp1.Origin = new Vector2(8.0f, 8.0f);
            SpriteCmp spriteCmp2 = entityManager.AddComponent(player,
                new SpriteCmp(tileSetTexture, new Rectangle(16, 32, 16, 16)));
            spriteCmp2.Transform.LocalPosition = new Vector2(0.0f, -32.0f);
            spriteCmp2.Origin = new Vector2(8.0f, 8.0f);
            PhysicsCmp physicsCmp = entityManager.AddComponent(player, new PhysicsCmp());
            physicsCmp.MaxLinearVelocity = new Vector2(250.0f, 250.0f);


            drawSystems.RegisterSystem(new RenderSystem(entityManager, spriteBatch, shapeBatch, camera));
            drawSystems.EnableSystem<RenderSystem>();

            updateSystems.RegisterSystem(new PhysicsSystem(entityManager, 1.0f / 60.0f));
            updateSystems.EnableSystem<PhysicsSystem>();
        }

        protected override void Update(GameTime gameTime)
        {
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


            PhysicsCmp physicsCmp = entityManager.GetComponent<PhysicsCmp>(player);
            const float PLAYER_FORCE = 2000.0f;
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
                player.Rotation += MathF.PI / 2.0f * dt;
            }
            if (KeyboardInput.IsKeyDown(Keys.M))
            {
                player.Rotation -= MathF.PI / 2.0f * dt;
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
            if (bounds.X < 0) Console.WriteLine("Left"); // camera.Position = new Vector2(0.0f, camera.Position.Y);
            if (bounds.Y < 0) Console.WriteLine("Top"); // camera.Position = new Vector2(camera.Position.X, 0.0f);
            if (bounds.Right > screen.Width)
            {
                Console.WriteLine("Right");
                //camera.Position =
                //    new Vector2(renderTarget2D.Width - bounds.Width, camera.Position.Y);
            }
                
            if (bounds.Bottom > screen.Width)
            {
                Console.WriteLine("Bottom");
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
            screen.Attach();
            GraphicsDevice.Clear(Color.CornflowerBlue);


            spriteBatch.Begin(camera, samplerState: SamplerState.PointClamp);
            foreach (Tile t in tiles)
            {
                spriteBatch.Draw(tileSetTexture, t.Position, 
                    t.Source, Color.White);
            }
            spriteBatch.Draw(tileSetTexture, new Vector2(640.0f, 0.0f),
                new Rectangle(0, 0, 16, 16), Color.White);

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



            //gameStates.DrawActiveStates(gameTime, spriteBatch);

            screen.Present(spriteBatch, SamplerState.PointClamp);


            base.Draw(gameTime);
        }

        protected override void OnActivated(object sender, EventArgs args)
        {
            Debug.LogInfo("Game is focused");

            base.OnActivated(sender, args);
        }

        protected override void OnDeactivated(object sender, EventArgs args)
        {
            Debug.LogInfo("Game lost focus");
            
            base.OnDeactivated(sender, args);
        }
    }
}