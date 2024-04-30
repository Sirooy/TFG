using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Engine.Core;
using Engine.Debug;
using Engine.Graphics;
using Core;

namespace UI
{
    public enum LayoutType
    {
        Horizontal,
        Vertical
    }

    public enum LayoutAlign
    {
        Start,
        Center,
        End
    }

    public class UIElement
    {
        public const string DEBUG_DRAW_LAYER = "UI";

        protected Vector2 position;
        protected Vector2 size;
        protected Constraints constraints;
        protected List<UIElement> children;
        protected bool isVisible;
        protected bool isEnabled;
        public Color Color;

        public UIContext Context { get; internal set; }
        public UIElement Parent  { get; internal set; }
        public UIEventHandler EventHandler { get; set; }
        public IPositionConstraint XConstraint  { get { return constraints.XConstraint; } }
        public IPositionConstraint YConstraint  { get { return constraints.YConstraint; } }
        public ISizeConstraint WidthConstraint  { get { return constraints.WidthConstraint; } }
        public ISizeConstraint HeightConstraint { get { return constraints.HeightConstraint; } }
        
        public virtual bool OverridesChildXConstraint      { get { return false; } }
        public virtual bool OverridesChildYConstraint      { get { return false; } }
        public virtual bool OverridesChildWidthConstraint  { get { return false; } }
        public virtual bool OverridesChildHeightConstraint { get { return false; } }

        public int ChildrenCount { get { return children.Count; } } 

        public bool IsVisible
        {
            get { return isVisible; }
            set
            {
                if (isVisible == value) return;

                isVisible = value;
                if (value == false && EventHandler != null)
                    EventHandler.OnDisable(this);

                //foreach (UIElement child in children)
                //    child.IsVisible = value;
            }
        }

        public bool IsEnabled
        {
            get { return isEnabled; }
            set
            {
                if (isEnabled == value) return;

                isEnabled = value;
                if (value == false && EventHandler != null)
                    EventHandler.OnDisable(this);

                //foreach (UIElement child in children)
                //    child.IsEnabled = value;
            }
        }

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

        public virtual Vector2 BaseSize
        {
            get { return Vector2.One; }
        }

        public UIElement(UIContext context, Constraints constraints)
        {
            this.constraints = constraints;
            this.Context     = context;
            this.children    = new List<UIElement>();
            this.Color       = Color.Black;
            this.IsVisible   = true;
            this.IsEnabled   = true;
        }

        public virtual void Update() 
        {
            if (!IsVisible || !IsEnabled) return;

            EventHandler?.HandleEvents(this);

            foreach (UIElement element in children)
            {
                if(element.IsVisible && element.IsEnabled)
                    element.Update();
            }
        }
        public virtual void Draw(SpriteBatch spriteBatch) 
        {
            DebugDraw.Rect(DEBUG_DRAW_LAYER, position, size,
                new Color((byte)(byte.MaxValue - Color.R),
                          (byte)(byte.MaxValue - Color.G),
                          (byte)(byte.MaxValue - Color.B)));

            foreach (UIElement element in children)
            {
                if (element.IsVisible)
                    element.Draw(spriteBatch);
            }
        }

        public virtual void AddElement(UIElement element, string name = "")
        {
            children.Add(element);
            element.Parent = this;
            element.UpdateConstraints();
            Context.RegisterElement(element, name);
        }

        public virtual UIElement RemoveElement(UIElement element)
        {
            if (children.Remove(element))
            {
                element.Parent = null;

                return element;
            }

            return null;
        }

        public virtual void RemoveElementRange(int startIndex, int count)
        {
            children.RemoveRange(startIndex, count);
        }

        public void ClearElements()
        {
            children.Clear();
        }

        public UIElement GetElement(int index)
        {
            return children[index];
        }

        public T GetElement<T>(int index) where T : UIElement
        {
            return (T) children[index];
        }

        public int GetElementIndex(UIElement element)
        {
            return children.FindIndex((UIElement e) => e == element);
        }

        protected void RecalculatePosAndSizeConstraints()
        {
            Vector2 newPos  = position;
            Vector2 newSize = size;

            if (constraints.XConstraint != null && 
                !Parent.OverridesChildXConstraint)
                newPos.X = constraints.XConstraint.GetXValue(this);

            if (constraints.YConstraint != null && 
                !Parent.OverridesChildYConstraint)
                newPos.Y = constraints.YConstraint.GetYValue(this);

            if (constraints.WidthConstraint != null && 
                !Parent.OverridesChildWidthConstraint)
                newSize.X = constraints.WidthConstraint.GetXValue(this);

            if (constraints.HeightConstraint != null && 
                !Parent.OverridesChildHeightConstraint)
                newSize.Y = constraints.HeightConstraint.GetYValue(this);

            position = newPos;
            size     = newSize;
        }

        protected virtual void UpdateConstraints()
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
        private Dictionary<string, UIElement> elements;

        public RenderScreen Screen { get { return screen; } }


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

        public override Vector2 BaseSize
        {
            get { return screen.Size; }
        }

        public UIContext(RenderScreen screen) : base(null, null)
        {
            this.screen   = screen;
            this.elements = new Dictionary<string, UIElement>();
            this.position = Vector2.Zero;
            this.size     = screen.Size;
            this.Context  = this;
        }

        internal void RegisterElement(UIElement element, string name)
        {
            if (name == "") return;

            DebugAssert.Success(!elements.ContainsKey(name),
                "UI Context already contains an element with the name \"{0}\"", name);
            elements[name] = element;
        }

        public T GetElement<T>(string name) where T : UIElement
        {
            DebugAssert.Success(elements.ContainsKey(name),
                "UI Context does not contain an element with the name \"{0}\"", name);

            return (T) elements[name];
        }
    }

    public class UIRectangle : UIElement
    {
        public UIRectangle(UIContext context, Constraints constraints, 
            Color color) : base(context, constraints)
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
                UpdateConstraints();
            }
        }

        public override Vector2 Size 
        {
            get { return size; } 
            set
            {
                base.Size = value;
                UpdateConstraints();
            }
        }

        public override Vector2 BaseSize
        {
            get { return new Vector2(source.Width, source.Height); }
        }

        public UIImage(UIContext context, Constraints constraints, 
            Texture2D texture, Rectangle source) : base(context, constraints) 
        {
            this.Texture = texture;
            this.source  = source;
            this.scale   = Vector2.One;
            this.Color   = Color.White;
        }

        private void RecalculateScale()
        {
            scale.X = (source.Width  != 0) ? size.X / source.Width  : 0.0f;
            scale.Y = (source.Height != 0) ? size.Y / source.Height : 0.0f;
        }

        protected override void UpdateConstraints()
        {
            base.UpdateConstraints();
            RecalculateScale();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Texture, position, source, Color, 0.0f, 
                Vector2.Zero, scale, SpriteEffects.None, 0.0f);

            base.Draw(spriteBatch);
        }
    }

    public class UIString : UIElement
    {
        private SpriteFont font;
        private Vector2 textSize;
        private Vector2 scale;
        private string text;

        public string Text
        {
            get { return text; }
            set
            {
                text = value;
                UpdateConstraints();
            }
        }

        public SpriteFont Font
        {
            get { return font; }
            set
            {
                font = value;
                UpdateConstraints();
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

        public override Vector2 BaseSize
        {
            get { return textSize; }
        }

        public UIString(UIContext context, Constraints constraints, 
            SpriteFont font, string text, Color color) : base(context, constraints)
        {
            this.font     = font;
            this.text     = text;
            this.scale    = Vector2.One;
            this.Color    = color;
            this.textSize = font.MeasureString(text);
        }

        private void RecalculateScale()
        {
            textSize = font.MeasureString(text);

            scale.X = (textSize.X != 0) ? size.X / textSize.X : 0.0f;
            scale.Y = (textSize.Y != 0) ? size.Y / textSize.Y : 0.0f;
        }

        protected override void UpdateConstraints()
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

    public class UILayout : UIElement
    {
        private LayoutType layoutType;
        private LayoutAlign layoutAlign;
        private ISizeConstraint marginConstraint;

        public LayoutType Type
        {
            get { return layoutType; }
            set 
            {
                layoutType = value;

                UpdateChildrenConstraints();
                UpdateChildrenPositions();
            }
        }

        public LayoutAlign Align
        {
            get { return layoutAlign; }
            set
            {
                layoutAlign = value;

                UpdateChildrenPositions();
            }
        }

        public ISizeConstraint Margin
        {
            get { return marginConstraint; }
            set
            {
                marginConstraint = value;

                UpdateChildrenPositions();
            }
        }

        public override Vector2 Position
        {
            get { return position; }
            set
            {
                position = value;

                UpdateChildrenConstraints();
                UpdateChildrenPositions();
            }
        }

        public override Vector2 Size
        {
            get { return size; }
            set
            {
                size = value;

                UpdateChildrenConstraints();
                UpdateChildrenPositions();
            }
        }

        public override bool OverridesChildXConstraint 
            { get { return layoutType == LayoutType.Horizontal; } }
        public override bool OverridesChildYConstraint 
            { get { return layoutType == LayoutType.Vertical; } }

        public bool DrawRectangle { get; set; }

        public UILayout(UIContext context, Constraints constraints, 
            LayoutType layoutType, LayoutAlign layoutAlign, 
            ISizeConstraint marginConstraint = null) : 
            base(context, constraints)
        {
            this.layoutType       = layoutType;
            this.layoutAlign      = layoutAlign;
            this.marginConstraint = marginConstraint;
            this.DrawRectangle    = false;
        }

        public UILayout(UIContext context, Constraints constraints,
            Color color, LayoutType layoutType, LayoutAlign layoutAlign,
            ISizeConstraint marginConstraint = null) :
            base(context, constraints)
        {
            this.Color            = color;
            this.layoutType       = layoutType;
            this.layoutAlign      = layoutAlign;
            this.marginConstraint = marginConstraint;
            this.DrawRectangle    = true;
        }

        public override void AddElement(UIElement element, string name = "")
        {
            base.AddElement(element, name);
            UpdateChildrenPositions();
        }

        public override UIElement RemoveElement(UIElement element)
        {
            UIElement removed = base.RemoveElement(element);
            if (removed != null)
                UpdateChildrenPositions();

            return removed;
        }

        public override void RemoveElementRange(int startIndex, int count)
        {
            base.RemoveElementRange(startIndex, count);
            UpdateChildrenPositions();
        }

        protected override void UpdateConstraints()
        {
            base.UpdateConstraints();
            UpdateChildrenPositions();
        }

        private void UpdateChildrenPositions()
        {
            float margin = 0.0f;

            if (layoutType == LayoutType.Horizontal)
            {
                if (marginConstraint != null)
                    margin = marginConstraint.GetXValue(this);

                float alignment = CalculateAlignmentPaddingX(margin);
                float currentX  = position.X + alignment;
                foreach (UIElement child in children)
                {
                    Vector2 childPos = child.Position;
                    childPos.X       = currentX;
                    child.Position   = childPos;

                    currentX        += child.Size.X + margin;
                }
            }
            else
            {
                if (marginConstraint != null)
                    margin = marginConstraint.GetYValue(this);

                float alignment = CalculateAlignmentPaddingY(margin);
                float currentY  = position.Y + alignment;
                foreach (UIElement child in children)
                {
                    Vector2 childPos = child.Position;
                    childPos.Y       = currentY;
                    child.Position   = childPos;

                    currentY        += child.Size.Y + margin;
                }
            }
        }

        private float CalculateChildrenTotalSizeX(float margin)
        {
            //Init the size at -margin to ignore the last element margin
            float totalSize = -margin;

            foreach (UIElement child in children)
                totalSize += child.Size.X + margin;

            return totalSize;
        }

        private float CalculateChildrenTotalSizeY(float margin)
        {
            //Init the size at -margin to ignore the last element margin
            float totalSize = -margin;

            foreach (UIElement child in children)
                totalSize += child.Size.Y + margin;

            return totalSize;
        }

        private float CalculateAlignmentPaddingX(float margin)
        {
            float totalSize = CalculateChildrenTotalSizeX(margin);

            switch(layoutAlign)
            {
                case LayoutAlign.Start:
                    return 0.0f;
                case LayoutAlign.Center:
                    return size.X * 0.5f - totalSize * 0.5f;
                case LayoutAlign.End:
                    return size.X - totalSize;
                default: return 0.0f;
            }
        }

        private float CalculateAlignmentPaddingY(float margin)
        {
            float totalSize = CalculateChildrenTotalSizeY(margin);

            switch (layoutAlign)
            {
                case LayoutAlign.Start:
                    return 0.0f;
                case LayoutAlign.Center:
                    return size.Y * 0.5f - totalSize * 0.5f;
                case LayoutAlign.End:
                    return size.Y - totalSize;
                default: return 0.0f;
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (DrawRectangle)
                spriteBatch.DrawRectangle(position, size, Color);

            base.Draw(spriteBatch);
        }
    }

    public class UICardLayout : UIElement
    {
        private enum State
        {
            HoveringCard,
            DraggingCard,
            DroppingCard
        }

        public delegate void DropDelegate(UIElement element);
        public event DropDelegate OnDropEvent;

        public override bool OverridesChildXConstraint
            { get { return true; } }

        public override Vector2 Position
        {
            get { return position; }
            set
            {
                position = value;

                UpdateChildrenConstraints();
                UpdateChildrenPositions();
            }
        }

        public override Vector2 Size
        {
            get { return size; }
            set
            {
                size = value;

                UpdateChildrenConstraints();
                UpdateChildrenPositions();
            }
        }

        private UIElement selectedElement;
        private UIElement lastSelectedElement;
        private State state;

        public UIElement SelectedElement 
            { get { return selectedElement; } }
        public bool SelectedElementChanged 
            { get { return selectedElement != lastSelectedElement; } }
        public bool ElementDropped       
            { get { return state == State.DroppingCard; } }

        public UICardLayout(UIContext context, Constraints constraints) : 
            base(context, constraints)
        {
            selectedElement     = null;
            lastSelectedElement = null;
            state               = State.HoveringCard;
        }

        public override void Update()
        {
            if (!IsVisible || !IsEnabled) return;

            switch(state)
            {
                case State.HoveringCard: UpdateHovering();
                    break;
                case State.DraggingCard: UpdateDragging();
                    break;
                case State.DroppingCard: UpdateDropping();
                    break;
            }

            base.Update();
        }

        private void UpdateHovering()
        {
            lastSelectedElement = selectedElement;
            selectedElement     = null;

            int index = children.Count - 1;
            while (index >= 0 && selectedElement == null)
            {
                UIElement child = children[index];
                Vector2 mousePos = MouseInput.GetPosition(Context.Screen);
                Vector2 childPos = child.Position;
                Vector2 childSize = child.Size;

                if (mousePos.X >= childPos.X && mousePos.X <= childPos.X + childSize.X &&
                    mousePos.Y >= childPos.Y && mousePos.Y <= childPos.Y + childSize.Y)
                {
                    selectedElement = child;
                }

                index--;
            }

            if (MouseInput.IsLeftButtonPressed() && selectedElement != null)
                state = State.DraggingCard;
        }

        private void UpdateDragging()
        {
            if (MouseInput.IsLeftButtonReleased())
            {
                state = State.DroppingCard;
                OnDropEvent?.Invoke(selectedElement);
            }
            else
                selectedElement.Position = MouseInput.GetPosition(Context.Screen);
        }

        private void UpdateDropping()
        {
            state           = State.HoveringCard;
            selectedElement = null;
            UpdateChildrenConstraints();
            UpdateChildrenPositions();
        }

        public override void AddElement(UIElement element, string name = "")
        {
            base.AddElement(element, name);
            UpdateChildrenPositions();
        }

        public override UIElement RemoveElement(UIElement element)
        {
            UIElement removed = base.RemoveElement(element);
            if (removed != null)
                UpdateChildrenPositions();

            if (selectedElement != null && removed == selectedElement)
                selectedElement = null;

            return removed;
        }

        public override void RemoveElementRange(int startIndex, int count)
        {
            if(selectedElement != null)
            {
                for (int i = 0; i < count; ++i)
                {
                    UIElement element = children[i + startIndex];
                    if (selectedElement == element)
                    {
                        selectedElement = null;
                        break;
                    }
                }
            }

            base.RemoveElementRange(startIndex, count);
            UpdateChildrenPositions();
        }
        protected override void UpdateConstraints()
        {
            base.UpdateConstraints();
            UpdateChildrenPositions();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if(selectedElement == null)
                base.Draw(spriteBatch);
            else
            {
                DebugDraw.Rect(DEBUG_DRAW_LAYER, position, size,
                    new Color((byte)(byte.MaxValue - Color.R),
                              (byte)(byte.MaxValue - Color.G),
                              (byte)(byte.MaxValue - Color.B)));

                foreach (UIElement child in children)
                {
                    if (child == selectedElement) continue;

                    child.Draw(spriteBatch);
                }

                selectedElement.Draw(spriteBatch);
            }
        }

        private void UpdateChildrenPositions()
        {
            float numChildren = children.Count;
            float separation  = size.X / (numChildren + 1.0f);
            float currentX    = position.X + separation;

            foreach(UIElement child in children) 
            {
                Vector2 childPos = child.Position;
                childPos.X       = currentX - child.Size.X * 0.5f;
                child.Position   = childPos;

                currentX        += separation;
            }
        }
    }
}
