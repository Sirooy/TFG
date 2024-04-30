using System;
using Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Cmps
{
    [Flags]
    public enum CharacterType : uint
    {
        None       = 0 << 0,
        Player     = 1 << 1,
        Enemy      = 1 << 2,
        Normal     = 1 << 3,
        Mage       = 1 << 4,
        Warrior    = 1 << 5,
        Ranger     = 1 << 6,
        Paladin    = 1 << 7,
        AllClasses = Normal | Mage | Warrior | Ranger | Paladin,
        AllTypes   = 0xFFFFFFFF
    }

    public enum SelectState
    {
        None = 0,
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
        public CharacterType Type;
        public CharacterType CanUseSkillsOfType;

        public CharacterCmp(Texture2D texture, CharacterType type, 
            CharacterType skills)
        {
            this.PlatformTexture    = texture;
            this.PlatformSourceRect = new Rectangle(0, 0, 0, 0);
            this.SelectSourceRect   = new Rectangle(0, 0, 0, 0);
            this.SelectState        = SelectState.None;
            this.Color              = Color.White;
            this.Type               = type;
            this.CanUseSkillsOfType = skills;
        }
    }
}
