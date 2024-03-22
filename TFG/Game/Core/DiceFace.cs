using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Engine.Ecs;
using Engine.Core;
using Cmps;
using Engine.Graphics;

namespace Core
{
    public class DiceFace
    {
        public Rectangle SourceRect;
        public DiceFace() { }

        //Returns if the action has finished
        public virtual bool Update(float dt, EntityManager<Entity> entityManager,
            Camera2D camera, Entity target) { return true; }
        public virtual void Draw(SpriteBatch spriteBatch, 
            Entity target) { }
    }

    public class DashDiceFace : DiceFace
    {
        public const float DISTANCE = 64.0f;

        public int Power;

        private float maxAngle;
        private float currentTime;
        private Vector2 currentDirection;

        public DashDiceFace(int power)
        {
            Power            = power;
            maxAngle         = MathHelper.ToRadians(15.0f);
            currentTime      = 0.0f;
            currentDirection = Vector2.Zero;
            SourceRect       = new Rectangle(32 + power * 32, 0, 32, 32);
        }

        public override bool Update(float dt, EntityManager<Entity> entityManager, 
            Camera2D camera, Entity target)
        {
            currentTime += dt;
            if (currentTime >= MathUtil.PI2)
                currentTime -= MathUtil.PI2;

            float currentAngle    = MathF.Sin(currentTime * 4.0f) * maxAngle;
            Vector2 baseDirection = Vector2.Normalize(target.Position - 
                MouseInput.GetPosition(camera));
            currentDirection   = MathUtil.Rotate(baseDirection, currentAngle);

            if(MouseInput.IsLeftButtonPressed())
            {
                PhysicsCmp phy      = entityManager.GetComponent<PhysicsCmp>(target);
                phy.LinearVelocity += Power * DISTANCE * 
                    phy.LinearDamping * currentDirection;

                return true;
            }

            return false;
        }

        public override void Draw(SpriteBatch spriteBatch, Entity target) 
        {
            Vector2 start = target.Position;
            Vector2 end   = target.Position + currentDirection * Power * DISTANCE;
            spriteBatch.DrawArrow(start, end, new Color(255, 255, 255, 64), 6.0f);
        }
    }

    public class ProjectileDiceFace : DiceFace
    {
        public const float DISTANCE = 32.0f;

        private float maxAngle;
        private float currentTime;
        private Vector2 currentDirection;

        public ProjectileDiceFace()
        {
            maxAngle = MathHelper.ToRadians(25.0f);
            currentTime = 0.0f;
            currentDirection = Vector2.Zero;
            SourceRect = new Rectangle(8 * 32, 0, 32, 32);
        }

        public override bool Update(float dt, EntityManager<Entity> entityManager,
            Camera2D camera, Entity target)
        {
            currentTime += dt;
            if (currentTime >= MathUtil.PI2)
                currentTime -= MathUtil.PI2;

            float currentAngle = MathF.Sin(currentTime) * maxAngle;
            Vector2 baseDirection = Vector2.Normalize(target.Position -
                MouseInput.GetPosition(camera));
            currentDirection = MathUtil.Rotate(baseDirection, currentAngle);

            if (MouseInput.IsLeftButtonPressed())
            {
                

                return true;
            }

            return false;
        }

        public override void Draw(SpriteBatch spriteBatch, Entity target)
        {
            Vector2 start = target.Position;
            Vector2 end = target.Position + currentDirection * DISTANCE;
            spriteBatch.DrawArrow(start, end, new Color(255, 255, 255, 64), 6.0f);
        }
    }
}
