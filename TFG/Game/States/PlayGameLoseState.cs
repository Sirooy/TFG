using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Engine.Core;
using Engine.Debug;
using UI;
using Core;

namespace States
{
    public class PlayGameLoseState : GameState
    {
        private GameMain game;
        private PlayGameState parentState;
        private SpriteBatch spriteBatch;
        private UIContext ui;

        public PlayGameLoseState(GameMain game, PlayGameState parentState) 
        {
            this.game        = game;
            this.parentState = parentState;
            this.spriteBatch = game.SpriteBatch;

            CreateUI();
        }

        private void CreateUI()
        {
            Texture2D backTexture  = game.Content.Load<Texture2D>(
                GameContent.BackgroundPath("CommonBackground"));
            Texture2D titleTexture = game.Content.Load<Texture2D>(
                GameContent.TexturePath("GameOverTitle"));

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
                new PercentConstraint(0.4f));
            UIImage titleString = new UIImage(ui, titleConstraints,
                titleTexture, new Rectangle(0, 0,
                titleTexture.Width, titleTexture.Height));
            ui.AddElement(titleString);

            Constraints returnButtonConstraints = new Constraints(
                new CenterConstraint(),
                new PercentConstraint(0.6f),
                new AspectConstraint(1.0f),
                new PercentConstraint(0.15f));
            UIImage returnButton = UIUtil.CreateCommonButton(ui,
                returnButtonConstraints, "Return", game.Content);
            ui.AddElement(returnButton);

            UIButtonEventHandler returnButtonEventHandler = (UIButtonEventHandler)
                returnButton.EventHandler;
            returnButtonEventHandler.OnPress += (UIElement element) =>
            {
                game.GameStates.PopAllActiveStates();
                game.GameStates.PushState<MainMenuState>();
            };
        }

        public override StateResult Update(GameTime gameTime)
        {
            if (KeyboardInput.IsKeyPressed(Keys.Enter) ||
                KeyboardInput.IsKeyPressed(Keys.Space))
            {
                game.GameStates.PopAllActiveStates();
                game.GameStates.PushState<MainMenuState>();
            }

            ui.Update();

            return StateResult.StopExecuting;
        }

        public override StateResult Draw(GameTime gameTime)
        {
            game.GraphicsDevice.Clear(Color.White);

            spriteBatch.Begin(samplerState: SamplerState.PointWrap,
                blendState: BlendState.NonPremultiplied);
            ui.Draw(spriteBatch);
            spriteBatch.End();

            return StateResult.KeepExecuting;
        }

        public override void OnEnter()
        {
            parentState.EntityManager.Clear();

            DebugDraw.Camera = null;
            DebugLog.Info("OnEnter state: {0}", nameof(PlayGameLoseState));
        }

        public override void OnExit()
        {
            DebugLog.Info("OnExit state: {0}", nameof(PlayGameLoseState));
        }
    }
}
