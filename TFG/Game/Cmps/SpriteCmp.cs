using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Core;

namespace Cmps
{
    public enum LayerOrder
    {
        AlwaysBottom = 0,
        AlwaysTop,
        Ordered
    };

    public class SpriteCmp
    {
        public EntityChildTransform Transform;
        public Texture2D Texture;
        public Rectangle? SourceRect;
        public Vector2 Origin;
        public Color Color;
        public SpriteEffects Effects;
        private LayerOrder layerOrder;
        public float LayerDepth;

        public LayerOrder LayerOrder 
        { 
            get 
            { 
                return layerOrder; 
            }
            set
            {
                layerOrder = value;
                switch(layerOrder)
                {
                    case LayerOrder.AlwaysBottom: LayerDepth = 0.0f; break;
                    case LayerOrder.AlwaysTop:    LayerDepth = 1.0f; break;
                }
            }
        }


        public SpriteCmp(Texture2D texture, Rectangle? source = null)
        {
            this.Transform  = new EntityChildTransform();
            this.Texture    = texture;
            this.SourceRect = source;
            this.Origin     = Vector2.Zero;
            this.Color      = Color.White;
            this.Effects    = SpriteEffects.None;
            this.layerOrder = LayerOrder.AlwaysBottom;
            this.LayerDepth = 0.0f;
        }
    }
}
