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
            SpriteFont uiFont = game.Content.Load<SpriteFont>(
                GameContent.FontPath("MainFont"));
            
            ui = new UIContext(game.Screen);

            Constraints titleConstraints = new Constraints(
                new CenterConstraint(),
                new PercentConstraint(0.2f),
                new AspectConstraint(1.0f),
                new PercentConstraint(0.2f));
            UIString titleString = new UIString(ui, titleConstraints,
                uiFont, "Lose", Color.Red);
            ui.AddElement(titleString);
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
