using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TFG;

namespace Cmps
{
    public class SpriteCmp
    {
        public EntityTransformChild Transform;
        public Texture2D Texture;
        public Rectangle? SourceRect;
        public Vector2 Origin;
        public Color Color;
        public float LayerDepth;

        public SpriteCmp(Texture2D texture, Rectangle? source = null)
        {
            this.Transform  = new EntityTransformChild();
            this.Texture    = texture;
            this.SourceRect = source;
            this.Origin     = Vector2.Zero;
            this.Color      = Color.White;
            this.LayerDepth = 0.0f;
        }
    }
}
