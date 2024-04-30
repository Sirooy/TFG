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
        private UIImage[] backgrounds;

        public MainMenuState(GameMain game) 
        {
            this.game        = game;
            this.spriteBatch = game.SpriteBatch;
            this.gameStates  = game.GameStates;
            this.backgrounds = new UIImage[2]; 

            CreateUI();
        }

        private void CreateUI()
        {
            Texture2D uiTexture   = game.Content.Load<Texture2D>(
                GameContent.TexturePath("UI"));
            Texture2D backTexture = game.Content.Load<Texture2D>(
                GameContent.BackgroundPath("MainMenuBackground"));
            Texture2D titleTexture = game.Content.Load<Texture2D>(
                GameContent.TexturePath("GameTitle"));
            SpriteFont uiFont     = game.Content.Load<SpriteFont>(
                GameContent.FontPath("MainFont"));

            ui = new UIContext(game.Screen);

            Constraints backgroundConstraints = new Constraints(
                null,
                new CenterConstraint(),
                new PercentConstraint(1.0f),
                new AspectConstraint(1.0f));
            backgrounds[0] = new UIImage(ui, backgroundConstraints,
                backTexture, new Rectangle(0, 0, backTexture.Width, backTexture.Height));
            backgrounds[1] = new UIImage(ui, backgroundConstraints,
                backTexture, new Rectangle(0, 0, backTexture.Width, backTexture.Height));
            ui.AddElement(backgrounds[0]);
            ui.AddElement(backgrounds[1]);
            backgrounds[1].Position = new Vector2(backgrounds[0].Size.X, 
                backgrounds[1].Position.Y);

            Constraints titleImgConstraints = new Constraints(
                new CenterConstraint(),
                new PercentConstraint(0.1f),
                new AspectConstraint(1.0f),
                new PercentConstraint(0.25f));
            UIImage titleImage = new UIImage(ui, titleImgConstraints,
                titleTexture, new Rectangle(0, 0, 
                titleTexture.Width, titleTexture.Height));
            ui.AddElement(titleImage);

            Constraints layoutConstraints = new Constraints(
                new CenterConstraint(),
                new PercentConstraint(0.4f),
                new PercentConstraint(0.4f),
                new PercentConstraint(0.5f));
            UILayout buttonsLayout = new UILayout(ui, layoutConstraints,
                LayoutType.Vertical, LayoutAlign.Center, 
                new PercentConstraint(0.05f));
            ui.AddElement(buttonsLayout);

            Constraints buttonConstraints = new Constraints(
                new CenterConstraint(),
                new CenterConstraint(),
                new PercentConstraint(0.7f),
                new AspectConstraint(1.0f));

            UIImage b1 = UIUtil.CreateCommonButton(ui, buttonConstraints,
                "Play", game.Content);
            UIImage b2 = UIUtil.CreateCommonButton(ui, buttonConstraints,
                "Exit", game.Content);

            buttonsLayout.AddElement(b1);
            buttonsLayout.AddElement(b2);

            UIButtonEventHandler b1EventHandler = (UIButtonEventHandler) b1.EventHandler;
            b1EventHandler.OnPress += (UIElement element) =>
            {
                gameStates.PopAllActiveStates();
                gameStates.PushState<PlayGameState>();
            };

            UIButtonEventHandler b2EventHandler = (UIButtonEventHandler)b2.EventHandler;
            b2EventHandler.OnPress += (UIElement element) =>
            {
                game.Exit();
            };
        }

        public override StateResult Update(GameTime gameTime)
        {
            const float SLIDE_SPEED = 100.0f;
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            //Update positions
            for(int i = 0;i < backgrounds.Length; ++i)
            {
                backgrounds[i].Position -= new Vector2(SLIDE_SPEED * dt, 0.0f);
            }

            //Fix positions
            for (int i = 0; i < backgrounds.Length; ++i)
            {
                UIImage back = backgrounds[i];
                if (back.Position.X <= -back.Size.X)
                {
                    UIImage next = backgrounds[(i + 1) % backgrounds.Length];
                    back.Position = new Vector2(
                        next.Position.X + next.Size.X,
                        back.Position.Y);
                }
            }

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
