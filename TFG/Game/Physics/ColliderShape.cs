using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cmps;
using Core;
using Microsoft.Xna.Framework;

namespace Physics
{
    public enum ColliderShapeType
    {
        Circle = 0,
        Rectangle = 1,
        MaxTypes
    }

    public class ColliderShape
    {
        private ColliderShapeType type;

        public ColliderShapeType Type { get { return type; } }

        protected ColliderShape(ColliderShapeType type)
        {
            this.type = type;
        }
    }

    public class CircleCollider : ColliderShape
    {
        public float Radius;

        public CircleCollider(float radius) : 
            base(ColliderShapeType.Circle)
        {
            this.Radius = radius;
        }
    }

    public class RectangleCollider : ColliderShape
    {
        public float Width;
        public float Height;
        public Vector2[] Vertices { get; private set; }

        public RectangleCollider(float width, float height) : 
            base(ColliderShapeType.Rectangle)
        {
            this.Width    = width;
            this.Height   = height;
            this.Vertices = new Vector2[4];
        }

        public Vector2[] GetVertices(Entity entity, CollisionCmp cmp)
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
    }
}
