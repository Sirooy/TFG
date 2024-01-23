using System;
using Microsoft.Xna.Framework;

namespace Engine.Core
{
    public struct AABB
    {
        public float Left;
        public float Right;
        public float Top;
        public float Bottom;

        public float Width      { get { return Right - Left; } }
        public float Height     { get { return Bottom - Top; } }
        public Vector2 Position { get {  return new  Vector2(Left, Top); } }

        public AABB(float left, float right, float top, float bottom)
        {
            Left   = left;
            Right  = right;
            Top    = top;
            Bottom = bottom;
        }

        public AABB(Vector2 position, float width, float height)
        {
            Left   = position.X;
            Right  = position.X + width;
            Top    = position.Y;
            Bottom = position.Y + height;
        }

        public AABB(Vector2 position, Vector2 size)
        {
            Left   = position.X;
            Right  = position.X + size.X;
            Top    = position.Y;
            Bottom = position.Y + size.Y;
        }

        public bool Contains(float x, float y) 
        {
            return x >= Left && x <= Right &&
                   y >= Top && y <= Bottom;
        }

        public bool Contains(Vector2 point)
        {
            return point.X >= Left && point.X <= Right &&
                   point.Y >= Top && point.Y <= Bottom;
        }

        public bool Contains(Vector2 position, float width, float height)
        {
            return Right >= position.X && Left <= position.X + width &&
                   Bottom >= position.Y && Top <= position.Y + height;
        }

        public bool Contains(Vector2 position, Vector2 size)
        {
            return Right  >= position.X && Left <= position.X + size.X &&
                   Bottom >= position.Y && Top <= position.Y + size.Y;
        }

        public bool Contains(in AABB other)
        {
            return Right >= other.Left && Left <= other.Right &&
                   Bottom >= other.Top && Top <= other.Bottom;
        }
    }
}
