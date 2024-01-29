using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Core;

namespace Cmps
{
    public class SpriteCmp
    {
        public EntityChildTransform Transform;
        public Texture2D Texture;
        public Rectangle? SourceRect;
        public Vector2 Origin;
        public Color Color;
        public SpriteEffects Effects;

        public SpriteCmp(Texture2D texture, Rectangle? source = null)
        {
            this.Transform  = new EntityChildTransform();
            this.Texture    = texture;
            this.SourceRect = source;
            this.Origin     = Vector2.Zero;
            this.Color      = Color.White;
            this.Effects    = SpriteEffects.None;
        }
    }
}
