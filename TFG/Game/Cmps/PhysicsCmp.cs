using Microsoft.Xna.Framework;
using System;

namespace Cmps
{
    public class PhysicsCmp
    {
        public Vector2 Force;
        public Vector2 LinearVelocity;
        public float GravityMultiplier;
        //Añadir la restitucion al componente de colision
        public float Restitution;

        private Vector2 maxLinearVelocity;
        private float mass;
        private float inverseMass;
        

        public Vector2 MaxLinearVelocity
        {
            get { return maxLinearVelocity; }
            set
            {
                maxLinearVelocity.X = MathF.Abs(value.X);
                maxLinearVelocity.Y = MathF.Abs(value.Y);
            }
        }

        public float Mass
        {
            get { return mass; }
            set
            {
                mass = value;
                if(value == 0.0f)
                    inverseMass = 0.0f;
                else
                    inverseMass = 1.0f / value;
            }
        }

        public float InverseMass 
        { 
            get { return inverseMass; } 
        }

        public PhysicsCmp()
        {
            Force             = Vector2.Zero;
            LinearVelocity    = Vector2.Zero;
            GravityMultiplier = 1.0f;
            Restitution       = 0.5f;
            mass              = 1.0f;
            inverseMass       = 1.0f;
            maxLinearVelocity = new Vector2(1000000.0f, 1000000.0f);
        }
    }
}
