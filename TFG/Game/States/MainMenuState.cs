using System;
using Engine.Core;
using Engine.Debug;
using Engine.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using TFG;
using UI;

namespace States
{
    public class MainMenuState : GameState
    {
        private GameMain game;
        private SpriteBatch spriteBatch;
        private GameStateStack gameStates;

        private UIContext ui;
        private UIElement rect;
        private UIString text;

        public MainMenuState(GameMain game) 
        {
            this.game        = game;
            this.spriteBatch = game.SpriteBatch;
            this.gameStates  = game.GameStates;

            this.ui = new UIContext(game.Screen);

            Constraints constraints1 = new Constraints(   
                new CenterConstraint(),
                new CenterConstraint(),
                new PercentConstraint(0.5f),
                new PercentConstraint(0.5f));
             rect = new UIRectangle(
                constraints1, 
                Color.Red);

            Constraints constraints2 = new Constraints(
                new CenterConstraint(),
                new CenterConstraint(),
                new PercentConstraint(1.0f),
                new PercentConstraint(0.2f));

            UIImage image = new UIImage(
                constraints2,
                game.Content.Load<Texture2D>("DiceFaceSpriteSheet"),
                new Rectangle(32, 0, 64, 32));

            UIRectangle rect2 = new UIRectangle(
                constraints2,
                Color.Honeydew);
            rect.AddElement(rect2);

            Constraints textConstraints = new Constraints(
                new CenterConstraint(),
                new CenterConstraint(),
                new AspectConstraint(1.0f),
                new PercentConstraint(1.0f)
                );
            text = new UIString(
                textConstraints,
                game.Content.Load<SpriteFont>("DebugFont"),
                "This is a Text", Color.Green);
            rect2.AddElement(text);

            this.ui.AddElement(rect);
        }

        public override StateResult Update(GameTime gameTime)
        {
            float dt = (float) gameTime.ElapsedGameTime.TotalSeconds;

            if(KeyboardInput.IsKeyPressed(Keys.Enter) ||
                KeyboardInput.IsKeyPressed(Keys.Space))
            {
                gameStates.PopAllActiveStates();
                gameStates.PushState<PlayGameState>();
            }

            this.ui.Update();
            if (KeyboardInput.IsKeyDown(Keys.Right))
                rect.Position += new Vector2(50.0f * dt, 0.0f);
            if (KeyboardInput.IsKeyDown(Keys.Left))
                rect.Position -= new Vector2(50.0f * dt, 0.0f);

            if (KeyboardInput.IsKeyPressed(Keys.K))
                text.Text = text.Text + "_";

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
