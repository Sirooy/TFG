using Engine.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using States;
using UI;

namespace Core
{
    public static class UIUtil
    {
        public static UIImage CreateCommonButton(UIContext context, 
            Constraints constraints, string text, ContentManager content)
        {
            Texture2D texture = content.Load<Texture2D>(
                GameContent.TexturePath("UI"));
            SpriteFont font   = content.Load<SpriteFont>
                (GameContent.FontPath("MainFont"));

            UIImage button = new UIImage(context, constraints,
                texture, new Rectangle(0, 0, 48, 16));

            Constraints buttonTextConstraints = new Constraints(
                new CenterConstraint(),
                new CenterConstraint(),
                new AspectConstraint(1.0f),
                new PercentConstraint(0.5f));

            UIString buttonText = new UIString(context, buttonTextConstraints,
                font, text, Color.White);

            button.AddElement(buttonText);

            UIButtonEventHandler buttonEventHandler = new UIButtonEventHandler();
            buttonEventHandler.OnEnterHover += (UIElement element) =>
            {
                button.Source = new Rectangle(48, 0, 48, 16);
            };
            buttonEventHandler.OnExitHover += (UIElement element) =>
            {
                button.Source = new Rectangle(0, 0, 48, 16);
            };
            button.EventHandler = buttonEventHandler;

            return button;
        }
    }
}
