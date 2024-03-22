using System;
using System.Collections.Generic;

namespace Core
{
    public class Dice
    {
        private List<DiceFace> faces;

        public Dice()
        {
            faces = new List<DiceFace>();
        }

        public Dice(List<DiceFace> faces)
        {
            this.faces = faces;
        }

        public DiceFace Roll()
        {
            int index = Random.Shared.Next(faces.Count);

            return faces[index];
        }
    }
}
