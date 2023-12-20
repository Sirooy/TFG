using Microsoft.Xna.Framework;
using System;

namespace Cmps
{
    public class PhysicsCmp
    {
        public Vector2 Force;
        public Vector2 LinearVelocity;
        public float Torque;
        public float AngularVelocity;
        public float LinearDamping;
        public float AngularDamping;
        public float GravityMultiplier;

        private Vector2 maxLinearVelocity;
        private float maxAngularVelocity;
        private float mass;
        private float inverseMass;
        public float inertia;
        public float inverseInertia;

        public Vector2 MaxLinearVelocity
        {
            get { return maxLinearVelocity; }
            set
            {
                maxLinearVelocity.X = MathF.Abs(value.X);
                maxLinearVelocity.Y = MathF.Abs(value.Y);
            }
        }

        public float MaxAngularVelocity
        {
            get { return maxAngularVelocity; }
            set
            {
                maxAngularVelocity = MathF.Abs(value);
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

        public float Inertia
        {
            get { return inertia; }
            set
            {
                inertia = value;
                if (value == 0.0f)
                    inverseInertia = 0.0f;
                else
                    inverseInertia = 1.0f / value;
            }
        }

        public float InverseIntertia
        {
            get { return inverseInertia; }
        }

        public PhysicsCmp()
        {
            Force              = Vector2.Zero;
            LinearVelocity     = Vector2.Zero;
            Torque             = 0.0f;
            AngularVelocity    = 0.0f;
            LinearDamping      = 0.0f;
            AngularDamping     = 0.0f;
            GravityMultiplier  = 1.0f;
            mass               = 1.0f;
            inverseMass        = 1.0f;
            inertia            = 1.0f;
            inverseInertia     = 1.0f;
            maxLinearVelocity  = new Vector2(1000000.0f, 1000000.0f);
            maxAngularVelocity = 1000000.0f;
        }
    }
}
