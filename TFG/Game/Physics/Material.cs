using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Physics
{
    public struct Material
    {
        public float Restitution;
        public float StaticFriction;
        public float DynamicFriction;

        public Material(float restitution, float staticFriction, 
            float dynamicFriction)
        {
            this.Restitution     = restitution;
            this.StaticFriction  = staticFriction;
            this.DynamicFriction = dynamicFriction;
        }
    }
}
