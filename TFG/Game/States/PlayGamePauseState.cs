using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Engine.Core;
using Engine.Debug;
using UI;
using Core;

namespace States
{
    public class PlayGamePauseState : GameState
    {
        private GameMain game;
        private PlayGameState parentState;
        private SpriteBatch spriteBatch;
        private UIContext ui;

        public PlayGamePauseState(GameMain game, PlayGameState parentState)
        {
            this.game = game;
            this.parentState = parentState;
            this.spriteBatch = game.SpriteBatch;

            CreateUI();
        }

        private void CreateUI()
        {
            Texture2D backTexture = game.Content.Load<Texture2D>(
                GameContent.BackgroundPath("CommonBackground"));
            Texture2D titleTexture = game.Content.Load<Texture2D>(
                GameContent.TexturePath("PauseTitle"));

            ui = new UIContext(game.Screen);

            //BACKGROUND IMAGE
            Constraints backgroundConstraints = new Constraints(
                new CenterConstraint(),
                new CenterConstraint(),
                new PercentConstraint(1.0f),
                new AspectConstraint(1.0f));
            UIImage background = new UIImage(ui, backgroundConstraints,
                backTexture, new Rectangle(0, 0, backTexture.Width, backTexture.Height));
            ui.AddElement(background);

            Constraints titleConstraints = new Constraints(
                new CenterConstraint(),
                new PercentConstraint(0.1f),
                new AspectConstraint(1.0f),
                new PercentConstraint(0.2f));
            UIImage titleString = new UIImage(ui, titleConstraints,
                titleTexture, new Rectangle(0, 0,
                titleTexture.Width, titleTexture.Height));
            ui.AddElement(titleString);

            Constraints buttonsLayoutConstraints = new Constraints(
                new CenterConstraint(),
                new PercentConstraint(0.4f),
                new PercentConstraint(1.0f),
                new PercentConstraint(0.4f));
            UILayout buttonsLayout = new UILayout(ui, buttonsLayoutConstraints,
                LayoutType.Vertical, LayoutAlign.Center, new PercentConstraint(0.05f));
            ui.AddElement(buttonsLayout);

            Constraints buttonConstraints = new Constraints(
                new CenterConstraint(),
                new PercentConstraint(0.6f),
                new AspectConstraint(1.0f),
                new PercentConstraint(0.3f));
            UIImage resumeButton = UIUtil.CreateCommonButton(ui,
                buttonConstraints, "Resume", game.Content);
            UIImage mainMenuButton = UIUtil.CreateCommonButton(ui,
                buttonConstraints, "Main menu", game.Content);
            
            buttonsLayout.AddElement(resumeButton);
            buttonsLayout.AddElement(mainMenuButton);

            UIButtonEventHandler returnButtonEventHandler = (UIButtonEventHandler)
                resumeButton.EventHandler;
            returnButtonEventHandler.OnPress += (UIElement element) =>
            {
                parentState.GameStates.PopLastState();
            };

            UIButtonEventHandler mainMenuButtonEventHandler = (UIButtonEventHandler)
                mainMenuButton.EventHandler;
            mainMenuButtonEventHandler.OnPress += (UIElement element) =>
            {
                game.GameStates.PopAllActiveStates();
                game.GameStates.PushState<MainMenuState>();
            };
        }

        public override StateResult Update(GameTime gameTime)
        {
            if (KeyboardInput.IsKeyPressed(Keys.Escape) ||
                KeyboardInput.IsKeyPressed(Keys.Space))
            {
                parentState.GameStates.PopLastState();
            }

            ui.Update();

            return StateResult.StopExecuting;
        }

        public override StateResult Draw(GameTime gameTime)
        {
            spriteBatch.Begin(samplerState: SamplerState.PointWrap,
                blendState: BlendState.NonPremultiplied);
            ui.Draw(spriteBatch);
            spriteBatch.End();

            return StateResult.StopExecuting;
        }

        public override void OnEnter()
        {
            DebugLog.Info("OnEnter state: {0}", nameof(PlayGamePauseState));
        }

        public override void OnExit()
        {
            DebugLog.Info("OnExit state: {0}", nameof(PlayGamePauseState));
        }
    }
}
