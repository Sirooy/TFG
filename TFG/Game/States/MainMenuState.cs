using System;
using Engine.Core;
using Engine.Debug;
using Engine.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using TFG;

namespace States
{
    public class MainMenuState : GameState
    {
        private GameMain game;
        private SpriteBatch spriteBatch;
        private GameStateStack gameStates;

        public MainMenuState(GameMain game) 
        {
            this.game        = game;
            this.spriteBatch = game.SpriteBatch;
            this.gameStates  = game.GameStates;
        }

        public override bool Update(GameTime gameTime)
        {
            if(KeyboardInput.IsKeyPressed(Keys.Enter) ||
                KeyboardInput.IsKeyPressed(Keys.Space))
            {
                gameStates.PopAllActiveStates();
                gameStates.PushState<PlayGameState>();
            }

            return false;
        }

        public override bool Draw(GameTime gameTime)
        {
            game.GraphicsDevice.Clear(Color.Blue);

            return false;
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
