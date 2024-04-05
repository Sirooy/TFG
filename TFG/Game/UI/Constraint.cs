using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI
{
    public class Constraints
    {
        public IPositionConstraint XConstraint;
        public IPositionConstraint YConstraint;
        public ISizeConstraint WidthConstraint;
        public ISizeConstraint HeightConstraint;

        public Constraints(
            IPositionConstraint xConstraint = null,
            IPositionConstraint yConstraint = null,
            ISizeConstraint widthConstraint = null,
            ISizeConstraint heightConstraint = null)
        {
            XConstraint = xConstraint;
            YConstraint = yConstraint;
            WidthConstraint = widthConstraint;
            HeightConstraint = heightConstraint;
        }
    }

    public interface IPositionConstraint
    {
        public abstract float GetXValue(UIElement element);
        public abstract float GetYValue(UIElement element);
    }

    public interface ISizeConstraint
    {
        public abstract float GetXValue(UIElement element);
        public abstract float GetYValue(UIElement element);
    }

    public class PercentConstraint : IPositionConstraint, ISizeConstraint
    {
        private float percentage;

        public PercentConstraint(float percentage)
        {
            this.percentage = percentage;
        }

        float IPositionConstraint.GetXValue(UIElement element)
        {
            return element.Parent.Position.X + element.Parent.Size.X * percentage;
        }

        float IPositionConstraint.GetYValue(UIElement element)
        {
            return element.Parent.Position.Y + element.Parent.Size.Y * percentage;
        }

        float ISizeConstraint.GetXValue(UIElement element)
        {
            return element.Parent.Size.X * percentage;
        }

        float ISizeConstraint.GetYValue(UIElement element)
        {
            return element.Parent.Size.Y * percentage;
        }
    }

    public class PixelConstraint : IPositionConstraint, ISizeConstraint
    {
        private float pixels;

        public PixelConstraint(float pixels)
        {
            this.pixels = pixels;
        }

        float IPositionConstraint.GetXValue(UIElement element)
        {
            return element.Parent.Position.X + pixels;
        }

        float IPositionConstraint.GetYValue(UIElement element)
        {
            return element.Parent.Position.Y + pixels;
        }

        float ISizeConstraint.GetXValue(UIElement element)
        {
            return pixels;
        }

        float ISizeConstraint.GetYValue(UIElement element)
        {
            return pixels;
        }
    }

    public class CenterConstraint : IPositionConstraint
    {
        float IPositionConstraint.GetXValue(UIElement element)
        {
            float elementHalfWidth = element.Size.X * 0.5f;
            float parentHalfWidth  = element.Parent.Size.X * 0.5f;
            float parentXPos       = element.Parent.Position.X;

            if(element.WidthConstraint != null)
                elementHalfWidth = element.WidthConstraint.GetXValue(element) * 0.5f;

            return parentXPos + parentHalfWidth - elementHalfWidth;
        }

        float IPositionConstraint.GetYValue(UIElement element)
        {
            float elementHalfHeight = element.Size.Y * 0.5f;
            float parentHalfHeight  = element.Parent.Size.Y * 0.5f;
            float parentYPos        = element.Parent.Position.Y;

            if (element.HeightConstraint != null)
                elementHalfHeight = element.HeightConstraint.GetYValue(element) * 0.5f;

            return parentYPos + parentHalfHeight - elementHalfHeight;
        }
    }

    public class AspectConstraint : ISizeConstraint
    {
        private float aspectRatio;

        public AspectConstraint(float aspectRatio)
        {
            this.aspectRatio = aspectRatio;
        }

        float ISizeConstraint.GetXValue(UIElement element)
        {
            float height = element.Size.Y;
            if (element.HeightConstraint != null)
                height = element.HeightConstraint.GetYValue(element);

            return height * aspectRatio;
        }

        float ISizeConstraint.GetYValue(UIElement element)
        {
            float width = element.Size.X;
            if (element.WidthConstraint != null)
                width = element.WidthConstraint.GetXValue(element);

            return width * aspectRatio;
        }
    }
}
