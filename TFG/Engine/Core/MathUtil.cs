﻿using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

namespace Engine.Core
{
    public static class MathUtil
    {
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
    }
}
