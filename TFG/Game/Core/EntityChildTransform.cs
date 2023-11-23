using System;
using Microsoft.Xna.Framework;

namespace Core
{
    public struct EntityChildTransform
    {
        public Vector2 LocalPosition;
        public float LocalRotation;
        public float LocalScale;

        public EntityChildTransform()
        {
            LocalPosition = Vector2.Zero;
            LocalRotation = 0.0f;
            LocalScale    = 1.0f;
        }

        public Vector2 GetWorldPosition(Entity entity)
        {
            float cos = MathF.Cos(entity.Rotation);
            float sin = MathF.Sin(entity.Rotation);
            float x = entity.Scale * LocalPosition.X;
            float y = entity.Scale * LocalPosition.Y;

            return new Vector2(
                (x * cos - y * sin) + entity.Position.X,
                (x * sin + y * cos) + entity.Position.Y);
        }

        public float GetWorldRotation(Entity entity)
        {
            return LocalRotation + entity.Rotation;
        }

        public float GetWorldScale(Entity entity)
        {
            return LocalScale * entity.Scale;
        }
    }
}
