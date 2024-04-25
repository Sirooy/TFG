using System;
using Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Cmps
{
    public enum SelectState
    {
        None = 0,
        Enabled,
        Hover,
        Selected
    }


    public class CharacterCmp
    {
        public Texture2D PlatformTexture;
        public Rectangle PlatformSourceRect;
        public Rectangle SelectSourceRect;
        public SelectState SelectState;
        public Color Color;

        public CharacterCmp(Texture2D texture)
        {
            this.PlatformTexture    = texture;
            this.PlatformSourceRect = new Rectangle(0, 0, 0, 0);
            this.SelectSourceRect   = new Rectangle(0, 0, 0, 0);
            this.SelectState        = SelectState.None;
            this.Color              = Color.White;
        }
    }
}
