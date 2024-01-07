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
using System.Threading;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using TFG;
using System.Collections;
using System.Linq;

public class DiceFace
{
    public Rectangle SourceRect;
    public Color Color;
    public Action Action;
    public Action<Entity> EnemyAction;
    public Action<ShapeBatch> DrawCommand;
    public float ActionDelay;
    public float ActionCooldown;

    public DiceFace(Rectangle sourceRect, Color color, Action action,
        Action<Entity> enemyAction, Action<ShapeBatch> drawCommand, 
        float actionDelay, float actionCooldown)
    {
        SourceRect  = sourceRect;
        Color       = color;
        Action      = action;
        EnemyAction = enemyAction;
        DrawCommand = drawCommand;
        ActionDelay = actionDelay;
        ActionCooldown = actionCooldown;
    }
}

public class Dice
{
    public List<DiceFace> Faces;

    public Dice(List<DiceFace> faces)
    {
        this.Faces = faces;
    }
}

public class DiceQueue
{
    public Dice Dice;
    public List<DiceFace> Queue;

    private List<DiceFace> diceFaceBag;
    private Random random;

    public DiceFace ActiveFace { get { return Queue.First(); } }
    public float TotalWidth { get; private set; }

    public DiceQueue(Dice dice, int size)
    {
        this.Dice        = dice;
        this.Queue       = new List<DiceFace>();
        this.diceFaceBag = new List<DiceFace>();
        this.random      = new Random();

        this.TotalWidth = dice.Faces[0].SourceRect.Width * size + 5.0f * size;
        for (int i = 0; i < size; ++i)
            AddFaceToQueue();
    }

    public void Dequeue()
    {
        Queue.RemoveAt(0);
        AddFaceToQueue();
    }

    public void AddFaceToQueue()
    {
        if (diceFaceBag.Count == 0)
            FillDiceFaceBag();

        int index = random.Next(diceFaceBag.Count);
        Queue.Add(diceFaceBag[index]);
        diceFaceBag.RemoveAt(index);
    }

    private void FillDiceFaceBag()
    {
        foreach (DiceFace face in Dice.Faces)
        {
            diceFaceBag.Add(face);
        }
    }
}

public enum QueueState
{
    Default,
    PreExecutingAction,
    PostExecutingAction
}

public class PlayerDiceQueue
{
    public Dice Dice;
    public Keys ActionKey;
    public Keys SwapKey;
    public Keys DestroyKey;
    public QueueState State;
    public List<DiceFace> Queue;
    public float FaceActionDelay;
    public float FaceActionCooldown;
    public float CurrentActionDelay;
    public float CurrentActionCooldown;
    public DiceFace ActiveFace { get { return Queue.First(); } }
    public DiceFace SwapFace;
    private Random random;
    private List<DiceFace> diceFaceBag;

    public int Size { get; private set; }
    public float TotalWidth { get; private set; }
    public Vector2 DrawStartPosition;
    public Texture2D DiceTexture;

    public PlayerDiceQueue(Dice dice, Keys actionKey, Keys swapKey, Keys destroyKey, 
        int size, Texture2D diceTexture)
    {
        this.Dice       = dice;
        this.ActionKey  = actionKey;
        this.SwapKey    = swapKey;
        this.DestroyKey = destroyKey;
        this.Queue      = new List<DiceFace>();
        this.SwapFace   = null;

        this.random = new Random();
        this.diceFaceBag = new List<DiceFace>();

        this.Size = size;
        this.TotalWidth = dice.Faces[0].SourceRect.Width * size + 5.0f * size;
        for (int i = 0;i < size; ++i)
        {
            AddFaceToQueue();
        }
        this.DrawStartPosition = Vector2.Zero;
        this.DiceTexture       = diceTexture;
        this.State             = QueueState.Default;
        this.FaceActionDelay = 0.0f;
        this.FaceActionCooldown = 0.0f;
        this.CurrentActionDelay = 0.0f;
        this.CurrentActionCooldown = 0.0f;
    }

    public bool Update()
    {
        if(KeyboardInput.IsKeyPressed(ActionKey))
        {
            return true;
        }
        else if(KeyboardInput.IsKeyPressed(SwapKey))
        {
            if(SwapFace == null)
            {
                SwapFace = Queue[0];
                Queue.RemoveAt(0);
                AddFaceToQueue();
                return true;
            }
            else
            {
                DiceFace active = Queue[0];
                Queue[0] = SwapFace;
                SwapFace = active;
                return true;
            }
        }
        else if(KeyboardInput.IsKeyPressed(DestroyKey))
        {
            Queue.RemoveAt(0);
            AddFaceToQueue();
        }

        return false;
    }

    public void Dequeue()
    {
        Queue.RemoveAt(0);
        AddFaceToQueue();
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D blankTexture)
    {
        const float MARGIN = 5.0f;

        spriteBatch.Draw(blankTexture, DrawStartPosition - new Vector2(MARGIN, 0.0f), 
            null, Color.Gold, 0.0f, Vector2.Zero, new Vector2(TotalWidth + MARGIN, 32.0f),
            SpriteEffects.None, 0.0f);

        for (int i = 0; i < Queue.Count; ++i)
        {
            DiceFace face = Queue[i];
            spriteBatch.Draw(DiceTexture, DrawStartPosition + 
                new Vector2(i * 32.0f + i * MARGIN, 0.0f), 
                face.SourceRect, face.Color);
        }

        if(SwapFace != null)
        {
            spriteBatch.Draw(blankTexture, DrawStartPosition +
                new Vector2(TotalWidth + MARGIN * 2.0f, 0.0f),
            null, Color.Gold, 0.0f, Vector2.Zero, new Vector2(32.0f + MARGIN * 2.0f, 32.0f),
            SpriteEffects.None, 0.0f);

            spriteBatch.Draw(DiceTexture, DrawStartPosition +
                new Vector2(TotalWidth + MARGIN * 3.0f, 0.0f),
                SwapFace.SourceRect, SwapFace.Color);
        }
    }
    
    private void AddFaceToQueue()
    {
        if (diceFaceBag.Count == 0)
            FillDiceFaceBag();

        int index = random.Next(diceFaceBag.Count);
        Queue.Add(diceFaceBag[index]);
        diceFaceBag.RemoveAt(index);
    }

    private void FillDiceFaceBag()
    {
        foreach(DiceFace face in Dice.Faces)
        {
            diceFaceBag.Add(face);
        }
    }
}

public class HealthCmp
{
    public float MaxHealth;
    public float CurrentHealth;

    public HealthCmp(float maxHealth)
    {
        MaxHealth     = maxHealth;
        CurrentHealth = maxHealth;
    }
}

public class EnemyCmp
{
    public const int MaxTurnAdvantage = 5;
    public const float TurnAdvantageTimeMult = 0.5f;

    public DiceQueue Queue;
    public int TurnAdvantage;
    public float TurnTimer;
    public float CurrentTurnTimer;
    public QueueState State;
    public float FaceActionDelay;
    public float FaceActionCooldown;
    public float CurrentActionDelay;
    public float CurrentActionCooldown;


    public EnemyCmp(Dice dice, int size, float turnTimer)
    {
        Queue = new DiceQueue(dice, size);
        TurnAdvantage = 0;
        TurnTimer = turnTimer;
        CurrentTurnTimer = turnTimer;
        State = QueueState.Default;
        FaceActionDelay = 0.0f;
        FaceActionCooldown = 0.0f;
        CurrentActionDelay = 0.0f;
        CurrentActionCooldown = 0.0f;
    }
}

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

    /*
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
        
    }*/

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

        //public List<DiceAction> actions = new List<DiceAction>();
        //public List<DiceQueue> queues = new List<DiceQueue>();
        private List<PlayerDiceQueue> diceQueues = new List<PlayerDiceQueue>();
        public PlayerDiceQueue activeQueue = null;
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
            entityManager.RegisterComponent<HealthCmp>();
            entityManager.RegisterComponent<EnemyCmp>();

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
            entityManager.AddComponent(player, new HealthCmp(100.0f));


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

            Dice playerDice1 = new Dice(new List<DiceFace>()
            {
                CreateMeleeAttackDiceFace(0.0f, 0.1f),
                CreateMoveDiceFace(128.0f + 32.0f * 1.0f, new Rectangle(64 + 32 * 1, 0, 32, 32)),
                CreateMoveDiceFace(128.0f + 32.0f * 2.0f, new Rectangle(64 + 32 * 2, 0, 32, 32)),
                CreateProjectileAttackDiceFace(0.0f, 0.25f)
            });

            Dice playerDice2 = new Dice(new List<DiceFace>()
            {
                CreateMoveDiceFace(128.0f + 32.0f * 0.0f, new Rectangle(64 + 32 * 0, 0, 32, 32)),
                CreateMoveDiceFace(128.0f + 32.0f * 1.0f, new Rectangle(64 + 32 * 1, 0, 32, 32)),
            });

            int width = screen.Width;
            int height = screen.Height;
            float halfW = width * 0.5f;

            PlayerDiceQueue queue1 = new PlayerDiceQueue(playerDice1, Keys.Q, Keys.E, Keys.W,
                playerDice1.Faces.Count, mainTexture);
            queue1.DrawStartPosition = new Vector2(halfW - halfW * 0.5f - queue1.TotalWidth * 0.5f, height - 48.0f);
            PlayerDiceQueue queue2 = new PlayerDiceQueue(playerDice2, Keys.A, Keys.D, Keys.S,
                playerDice2.Faces.Count, mainTexture);
            queue2.DrawStartPosition = new Vector2(halfW + halfW * 0.5f - queue1.TotalWidth * 0.5f, height - 48.0f);

            diceQueues.Add(queue1);
            diceQueues.Add(queue2);
        }

        protected DiceFace CreateProjectileAttackDiceFace(float delay, float cooldown)
        {
            return new DiceFace(new Rectangle(32 * 6 + 64, 0, 32, 32), Color.White,
             () =>
             {
                 Vector2 dir = MouseInput.GetPosition(camera) - player.Position;
                 dir.Normalize();
                 dir *= 250.0f;

                 CreateProjectileAttack(CollisionBitmask.PlayerAttack, 
                     CollisionBitmask.Enemy | CollisionBitmask.Wall | 
                     CollisionBitmask.EnemyAttack,
                     dir, player.Position, new Color(0, 255, 0));
             },
             (Entity enemy) =>
             {
                 Vector2 dir = player.Position - enemy.Position;
                 dir.Normalize();
                 dir *= 250.0f;

                 CreateProjectileAttack(CollisionBitmask.EnemyAttack, 
                     CollisionBitmask.Player | CollisionBitmask.Wall | 
                     CollisionBitmask.PlayerAttack,
                     dir, enemy.Position, new Color(255, 0, 0));
             },
             (ShapeBatch shapeBatch) =>
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

             }, delay, cooldown);
        }

        protected DiceFace CreateMeleeAttackDiceFace(float delay, float cooldown)
        {
            return new DiceFace(new Rectangle(32 * 7 + 64, 0, 32, 32), Color.White,
            () =>
            {
                CreateMeleeAttack(CollisionBitmask.PlayerAttack,
                    CollisionBitmask.Enemy | CollisionBitmask.EnemyAttack,
                    player.Position, 48.0f, 10000.0f, Color.Green);
            },
            (Entity enemy) =>
            {
                CreateMeleeAttack(CollisionBitmask.EnemyAttack,
                    CollisionBitmask.Player | CollisionBitmask.PlayerAttack,
                    enemy.Position, 48.0f, 10000.0f, Color.Red);
            },
            (ShapeBatch shapeBatch) =>
            {
                shapeBatch.DrawCircle(player.Position, 48.0f, 16, 2.0f, Color.Yellow);
            }, delay, cooldown);
        }

        protected Dice CreateEnemyDice()
        {
            return new Dice(new List<DiceFace>()
            {
                CreateMeleeAttackDiceFace(0.0f,0.1f),
                CreateMoveDiceFace(64.0f + 32.0f * 1.0f, new Rectangle(64 + 32 * 1, 0, 32, 32)),
                CreateMoveDiceFace(64.0f + 32.0f * 2.0f, new Rectangle(64 + 32 * 2, 0, 32, 32)),
                CreateProjectileAttackDiceFace(0.0f, 0.5f)
            });
        }

        protected DiceFace CreateMoveDiceFace(float maxDistance, Rectangle source)
        {
            return new DiceFace(source, Color.White,
                () =>
                {
                    PhysicsCmp physicsCmp = entityManager.GetComponent<PhysicsCmp>(player);

                    Vector2 dir = MouseInput.GetPosition(camera) - player.Position;
                    float distance = MathF.Min(dir.Length(), maxDistance);
                    dir.Normalize();
                    float d = physicsCmp.LinearDamping;
                    physicsCmp.LinearVelocity = dir * (distance * d);
                },
                (Entity enemy) =>
                {
                    PhysicsCmp physicsCmp = entityManager.GetComponent<PhysicsCmp>(enemy);

                    Vector2 dir = player.Position - enemy.Position;
                    float distance = MathF.Max(MathF.Min(dir.Length(), maxDistance) - 16.0f,
                        0.0f);
                    dir.Normalize();
                    float d = physicsCmp.LinearDamping;
                    physicsCmp.LinearVelocity = dir * (distance * d);
                },
                (ShapeBatch shapeBatch) =>
                {
                    Vector2 dir = MouseInput.GetPosition(camera) - player.Position;
                    float distance = MathF.Min(dir.Length(), maxDistance);
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
                }, 0.0f, 0.75f);
        }

        public Entity CreateProjectileAttack(CollisionBitmask layer, 
            CollisionBitmask mask, Vector2 dir, Vector2 position, Color color)
        {
            Entity ent = entityManager.CreateEntity();
            ent.Position = position;

            PhysicsCmp phy = entityManager.AddComponent(ent, new PhysicsCmp());
            phy.Inertia        = 0.0f;
            phy.LinearVelocity = dir;
            Vector2 normalDir = Vector2.Normalize(dir);

            TriggerColliderCmp col = entityManager.AddComponent(ent,
                new TriggerColliderCmp(new CircleCollider(8.0f), 
                layer, mask));
            col.OnTriggerEnter += (Entity e1, TriggerColliderCmp c1,
                Entity e2, ColliderBody c2, ColliderType type, in Manifold m)
                =>
            {
                if(type == ColliderType.Dynamic)
                {
                    if(entityManager.TryGetComponent(e2, out PhysicsCmp phy2))
                    {
                        phy2.Force += normalDir * 5000.0f;
                    }

                    if (entityManager.TryGetComponent(e2, out HealthCmp health))
                    {
                        health.CurrentHealth -= 20.0f;
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

        protected Entity CreateMeleeAttack(CollisionBitmask layer,
            CollisionBitmask mask, Vector2 position, float size, float knockback, 
            Color color)
        {
            Entity ent = entityManager.CreateEntity();
            ent.Position = position;

            TriggerColliderCmp col = entityManager.AddComponent(ent,
                new TriggerColliderCmp(new CircleCollider(size),
                layer, mask));
            col.OnTriggerEnter += (Entity e1, TriggerColliderCmp c1,
                Entity e2, ColliderBody c2, ColliderType type, in Manifold m)
                =>
            {
                if (type == ColliderType.Dynamic)
                {
                    if (entityManager.TryGetComponent(e2, out PhysicsCmp phy2))
                    {
                        phy2.Force += Vector2.Normalize(e2.Position - ent.Position) 
                            * knockback;
                    }

                    if (entityManager.TryGetComponent(e2, out HealthCmp health))
                    {
                        health.CurrentHealth -= 50.0f;
                    }
                }
            };

            HealthCmp health = entityManager.AddComponent(ent, new HealthCmp(100.0f));
            entityManager.AddComponent(ent, new ScriptCmp((Entity attack) =>
            {
                health.CurrentHealth -= health.MaxHealth * 0.0166666f * (1.0f / 0.1f);
            }));

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
            entityManager.AddComponent(enemy,
                new HealthCmp(100.0f));
            entityManager.AddComponent(enemy,
                new EnemyCmp(CreateEnemyDice(), 3, 4.0f));
        }

        protected override void Update(GameTime gameTime)
        {
            DebugTimer.Start("Update");

            if (KeyboardInput.IsKeyPressed(Keys.Escape))
                Exit();

            float dt = (float) gameTime.ElapsedGameTime.TotalSeconds;
            Input.Update();
            gameStates.Update();
            gameStates.UpdateActiveStates(gameTime);

            HandleCamera(dt);

            if (KeyboardInput.IsKeyPressed(Keys.D3))
            {
                CreateEnemy(MouseInput.GetPosition(camera));
            }

            HandlePlayerInput(dt);
            UpdateEnemies(dt);

            updateSystems.UpdateSystems();

            entityManager.ForEachComponent((Entity entity, HealthCmp health) =>
            {
                if (health.CurrentHealth <= 0.0f && entity.IsValid) 
                    entityManager.RemoveEntity(entity);
            });


            DebugTimer.Stop("Update");
            base.Update(gameTime);
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
                    t.Source, Color.White, 0.0f, Vector2.Zero, 1.0f, SpriteEffects.None, 1.0f);
            }

            entityManager.ForEachComponent((Entity e, EnemyCmp enemy) =>
            {
                const float SCALE = 0.5f;
                const float MARGIN = 5.0f;
                DiceQueue queue = enemy.Queue;
                Vector2 drawStartPos = e.Position - new Vector2(
                    queue.TotalWidth * SCALE * 0.5f, 32.0f);

                for (int i = 0; i < queue.Queue.Count; ++i)
                {
                    DiceFace face = queue.Queue[i];
                    spriteBatch.Draw(mainTexture, drawStartPos +
                        new Vector2(i * 32.0f * SCALE + i * MARGIN * SCALE, 0.0f),
                        face.SourceRect, face.Color, 0.0f, Vector2.Zero, SCALE,
                        SpriteEffects.None, 0.0f);
                }

                int turnAdvantage = Math.Min(enemy.TurnAdvantage,
                        EnemyCmp.MaxTurnAdvantage);
                spriteBatch.DrawString(font, turnAdvantage.ToString(), 
                    e.Position + new Vector2(12.0f, 0.0f), Color.White, 0.0f, 
                    Vector2.Zero, SCALE, SpriteEffects.None, 0.0f);
            });

            spriteBatch.End();

            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            foreach (PlayerDiceQueue queue in diceQueues)
            {
                queue.Draw(spriteBatch, blankTexture);
            }
            spriteBatch.End();

            shapeBatch.Begin(camera);

            shapeBatch.DrawLine(new Vector2(0.0f, 0.0f),
                new Vector2(0.0f, 50.0f), 4.0f, new Color(0, 255, 0));
            shapeBatch.DrawLine(new Vector2(0.0f, 0.0f),
                new Vector2(50.0f, 0.0f), 4.0f, Color.Red);

            if(activeQueue != null)
            {
                DiceFace face = activeQueue.ActiveFace;

                if(activeQueue.State != QueueState.Default)
                {
                    const float MAX_SIZE = 32.0f;
                    float t = 0.0f;
                    if(activeQueue.State == QueueState.PreExecutingAction)
                        t = activeQueue.CurrentActionDelay / activeQueue.FaceActionDelay;
                    else
                        t = activeQueue.CurrentActionCooldown / activeQueue.FaceActionCooldown;

                    shapeBatch.DrawFilledRectangle(player.Position +
                        new Vector2(-MAX_SIZE * 0.5f, 15.0f),
                        new Vector2(MAX_SIZE, 4.0f), Color.Red);
                    shapeBatch.DrawFilledRectangle(player.Position + 
                        new Vector2(-MAX_SIZE * 0.5f, 16.0f),
                        new Vector2(MAX_SIZE * t, 2.0f), Color.White);
                }
                else
                {
                    face.DrawCommand(shapeBatch);
                }
            }

            entityManager.ForEachComponent((Entity e, EnemyCmp enemy) =>
            {

                if (enemy.State != QueueState.Default)
                {
                    const float MAX_SIZE = 32.0f;
                    DiceFace face = enemy.Queue.ActiveFace;

                    float t = 0.0f;
                    if (enemy.State == QueueState.PreExecutingAction)
                        t = enemy.CurrentActionDelay / enemy.FaceActionDelay;
                    else
                        t = enemy.CurrentActionCooldown / enemy.FaceActionCooldown;
                    shapeBatch.DrawFilledRectangle(e.Position +
                        new Vector2(-MAX_SIZE * 0.5f, 15.0f),
                        new Vector2(MAX_SIZE, 4.0f), Color.Red);
                    shapeBatch.DrawFilledRectangle(e.Position +
                        new Vector2(-MAX_SIZE * 0.5f, 16.0f),
                        new Vector2(MAX_SIZE * t, 2.0f), Color.White);
                }
                else if(enemy.TurnAdvantage > 0)
                {
                    const float MAX_SIZE = 32.0f;
                    float t = enemy.CurrentTurnTimer / enemy.TurnTimer;
                    shapeBatch.DrawFilledRectangle(e.Position +
                        new Vector2(-MAX_SIZE * 0.5f, 16.0f),
                        new Vector2(MAX_SIZE * t, 2.0f), Color.Yellow);
                }
            });

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

        /*BORRAR*/
        public void UpdateEnemies(float dt)
        {
            entityManager.ForEachComponent((Entity e, EnemyCmp cmp) =>
            {
                if (cmp.TurnAdvantage == 0) return;

                if(cmp.State == QueueState.Default)
                {
                    int turnAdvantage = Math.Min(cmp.TurnAdvantage,
                        EnemyCmp.MaxTurnAdvantage);

                    cmp.CurrentTurnTimer -= dt * (1 + (turnAdvantage - 1) *
                        EnemyCmp.TurnAdvantageTimeMult);

                    if (cmp.CurrentTurnTimer <= 0.0f)
                    {
                        cmp.State = QueueState.PreExecutingAction;

                        cmp.CurrentActionDelay    = cmp.Queue.ActiveFace.ActionDelay;
                        cmp.CurrentActionCooldown = cmp.Queue.ActiveFace.ActionCooldown;
                        cmp.FaceActionDelay       = cmp.Queue.ActiveFace.ActionDelay;
                        cmp.FaceActionCooldown    = cmp.Queue.ActiveFace.ActionCooldown;
                    }
                }

                if (cmp.State == QueueState.PreExecutingAction)
                {
                    DiceFace face = cmp.Queue.ActiveFace;
                    if (cmp.CurrentActionDelay <= 0.0f)
                    {
                        face.EnemyAction(e);
                        cmp.Queue.Dequeue();
                        cmp.State = QueueState.PostExecutingAction;
                    }
                    else
                        cmp.CurrentActionDelay -= dt;
                }

                if(cmp.State == QueueState.PostExecutingAction)
                {
                    if (cmp.CurrentActionCooldown <= 0.0f)
                    {
                        cmp.CurrentTurnTimer = cmp.TurnTimer;
                        cmp.State = QueueState.Default;
                        cmp.TurnAdvantage--;
                    }
                    else
                        cmp.CurrentActionCooldown -= dt;
                }
            });
        }
        
        public void HandlePlayerInput(float dt)
        {
            if(activeQueue == null)
            {
                foreach (PlayerDiceQueue queue in diceQueues)
                {
                    if (queue.Update())
                    {
                        activeQueue = queue;
                        break;
                    }
                }
            }

            if (activeQueue != null)
            {
                if (activeQueue.State == QueueState.Default) //MouseInput.IsLeftButtonPressed() && 
                {
                    activeQueue.State = QueueState.PreExecutingAction;
                    activeQueue.CurrentActionDelay =
                        activeQueue.ActiveFace.ActionDelay;
                    activeQueue.CurrentActionCooldown =
                            activeQueue.ActiveFace.ActionCooldown;
                    activeQueue.FaceActionDelay =
                        activeQueue.ActiveFace.ActionDelay;
                    activeQueue.FaceActionCooldown =
                        activeQueue.ActiveFace.ActionCooldown;
                }

                if (activeQueue.State == QueueState.PreExecutingAction)
                {
                    DiceFace face = activeQueue.ActiveFace;
                    if(activeQueue.CurrentActionDelay <= 0.0f)
                    {
                        face.Action();

                        activeQueue.State = QueueState.PostExecutingAction;

                        activeQueue.Dequeue();
                        AddTurnAdvantage(1);
                    }
                    else
                        activeQueue.CurrentActionDelay -= dt;
                }

                if(activeQueue.State == QueueState.PostExecutingAction)
                {
                    if (activeQueue.CurrentActionCooldown <= 0.0f)
                    {
                        activeQueue.State = QueueState.Default;
                        activeQueue = null;
                    }
                    else
                        activeQueue.CurrentActionCooldown -= dt;
                }
            }
        }

        public void AddTurnAdvantage(int amount)
        {
            entityManager.ForEachComponent((EnemyCmp cmp) =>
            {
                cmp.TurnAdvantage += amount;
            });
        }

        public void HandleCamera(float dt)
        {
            if (KeyboardInput.IsKeyPressed(Keys.F))
            {
                graphics.HardwareModeSwitch = false;

                graphics.ToggleFullScreen();
                PrintSizes();
            }

            if (MouseInput.ScrollHasChanged())
            {
                int sign = -MathF.Sign(MouseInput.ScrollValueDiff);
                camera.Zoom += 0.1f * sign;
            }

            if (KeyboardInput.IsKeyPressed(Keys.D5))
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
            if (KeyboardInput.IsKeyDown(Keys.Up))
                vel.Y -= 1.0f;
            if (KeyboardInput.IsKeyDown(Keys.Down))
                vel.Y += 1.0f;
            if (KeyboardInput.IsKeyDown(Keys.Left))
                vel.X -= 1.0f;
            if (KeyboardInput.IsKeyDown(Keys.Right))
                vel.X += 1.0f;

            if (vel != Vector2.Zero)
            {
                vel.Normalize();
                vel *= CAM_SPEED * dt;
                camera.Position += vel;
            }
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
    }
}