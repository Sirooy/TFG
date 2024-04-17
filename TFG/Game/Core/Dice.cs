using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace Core
{
    public class Dice
    {
        public Color Color;
        private List<DiceFace> faces;

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

        public List<DiceFace> Faces { get { return faces; } } 

        public Dice(Color color)
        {
            this.faces = new List<DiceFace>();
            this.Color = color;
        }

        public Dice(List<DiceFace> faces, Color color)
        {
            this.faces = faces;
            this.Color = color;
        }

        public DiceFace Roll()
        {
            int index = Random.Shared.Next(faces.Count);

            return faces[index];
        }
    }
}
