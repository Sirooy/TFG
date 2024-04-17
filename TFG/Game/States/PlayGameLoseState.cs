using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Engine.Core;
using Engine.Debug;
using System;

namespace States
{
    public class PlayGameLoseState : GameState
    {
        private GameMain game;
        private PlayGameState parentState;

        public PlayGameLoseState(GameMain game, PlayGameState parentState) 
        {
            this.game        = game;
            this.parentState = parentState;
        }

        public override StateResult Update(GameTime gameTime)
        {
            if (KeyboardInput.IsKeyPressed(Keys.Enter) ||
                KeyboardInput.IsKeyPressed(Keys.Space))
            {
                game.GameStates.PopAllActiveStates();
                game.GameStates.PushState<MainMenuState>();
            }

            return StateResult.StopExecuting;
        }

        public override StateResult Draw(GameTime gameTime)
        {
            game.GraphicsDevice.Clear(Color.Yellow);

            return StateResult.StopExecuting;
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
