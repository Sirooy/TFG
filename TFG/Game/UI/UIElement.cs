using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Engine.Core;
using Engine.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace UI
{
    public abstract class UIElement
    {
        protected Vector2 position;
        protected Vector2 size;
        protected Constraints constraints;
        protected List<UIElement> children;
        public Color Color;
        public bool IsVisible;

        public UIElement Parent { get; internal set; }
        public IPositionConstraint XConstraint  { get { return constraints.XConstraint; } }
        public IPositionConstraint YConstraint  { get { return constraints.YConstraint; } }
        public ISizeConstraint WidthConstraint  { get { return constraints.WidthConstraint; } }
        public ISizeConstraint HeightConstraint { get { return constraints.HeightConstraint; } }


        public virtual Vector2 Position 
        { 
            get { return position;  }
            set 
            { 
                position = value;

                UpdateChildrenConstraints();
            }
        }
        public virtual Vector2 Size 
        { 
            get { return size;  } 
            set 
            { 
                size = value;

                UpdateChildrenConstraints();
            }
        }

        public UIElement(Constraints constraints)
        {
            this.constraints = constraints;
            this.children    = new List<UIElement>();
            this.Color       = Color.White;
            this.IsVisible   = true;
        }

        public virtual void Update() 
        {
            foreach (UIElement element in children)
                element.Update();
        }
        public virtual void Draw(SpriteBatch spriteBatch) 
        { 
            foreach (UIElement element in children)
            {
                if (element.IsVisible)
                    element.Draw(spriteBatch);
            }   
        }

        public virtual void AddElement(UIElement element)
        {
            children.Add(element);
            element.Parent = this;
            element.UpdateConstraints();
        }

        private void RecalculatePosAndSizeConstraints()
        {
            Vector2 newPos  = position;
            Vector2 newSize = size;

            if (constraints.XConstraint != null)
                newPos.X = constraints.XConstraint.GetXValue(this);

            if (constraints.YConstraint != null)
                newPos.Y = constraints.YConstraint.GetYValue(this);

            if (constraints.WidthConstraint != null)
                newSize.X = constraints.WidthConstraint.GetXValue(this);

            if (constraints.HeightConstraint != null)
                newSize.Y = constraints.HeightConstraint.GetYValue(this);

            position = newPos;
            size     = newSize;
        }

        public virtual void UpdateConstraints()
        {
            RecalculatePosAndSizeConstraints();

            UpdateChildrenConstraints();
        }

        public void UpdateChildrenConstraints()
        {
            foreach (UIElement element in children)
                element.UpdateConstraints();
        }
    }

    public class UIContext : UIElement
    {
        private RenderScreen screen;

        public override Vector2 Position 
        { 
            get { return Vector2.Zero; } 
            set { } 
        }

        public override Vector2 Size 
        { 
            get { return screen.Size; }
            set { } 
        }

        public UIContext(RenderScreen screen) : base(null)
        {
            this.screen   = screen;
            this.position = Vector2.Zero;
            this.size     = screen.Size;
        }
    }

    public class UIRectangle : UIElement
    {
        public UIRectangle(Constraints constraints, Color color) : 
            base(constraints)
        {
            this.Color = color;
        }

        public override void Draw(SpriteBatch spriteBatch) 
        {
            spriteBatch.DrawRectangle(position, size, Color);

            base.Draw(spriteBatch);
        }
    }

    public class UIImage : UIElement
    {
        public Texture2D Texture;
        private Vector2 scale;
        private Rectangle source;

        public Rectangle Source 
        { 
            get { return source; } 
            set 
            { 
                source = value;
                RecalculateScale();
            }
        }

        public override Vector2 Size 
        {
            get { return size; } 
            set
            {
                base.Size = value;
                RecalculateScale();
            }
        }

        public UIImage(Constraints constraints, Texture2D texture, Rectangle source) : 
            base(constraints) 
        {
            this.Texture = texture;
            this.source  = source;
            this.scale   = Vector2.One;
        }

        private void RecalculateScale()
        {
            scale.X = (source.Width  != 0) ? size.X / source.Width  : 0.0f;
            scale.Y = (source.Height != 0) ? size.Y / source.Height : 0.0f;
        }

        public override void UpdateConstraints()
        {
            base.UpdateConstraints();
            RecalculateScale();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Texture, position, source, Color, 0.0f, 
                Vector2.Zero, scale, SpriteEffects.None, 0.0f);
        }
    }

    public class UIString : UIElement
    {
        private SpriteFont font;
        private Vector2 scale;
        private string text;

        public string Text
        {
            get { return text; }
            set
            {
                text = value;
                UpdateConstraints();
                RecalculateScale();
            }
        }

        public SpriteFont Font
        {
            get { return font; }
            set
            {
                font = value;
                UpdateConstraints();
                RecalculateScale();
            }
        }

        public override Vector2 Size
        {
            get { return size; }
            set
            {
                base.Size = value;
                RecalculateScale();
            }
        }

        public UIString(Constraints constraints, SpriteFont font, string text, Color color) : 
            base(constraints)
        {
            this.font  = font;
            this.text  = text;
            this.scale = Vector2.One;
            this.Color = color;
        }

        private void RecalculateScale()
        {
            Vector2 textSize = font.MeasureString(text);

            scale.X = (textSize.X != 0) ? size.X / textSize.X : 0.0f;
            scale.Y = (textSize.Y != 0) ? size.Y / textSize.Y : 0.0f;
        }

        public override void UpdateConstraints()
        {
            base.UpdateConstraints();
            RecalculateScale();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.DrawString(font, text, position, Color,
                0.0f, Vector2.Zero, scale, SpriteEffects.None, 0.0f);

            base.Draw(spriteBatch);
        }
    }   
}
