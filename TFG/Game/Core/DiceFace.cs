using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Engine.Ecs;
using Engine.Core;
using Engine.Graphics;
using AI;
using Cmps;
using Physics;

namespace Core
{
    public abstract class DiceFace
    {
        private Rectangle sourceRect;

        public Rectangle SourceRect { get { return sourceRect; } }

        public DiceFace(Rectangle sourceRect) 
        {
            this.sourceRect = sourceRect;
        }

        public virtual SkillState Update(float dt, EntityManager<Entity> entityManager,
            EntityFactory entityFactory, Camera2D camera, Entity target) 
            { return SkillState.Finished; }
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

        public DashDiceFace(int power) : base(new Rectangle(32 + power * 32, 0, 32, 32))
        {
            Power            = power;
            maxAngle         = MathHelper.ToRadians(15.0f);
            currentTime      = 0.0f;
            currentDirection = Vector2.Zero;
        }

        public override SkillState Update(float dt, EntityManager<Entity> entityManager,
            EntityFactory entityFactory, Camera2D camera, Entity target)
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

                return SkillState.Finished;
            }

            return SkillState.Executing;
        }

        public override void Draw(SpriteBatch spriteBatch, Entity target) 
        {
            Vector2 start = target.Position;
            Vector2 end   = target.Position + currentDirection * Power * DISTANCE;
            spriteBatch.DrawArrow(start, end, 0.5f, new Color(255, 255, 255, 64));
        }
    }

    public class ProjectileDiceFace : DiceFace
    {
        public const float DISTANCE = 48.0f;

        private float maxAngle;
        private float currentTime;
        private Vector2 currentDirection;

        public ProjectileDiceFace() : base(new Rectangle(8 * 32, 0, 32, 32))
        {
            maxAngle         = MathHelper.ToRadians(35.0f);
            currentTime      = 0.0f;
            currentDirection = Vector2.Zero;
        }

        public override SkillState Update(float dt, EntityManager<Entity> entityManager,
            EntityFactory entityFactory, Camera2D camera, Entity target)
        {
            currentTime += dt;
            if (currentTime >= MathUtil.PI2)
                currentTime -= MathUtil.PI2;

            float currentAngle = MathF.Sin(currentTime * 2.0f) * maxAngle;
            Vector2 baseDirection = Vector2.Normalize(target.Position -
                MouseInput.GetPosition(camera));
            currentDirection = MathUtil.Rotate(baseDirection, currentAngle);

            if (MouseInput.IsLeftButtonPressed())
            {
                Vector2 position = target.Position + currentDirection * 8.0f;
                Entity projectile = entityFactory.CreateAttack(
                    AttackType.Projectile1, position);
                projectile.Rotation = MathF.Atan2(
                    currentDirection.Y, currentDirection.X);

                TriggerColliderCmp colCmp = entityManager.GetComponent
                    <TriggerColliderCmp>(projectile);
                colCmp.AddCollisionMask(CollisionBitmask.Enemy);

                PhysicsCmp physicsCmp = entityManager.GetComponent
                    <PhysicsCmp>(projectile);
                physicsCmp.LinearVelocity = currentDirection * 200.0f;
                
                return SkillState.Finished;
            }

            return SkillState.Executing;
        }

        public override void Draw(SpriteBatch spriteBatch, Entity target)
        {
            Vector2 start = target.Position;
            Vector2 end = target.Position + currentDirection * DISTANCE;
            spriteBatch.DrawArrow(start, end, 0.5f, 
                new Color(255, 255, 255, 64));
        }
    }

    //DEBUG DICE FACES
    public class KillEntityDiceFace : DiceFace
    {
        public KillEntityDiceFace() : base(new Rectangle(32, 0, 32, 32))
            { }

        public override SkillState Update(float dt, EntityManager<Entity> entityManager,
            EntityFactory entityFactory, Camera2D camera, Entity target)
        { 
            if(entityManager.TryGetComponent(target, out HealthCmp health))
                health.CurrentHealth = 0.0f;

            return SkillState.Finished;
        }
    }
}
