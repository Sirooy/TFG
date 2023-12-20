using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Physics
{
    public struct Material
    {
        public static readonly Material Zero     = new Material(0.0f, 0.0f, 0.0f);
        public static readonly Material One      = new Material(1.0f, 1.0f, 1.0f);
        public static readonly Material Rock     = new Material(0.1f, 0.85f, 0.7f);
        public static readonly Material Wood     = new Material(0.2f, 0.7f, 0.6f);
        public static readonly Material Metal    = new Material(0.05f, 0.6f, 0.5f);
        public static readonly Material Rubber   = new Material(0.8f, 0.2f, 0.1f);
        public static readonly Material Pillow   = new Material(0.3f, 0.3f, 0.2f);
        public static readonly Material Friction = new Material(0.0f, 1.0f, 1.0f);

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
