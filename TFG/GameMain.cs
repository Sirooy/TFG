using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Engine.Core;
using Engine.Graphics;
using Engine.Debug;
using Systems;
using States;
using UI;
using Core;

public class GameMain : Game
{
    private GraphicsDeviceManager graphics;
    private SpriteBatch spriteBatch;
    private GameStateStack gameStates;

    private RenderScreen screen;
    private SpriteFont font;

    public const int WindowWidth  = 1024;
    public const int WindowHeight = 720;

    public GraphicsDeviceManager Graphics { get { return graphics; } }
    public GameStateStack GameStates { get { return gameStates; } }
    public SpriteBatch SpriteBatch { get { return spriteBatch; } }
    public RenderScreen Screen { get { return screen; } }

    public GameMain()
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

        screen      = new RenderScreen(GraphicsDevice, 1024, 720);
        spriteBatch = new SpriteBatch(GraphicsDevice);
        gameStates  = new GameStateStack();

        DebugTimer.Register("Update", 120);
        DebugTimer.Register("Draw",   50);
        DebugTimer.Register("Physics", 120);
        DebugDraw.Init(GraphicsDevice);
        DebugDraw.RegisterLayer(PhysicsSystem.DEBUG_DRAW_LAYER, 2.0f, 1.0f, 16);
        DebugDraw.RegisterLayer(UIElement.DEBUG_DRAW_LAYER, 4.0f, 2.0f, 16, false);
        DebugDraw.SetMainLayerData(2.0f, 1.0f, 16);
        DrawUtil.Init(GraphicsDevice, Content);

        gameStates.RegisterState(new MainMenuState(this));
        gameStates.RegisterState(new PlayGameState(this));
        gameStates.PushState<MainMenuState>();

        PrintSizes();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        DebugLog.Info("Loading main content");

        font = Content.Load<SpriteFont>(
            GameContent.FontPath("DebugFont"));
    }

    protected override void Update(GameTime gameTime)
    {
        DebugTimer.Start("Update");
        Input.Update();

        EnableDisableDebugDraw();
        gameStates.Update();
        gameStates.UpdateActiveStates(gameTime);

        DebugTimer.Stop("Update");

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        DebugTimer.Start("Draw");
        screen.Attach();

        DrawWorldAxles();
        gameStates.DrawActiveStates(gameTime);

        DebugDraw.Draw();
        DebugTimer.Stop("Draw");
        DebugTimer.Draw(spriteBatch, font);

        screen.Present(spriteBatch, SamplerState.PointClamp, Color.Green);

        base.Draw(gameTime);
    }

    private void DrawWorldAxles()
    {
        DebugDraw.Line(new Vector2(0.0f, 0.0f),
            new Vector2(0.0f, 50.0f), new Color(0, 255, 0));
        DebugDraw.Line(new Vector2(0.0f, 0.0f),
            new Vector2(50.0f, 0.0f), Color.Red);
        DebugDraw.Point(new Vector2(0.0f, 0.0f), Color.Blue);
    }

    private void Graphics_DeviceReset(object sender, EventArgs e)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Reset");
        PrintSizes();
        Console.WriteLine("################");
    }

    [Conditional(DebugDraw.DEBUG_DEFINE)]
    public void EnableDisableDebugDraw()
    {
        if(KeyboardInput.IsKeyPressed(Keys.NumPad1))
        {
            bool enabled = DebugDraw.IsMainLayerEnabled();
            DebugDraw.SetMainLayerEnabled(!enabled);
        }

        if (KeyboardInput.IsKeyPressed(Keys.NumPad2))
        {
            bool enabled = DebugDraw.IsLayerEnabled(PhysicsSystem.DEBUG_DRAW_LAYER);
            DebugDraw.SetLayerEnabled(PhysicsSystem.DEBUG_DRAW_LAYER, !enabled);
        }

        if (KeyboardInput.IsKeyPressed(Keys.NumPad3))
        {
            bool enabled = DebugDraw.IsLayerEnabled(UIElement.DEBUG_DRAW_LAYER);
            DebugDraw.SetLayerEnabled(UIElement.DEBUG_DRAW_LAYER, !enabled);
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