using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace Core
{
    public class Dice
    {
        public Color Color;
        private List<PlayerSkill> faces;

        public Rectangle SourceRect
        {
            get
            {
                switch(faces.Count)
                {
                    case 4: return new Rectangle(32, 0, 32, 32);
                    case 5: return new Rectangle(64, 0, 32, 32);
                    case 6: return new Rectangle(96, 0, 32, 32);
                    default: return new Rectangle(96, 0, 32, 32);
                }
            }
        }

        public List<PlayerSkill> Faces { get { return faces; } } 

        public Dice(Color color)
        {
            this.faces = new List<PlayerSkill>();
            this.Color = color;
        }

        public Dice(List<PlayerSkill> faces, Color color)
        {
            this.faces = faces;
            this.Color = color;
        }

        public PlayerSkill Roll()
        {
            int index = Random.Shared.Next(faces.Count);

            return faces[index];
        }
    }
}
