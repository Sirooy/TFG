using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cmps;
using Core;
using Microsoft.Xna.Framework;

namespace Physics
{
    public struct AABB
    {
        public float Left;
        public float Right;
        public float Top;
        public float Bottom;

        public float Width  { get { return Right - Left; } }
        public float Height { get { return Bottom - Top; } }

        public AABB(float left, float right, float top, float bottom)
        {
            Left   = left;
            Right  = right;
            Top    = top;
            Bottom = bottom;
        }
    }

    public enum ColliderShapeType
    {
        Circle = 0,
        Rectangle = 1,
        MaxTypes
    }

    public abstract class ColliderShape
    {
        protected ColliderShapeType type;
        protected AABB boundingAABB;

        public ColliderShapeType Type { get { return type; } }
        public AABB BoundingAABB { get { return boundingAABB; } }

        protected ColliderShape(ColliderShapeType type)
        {
            this.type         = type;
            this.boundingAABB = default;
        }

        public abstract void RecalculateBoundingAABBAndTransform(
            in EntityChildTransform parent);
    }

    public class CircleCollider : ColliderShape
    {
        public float Radius;
        public float CachedRadius;

        public CircleCollider(float radius) : 
            base(ColliderShapeType.Circle)
        {
            this.Radius = radius;
        }

        public override void RecalculateBoundingAABBAndTransform(
            in EntityChildTransform parent)
        {
            CachedRadius = parent.CachedWorldScale * Radius;

            Vector2 center = parent.CachedWorldPosition;
            boundingAABB   = new AABB(
                center.X - CachedRadius, center.X + CachedRadius,
                center.Y - CachedRadius, center.Y + CachedRadius);
        }
    }

    public class RectangleCollider : ColliderShape
    {
        public float Width;
        public float Height;
        public Vector2[] Vertices { get; private set; }
        public Vector2[] Normals { get; private set; }

        public RectangleCollider(float width, float height) : 
            base(ColliderShapeType.Rectangle)
        {
            this.Width    = width;
            this.Height   = height;
            this.Vertices = new Vector2[4];
            this.Normals  = new Vector2[2];
        }

        public RectangleCollider(float size) : this(size, size)
            { }

        public Vector2[] GetVertices(Entity entity, ColliderCmp cmp)
        {
            Vector2 center = cmp.Transform.GetWorldPosition(entity);
            float rotation = cmp.Transform.GetWorldRotation(entity);
            float scale    = cmp.Transform.GetWorldScale(entity);

            float cos = MathF.Cos(rotation);
            float sin = MathF.Sin(rotation);

            Vector2 right = new Vector2(cos * Width * scale * 0.5f, 
                sin * Width * scale * 0.5f);
            Vector2 down = new Vector2(-sin * Height * scale * 0.5f,
                cos * Height * scale * 0.5f);

            Vertices[0] = center - right - down; //Left Up
            Vertices[1] = center - right + down; //Left Down
            Vertices[2] = center + right + down; //Right Down
            Vertices[3] = center + right - down; //Right Up

            return Vertices;
        }

        public override void RecalculateBoundingAABBAndTransform(
            in EntityChildTransform parent)
        {
            Vector2 center = parent.CachedWorldPosition;
            float rotation = parent.CachedWorldRotation;
            float scale    = parent.CachedWorldScale;

            float cos = MathF.Cos(rotation);
            float sin = MathF.Sin(rotation);

            Vector2 right = new Vector2(cos, sin);
            Vector2 down  = new Vector2(-sin, cos);
            Normals[0]    = right;
            Normals[1]    = down;
            right        *= Width * scale * 0.5f;
            down         *= Height * scale * 0.5f;

            Vertices[0] = center - right - down; //Left Up
            Vertices[1] = center - right + down; //Left Down
            Vertices[2] = center + right + down; //Right Down
            Vertices[3] = center + right - down; //Right Up

            //The half width and half height of the AABB is the absolute 
            //projection of the scaled normals with the world axles

            //The dot product is not necessary because the projection into the 
            //world axles is just the x,y components
            float hw = MathF.Abs(right.X) + MathF.Abs(down.X);
            float hh = MathF.Abs(right.Y) + MathF.Abs(down.Y);
            boundingAABB = new AABB(
                center.X - hw, center.X + hw,
                center.Y - hh, center.Y + hh);
        }
    }
}
