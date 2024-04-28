using Engine.Core;
using Microsoft.Xna.Framework;
using System;

namespace UI
{
    public abstract class UIEventHandler
    {
        public abstract void HandleEvents(UIElement element);
    }

    public class UIHoverEventHandler : UIEventHandler
    {
        public delegate void HoverDelegate(UIElement element);

        public event HoverDelegate OnEnterHover;
        public event HoverDelegate OnExitHover;
        public event HoverDelegate OnHover;

        protected bool mouseIsOver;

        public UIHoverEventHandler()
        {
            mouseIsOver = false;
        }

        public override void HandleEvents(UIElement element)
        {
            Vector2 pos      = element.Position;
            Vector2 size     = element.Size;
            Vector2 mousePos = MouseInput.GetPosition(element.Context.Screen);

            if(mousePos.X >= pos.X && mousePos.X <= pos.X + size.X &&
                mousePos.Y >= pos.Y && mousePos.Y <= pos.Y + size.Y)
            {
                if(mouseIsOver)
                {
                    OnHover?.Invoke(element);
                }
                else
                {
                    mouseIsOver = true;
                    OnEnterHover?.Invoke(element);
                }
            }
            else
            {
                if(mouseIsOver)
                {
                    mouseIsOver = false;
                    OnExitHover?.Invoke(element);
                }
            }
        }
    }

    public class UIButtonEventHandler : UIHoverEventHandler
    {
        protected enum State
        {
            Default = 0,
            Pressed,
            Held,
            Released
        }

        public delegate void ButtonDelegate(UIElement element);

        public event ButtonDelegate OnPress;
        public event ButtonDelegate OnHold;
        public event ButtonDelegate OnRelease;

        protected State state;

        public UIButtonEventHandler()
        {
            state = State.Default;
        }

        public override void HandleEvents(UIElement element) 
        {
            base.HandleEvents(element);

            if (mouseIsOver && MouseInput.IsLeftButtonPressed())
            {
                state = State.Pressed;
                OnPress?.Invoke(element);
            }
            else if ((state == State.Pressed || state == State.Held) &&
                MouseInput.IsLeftButtonDown())
            {
                state = State.Held;
                OnHold?.Invoke(element);
            }
            else if ((state == State.Pressed || state == State.Held) &&
                MouseInput.IsLeftButtonReleased())
            {
                state = State.Released;
                OnRelease?.Invoke(element);
            }
            else
                state = State.Default;
        }
    }

    public class UICheckboxEventHandler : UIButtonEventHandler
    {
        public delegate void CheckboxDelegate(UIElement element, bool value);

        public event CheckboxDelegate OnValueChange;

        protected bool value;

        public UICheckboxEventHandler(bool value)
        {
            this.value = value;
        }

        public override void HandleEvents(UIElement element)
        {
            base.HandleEvents(element);

            if (mouseIsOver && MouseInput.IsLeftButtonPressed())
            {
                value = !value;
                OnValueChange?.Invoke(element, value);
            }
        }
    }
}
