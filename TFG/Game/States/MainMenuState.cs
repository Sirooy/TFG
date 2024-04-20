using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Engine.Core;
using Engine.Debug;
using UI;
using Core;

namespace States
{
    public class MainMenuState : GameState
    {
        private GameMain game;
        private SpriteBatch spriteBatch;
        private GameStateStack gameStates;

        private UIContext ui;

        public MainMenuState(GameMain game) 
        {
            this.game        = game;
            this.spriteBatch = game.SpriteBatch;
            this.gameStates  = game.GameStates;

            CreateUI();
        }

        private void CreateUI()
        {
            Texture2D uiTexture = game.Content.Load<Texture2D>(
                GameContent.TexturePath("UI"));
            SpriteFont uiFont   = game.Content.Load<SpriteFont>(
                GameContent.FontPath("MainFont"));

            ui = new UIContext(game.Screen);

            Constraints titleConstraints = new Constraints(
                new CenterConstraint(),
                new PercentConstraint(0.1f),
                new AspectConstraint(1.0f),
                new PercentConstraint(0.15f));
            UIString titleString = new UIString(ui, titleConstraints,
                uiFont, "Titulo", Color.White);
            ui.AddElement(titleString);

            Constraints layoutConstraints = new Constraints(
                new CenterConstraint(),
                new PercentConstraint(0.3f),
                new PercentConstraint(0.4f),
                new PercentConstraint(0.6f));
            UILayout buttonsLayout = new UILayout(ui, layoutConstraints,
                UILayout.LayoutType.Vertical, new PercentConstraint(0.05f));
            ui.AddElement(buttonsLayout);

            Constraints buttonConstraints = new Constraints(
                new CenterConstraint(),
                new CenterConstraint(),
                new PercentConstraint(1.0f),
                new AspectConstraint(1.0f));

            UIImage b1 = new UIImage(ui, buttonConstraints, 
                uiTexture, new Rectangle(0, 0, 128, 32));
            UIImage b2 = new UIImage(ui, buttonConstraints,
                uiTexture, new Rectangle(0, 0, 128, 32));
            UIImage b3 = new UIImage(ui, buttonConstraints,
                uiTexture, new Rectangle(0, 0, 128, 32));
            buttonsLayout.AddElement(b1);
            buttonsLayout.AddElement(b2);
            buttonsLayout.AddElement(b3);

            Constraints buttonTextConstraints = new Constraints(
                new CenterConstraint(),
                new CenterConstraint(),
                new AspectConstraint(1.0f),
                new PercentConstraint(0.5f));

            UIString b1Text = new UIString(ui, buttonTextConstraints,
                uiFont, "Play", Color.White);
            UIString b2Text = new UIString(ui, buttonTextConstraints,
                uiFont, "Settings", Color.White);
            UIString b3Text = new UIString(ui, buttonTextConstraints,
                uiFont, "Exit", Color.White);

            b1.AddElement(b1Text);
            b2.AddElement(b2Text);
            b3.AddElement(b3Text);

            UIButtonEventHandler b1EventHandler = new UIButtonEventHandler();
            b1EventHandler.OnPress += (UIElement element) =>
            {
                gameStates.PopAllActiveStates();
                gameStates.PushState<PlayGameState>();
            };
            b1.EventHandler = b1EventHandler;

            UIButtonEventHandler b2EventHandler = new UIButtonEventHandler();
            b2EventHandler.OnPress += (UIElement element) =>
            {
                DebugLog.Info("SETTINGS");
            };
            b2.EventHandler = b2EventHandler;

            UIButtonEventHandler b3EventHandler = new UIButtonEventHandler();
            b3EventHandler.OnPress += (UIElement element) =>
            {
                game.Exit();
            };
            b3.EventHandler = b3EventHandler;
        }

        public override StateResult Update(GameTime gameTime)
        {
            ui.Update();

            return StateResult.KeepExecuting;
        }

        public override StateResult Draw(GameTime gameTime)
        {
            game.GraphicsDevice.Clear(Color.Blue);

            spriteBatch.Begin(samplerState: SamplerState.PointWrap);
            ui.Draw(spriteBatch);
            spriteBatch.End();

            return StateResult.KeepExecuting;
        }

        public override void OnEnter()
        {
            DebugDraw.Camera = null;
            DebugLog.Info("OnEnter state: {0}", nameof(MainMenuState));
        }

        public override void OnExit()
        {
            DebugLog.Info("OnExit state: {0}", nameof(MainMenuState));
        }
    }
}
