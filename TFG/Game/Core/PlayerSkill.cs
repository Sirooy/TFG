using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Engine.Ecs;
using Engine.Core;
using Engine.Graphics;
using AI;
using Cmps;
using Physics;
using System.Collections.Generic;
using TFG.Game.AI;

namespace Core
{
    public abstract class PlayerSkill
    {
        private Rectangle sourceRect;
        private CharacterType type;
        private CharacterType canBeUsedByType;

        public Rectangle SourceRect           { get { return sourceRect; } }
        public CharacterType Type             { get { return type; } }
        public CharacterType CanBeUsedByTypes { get { return canBeUsedByType; } }

        public PlayerSkill(Rectangle sourceRect, CharacterType type, 
            CharacterType canBeUsedBy) 
        {
            this.sourceRect      = sourceRect;
            this.type            = type;
            this.canBeUsedByType = canBeUsedBy;
        }

        public virtual void Init() { }
        public virtual SkillState Update(GameWorld world, Entity target) 
            { return SkillState.Finished; }
        public virtual void Draw(GameWorld world, SpriteBatch spriteBatch, 
            Entity target) { }

        public bool CanUseSkill(CharacterCmp chara)
        {
            return ((chara.Type & canBeUsedByType) != CharacterType.None &&
                    (chara.CanUseSkillsOfType & type) != CharacterType.None);
        }
    }

    public class DirectDamagePSkill : PlayerSkill
    {
        public float Damage;

        public DirectDamagePSkill(float damage = 5.0f) : 
                base(new Rectangle(16, 0, 32, 32),
            CharacterType.Normal,
            CharacterType.Enemy) 
        {
            this.Damage = damage;
        }

        public override SkillState Update(GameWorld world, Entity target)
        {
            world.EntityFactory.CreateEffect(EffectType.Slash, 
                target.Position);

            if(world.EntityManager.TryGetComponent(target, 
                out HealthCmp health))
            {
                health.AddHealth(-Damage);
            }

            return SkillState.Finished;
        }
    }

    public class DashPlayerSkill : PlayerSkill
    {
        public const float DISTANCE = 64.0f;

        public int Power;

        private float maxAngle;
        private float currentTime;
        private Vector2 currentDirection;

        public DashPlayerSkill(int power) : base(new Rectangle(32 + power * 32, 0, 32, 32),
            CharacterType.Normal,
            CharacterType.Player)
        {
            Power            = power;
            maxAngle         = MathHelper.ToRadians(15.0f);
            currentTime      = 0.0f;
            currentDirection = Vector2.Zero;
        }

        public override SkillState Update(GameWorld world, Entity target)
        {
            currentTime += world.Dt;
            if (currentTime >= MathUtil.PI2)
                currentTime -= MathUtil.PI2;

            float currentAngle    = MathF.Sin(currentTime * 4.0f) * maxAngle;
            Vector2 baseDirection = Vector2.Normalize(target.Position - 
                MouseInput.GetPosition(world.Camera));
            currentDirection   = MathUtil.Rotate(baseDirection, currentAngle);

            if(MouseInput.IsLeftButtonPressed())
            {
                PhysicsCmp phy      = world.EntityManager.GetComponent<PhysicsCmp>(target);
                phy.LinearVelocity += Power * DISTANCE * 
                    phy.LinearDamping * currentDirection;

                return SkillState.Finished;
            }

            return SkillState.Executing;
        }

        public override void Draw(GameWorld world, 
            SpriteBatch spriteBatch, Entity target) 
        {
            Vector2 start = target.Position;
            Vector2 end   = target.Position + currentDirection * Power * DISTANCE;
            spriteBatch.DrawArrow(start, end, 16.0f, new Color(255, 255, 255, 64));
        }
    }

    public class PathFollowPSkill : PlayerSkill
    {
        private enum InternalState
        {
            SelectingPosition,
            FollowingPosition
        }

        public float MaxDistance;

        private float speed;
        private float travelTime;
        private float maxTravelDistance;
        private List<Vector2> path;
        private Vector2 targetPosition;
        private InternalState internalState;

        public PathFollowPSkill() : base(new Rectangle(32, 0, 32, 32),
            CharacterType.Normal,
            CharacterType.Player)
        {
            path           = new List<Vector2>();
            speed          = 50.0f;
            targetPosition = Vector2.Zero;
            MaxDistance    = 150.0f;
        }

        public override void Init()
        {
            internalState     = InternalState.SelectingPosition;
            travelTime        = 0.0f;
            maxTravelDistance = 0.0f;
            targetPosition    = Vector2.Zero;
        }

        public override SkillState Update(GameWorld world, Entity target)
        {
            if(internalState == InternalState.SelectingPosition)
            {
                Vector2 mousePos = MouseInput.GetPosition(world.Camera);

                if (MouseInput.IsLeftButtonPressed() && 
                    PositionIsValid(world, mousePos, target))
                {
                    targetPosition    = mousePos;
                    maxTravelDistance = Vector2.Distance(target.Position, 
                        targetPosition);
                    internalState     = InternalState.FollowingPosition;
                }

                return SkillState.Executing;
            }
            else
            {
                travelTime          += world.Dt;
                float travelDistance = travelTime * speed;

                if (world.EntityManager.TryGetComponent(target,
                    out PhysicsCmp physics))
                {
                    world.Level.PathFindingMap.FindPath(path,
                        target.Position, targetPosition);

                    if (path.Count <= 1 || travelDistance >= maxTravelDistance)
                    {
                        physics.LinearVelocity *= 0.5f;
                        return SkillState.Finished;
                    }

                    AIUtil.PathFollowing(target, physics, path, speed);

                    return SkillState.Executing;
                }

                return SkillState.Finished;
            }
        }

        public override void Draw(GameWorld world, 
            SpriteBatch spriteBatch, Entity target)
        {
            if(internalState == InternalState.SelectingPosition)
            {
                spriteBatch.DrawCircle(target.Position, MaxDistance, 
                    1.0f, Color.Yellow, 32);

                Vector2 mousePos = MouseInput.GetPosition(world.Camera);
                Color color      = Color.Red;
                if(PositionIsValid(world, mousePos, target))
                    color = new Color(0, 255, 0);

                spriteBatch.DrawCircle(mousePos, 4.0f, 1.0f, 
                    color, 16);
            }
        }

        private bool PositionIsValid(GameWorld world, 
            Vector2 position, Entity e)
        {
            float distSqr = Vector2.DistanceSquared(e.Position, position);
            if (world.Level.CollisionMap.HasCollision(position) ||
                distSqr > MaxDistance * MaxDistance)
            {
                return false;
            }

            return true;
        }
    }

    public class ProjectilePSkill : PlayerSkill
    {
        public const float DISTANCE = 48.0f;

        private float maxAngle;
        private float currentTime;
        private Vector2 currentDirection;

        public ProjectilePSkill() : base(new Rectangle(8 * 32, 0, 32, 32),
            CharacterType.Normal,
            CharacterType.Player)
        {
            maxAngle         = MathHelper.ToRadians(35.0f);
            currentTime      = 0.0f;
            currentDirection = Vector2.Zero;
        }

        public override SkillState Update(GameWorld world, Entity target)
        {
            currentTime += world.Dt;
            if (currentTime >= MathUtil.PI2)
                currentTime -= MathUtil.PI2;

            float currentAngle = MathF.Sin(currentTime * 2.0f) * maxAngle;
            Vector2 baseDirection = Vector2.Normalize(target.Position -
                MouseInput.GetPosition(world.Camera));
            currentDirection = MathUtil.Rotate(baseDirection, currentAngle);

            if (MouseInput.IsLeftButtonPressed())
            {
                Vector2 position = target.Position + currentDirection * 8.0f;
                Entity projectile = world.EntityFactory.CreateAttack(
                    AttackType.Projectile1, position);
                projectile.Rotation = MathF.Atan2(
                    currentDirection.Y, currentDirection.X);

                TriggerColliderCmp colCmp = world.EntityManager.GetComponent
                    <TriggerColliderCmp>(projectile);
                colCmp.AddCollisionMask(CollisionBitmask.Enemy);

                PhysicsCmp physicsCmp = world.EntityManager.GetComponent
                    <PhysicsCmp>(projectile);
                physicsCmp.LinearVelocity = currentDirection * 200.0f;
                
                return SkillState.Finished;
            }

            return SkillState.Executing;
        }

        public override void Draw(GameWorld world, 
            SpriteBatch spriteBatch, Entity target)
        {
            Vector2 start = target.Position;
            Vector2 end = target.Position + currentDirection * DISTANCE;
            spriteBatch.DrawArrow(start, end, 8.0f, 
                new Color(255, 255, 255, 64));
        }
    }

    //DEBUG DICE FACES
    #region Debug Faces
    public class TestPSkill : PlayerSkill
    {
        public TestPSkill() : base(new Rectangle(32, 0, 32, 32),
            CharacterType.AllTypes,
            CharacterType.AllTypes)
            { }

        public override void Init()
        {
            //ZigZagArrowMinigame.Init(MathHelper.ToRadians(45.0f),
            //    1.0f, 32.0f, 96.0f);
            ChargeCircleMinigame.Init(1.0f, 2.0f, Color.Yellow, Color.Red,
                16.0f, 32.0f);
        }

        public override SkillState Update(GameWorld world, Entity target)
        {
            //MinigameState minigameState = ChargeBarMinigame.Update(dt);
            //if (minigameState == MinigameState.Finished)
            //{
            //    return SkillState.Finished;
            //}
            //ZigZagArrowMinigame.Update(dt, target, camera);
            if (ChargeCircleMinigame.Update(world.Dt) == MinigameState.Finished)
                return SkillState.Finished;

            return SkillState.Executing;
        }

        public override void Draw(GameWorld world, 
            SpriteBatch spriteBatch, Entity target)
        {
            //ChargeBarMinigame.Draw(spriteBatch, target.Position, 48.0f, 8.0f, 
            //    Color.White);
            //ZigZagArrowMinigame.Draw(spriteBatch, target.Position, 24.0f, 48.0f, 
            //    Color.White);
            ChargeCircleMinigame.Draw(spriteBatch, target.Position, 1.0f,
                Color.White, 32);
        }
    }

    public class KillEntityPSkill : PlayerSkill
    {
        public KillEntityPSkill() : base(new Rectangle(32, 0, 32, 32),
            CharacterType.AllTypes,
            CharacterType.AllTypes)
            { }

        public override SkillState Update(GameWorld world, Entity target)
        { 
            if(world.EntityManager.TryGetComponent(target, out HealthCmp health))
                health.CurrentHealth = 0.0f;

            return SkillState.Finished;
        }
    }

    public class FullHealDiceFace : PlayerSkill
    {
        public FullHealDiceFace() : base(new Rectangle(32, 0, 32, 32), 
            CharacterType.AllTypes, 
            CharacterType.AllTypes)
            { }

        public override SkillState Update(GameWorld world, Entity target)
        {
            if (world.EntityManager.TryGetComponent(target, out HealthCmp health))
                health.CurrentHealth = health.MaxHealth;

            return SkillState.Finished;
        }
    }

    #endregion
}
