using System;
using Core;
using Cmps;
using Microsoft.Xna.Framework;

namespace Physics
{
    public struct Manifold
    {
        public Vector2 Normal;
        public Vector2 Contact1;
        public Vector2 Contact2;
        public int NumContacts;
        public float Depth;

        public Manifold()
        {
            Normal      = Vector2.Zero;
            Contact1    = Vector2.Zero;
            Contact2    = Vector2.Zero;
            NumContacts = 0;
            Depth       = float.MaxValue;
        }
    }
}
