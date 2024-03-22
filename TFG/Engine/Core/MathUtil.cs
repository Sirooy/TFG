using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

namespace Engine.Core
{
    public static class MathUtil
    {
        public const float PI        = MathF.PI;
        public const float PI2       = PI * 2.0f;
        public const float PI_OVER_2 = PI / 2.0f;
        public const float PI_OVER_4 = PI / 4.0f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Cross2D(this Vector2 v1, Vector2 v2)
        {
            return v1.X * v2.Y - v1.Y * v2.X;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Rotate90(this Vector2 v)
        {
            return new Vector2(-v.Y, v.X);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Rotate180(this Vector2 v)
        {
            return new Vector2(-v.X, -v.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Rotate(this Vector2 v, float angle)
        {
            float cos = MathF.Cos(angle);
            float sin = MathF.Sin(angle);

            return new Vector2(v.X * cos - v.Y * sin,
                               v.X * sin + v.Y * cos);
        }
    }
}
