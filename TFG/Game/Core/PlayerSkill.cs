using System;
using System.Collections.Generic;
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
    public abstract class PlayerSkill
    {
        protected const float CHARGE_BAR_LENGTH = 24.0f;
        protected const float CHARGE_BAR_HEIGHT = 8.0f;
        protected const int ICON_SIZE = 48;
        protected const int ICONS_PER_ROW = 10;

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

        public PlayerSkill(int sourceIndex, CharacterType type,
            CharacterType canBeUsedBy)
        {
            this.sourceRect = GetIcon(sourceIndex);
            this.type = type;
            this.canBeUsedByType = canBeUsedBy;
        }

        public Rectangle GetIcon(int index)
        {
            return new Rectangle(
                (index % ICONS_PER_ROW) * ICON_SIZE, 
                (index / ICONS_PER_ROW) * ICON_SIZE, 
                ICON_SIZE, 
                ICON_SIZE);
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

        protected static bool PositionIsValid(GameWorld world,
            Vector2 center, Vector2 position, float maxDistance)
        {
            float distSqr = Vector2.DistanceSquared(center, position);
            if (world.Level.CollisionMap.HasCollision(position) ||
                distSqr > maxDistance * maxDistance)
            {
                return false;
            }

            return true;
        }
        
        protected static void DrawCircularSelectPosition(GameWorld world, 
            SpriteBatch spriteBatch, Vector2 position, 
            float maxDistance, Color circleColor)
        {
            spriteBatch.DrawCircle(position, maxDistance,
                    1.0f, circleColor, 32);

            Vector2 mousePos   = MouseInput.GetPosition(world.Camera);
            Color pointerColor = Color.Red;
            if (PositionIsValid(world, position, mousePos, maxDistance))
                pointerColor = new Color(0, 255, 0);

            spriteBatch.DrawCircle(mousePos, 4.0f, 1.0f,
                pointerColor, 16);
        }
    }

    public class DirectDamagePSkill : PlayerSkill
    {
        public float Damage;

        public DirectDamagePSkill(int level) : base(3 + level,
            CharacterType.Normal,
            CharacterType.Enemy) 
        {
            this.Damage = level * 3.0f;
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

    public class DirectHealthPSkill : PlayerSkill
    {
        public float Health;

        public DirectHealthPSkill(int level) :
                base(6 + level,
            CharacterType.Normal,
            CharacterType.Player)
        {
            this.Health = 3.0f * level;
        }

        public override SkillState Update(GameWorld world, Entity target)
        {
            world.EntityFactory.CreateEffect(EffectType.Health,
                target.Position);

            if (world.EntityManager.TryGetComponent(target,
                out HealthCmp health))
            {
                health.AddHealth(Health);
            }

            return SkillState.Finished;
        }
    }

    #region Movement Skills

    public class DragDashPlayerSkill : PlayerSkill
    {
        private const float MIN_DISTANCE = 20.0f;
        private int levels;

        public DragDashPlayerSkill(int level) : base(
            -1 + level,
            CharacterType.Normal,
            CharacterType.Player)
        {
            level  = Math.Clamp(level, 1, 4);
            levels = level;
        }

        public override void Init()
        {
            ZigZagChargeArrowMinigame.Init(levels, MathHelper.ToRadians(50.0f),
                1.0f, 0.5f, 8.0f, 24.0f + levels * 4.0f, Color.Yellow, Color.Red);
        }

        public override SkillState Update(GameWorld world, Entity target)
        {
            if(ZigZagChargeArrowMinigame.Update(world.Dt, target, world.Camera) == 
                MinigameState.Finished)
            {
                int level = ZigZagChargeArrowMinigame.CurrentLevel;

                AIUtil.SetEntityDashVelocity(world.EntityManager, target,
                    ZigZagChargeArrowMinigame.Direction,
                    MIN_DISTANCE + level * 30.0f);

                return SkillState.Finished;
            }

            return SkillState.Executing;
        }

        public override void Draw(GameWorld world, 
            SpriteBatch spriteBatch, Entity target) 
        {
            ZigZagChargeArrowMinigame.Draw(spriteBatch,
                target.Position, 10.0f, MIN_DISTANCE, 24.0f, 6.0f,
                new Color(255, 255, 255, 128), Color.White);
        }
    }

    public class RotatingDashPlayerSkill : PlayerSkill
    {
        private enum InternalState
        {
            SelectingDirection,
            ChargingPower
        }

        public const float DISTANCE = 64.0f;

        private float rps;
        private float minDistance;
        private float maxDistance;
        private float chargeBarFillSpeed;
        private InternalState internalState;
        private Vector2 currentDirection;

        public RotatingDashPlayerSkill(int level) : base(
            34 + level,
            CharacterType.Warrior,
            CharacterType.Normal)
        {
            level = Math.Clamp(level, 1, 4);

            currentDirection   = Vector2.Zero;
            rps                = 0.8f + level * 0.2f;
            minDistance        = 20.0f;
            maxDistance        = 50.0f + level * 35.0f;
            chargeBarFillSpeed = 1.1f - level * 0.1f;

        }

        public override void Init()
        {
            internalState = InternalState.SelectingDirection;
            currentDirection = Vector2.Zero;

            ChargeBarMinigame.Init(chargeBarFillSpeed, 1.0f,
                Color.Yellow, Color.Red, minDistance, maxDistance);
            RotatingArrowMinigame.Init(rps, 0.5f);
        }

        public override SkillState Update(GameWorld world, Entity target)
        {
            if (internalState == InternalState.SelectingDirection)
            {
                if (RotatingArrowMinigame.Update(world.Dt) == MinigameState.Finished)
                {
                    currentDirection = RotatingArrowMinigame.Direction;
                    internalState = InternalState.ChargingPower;
                }

                return SkillState.Executing;
            }
            else
            {
                if (ChargeBarMinigame.Update(world.Dt) == MinigameState.Finished)
                {
                    float currentDistance = ChargeBarMinigame.Value;
                    AIUtil.SetEntityDashVelocity(world.EntityManager,
                        target, currentDirection, currentDistance);

                    return SkillState.Finished;
                }

                return SkillState.Executing;
            }
        }

        public override void Draw(GameWorld world,
            SpriteBatch spriteBatch, Entity target)
        {
            if (internalState == InternalState.SelectingDirection)
            {
                RotatingArrowMinigame.Draw(spriteBatch, target.Position,
                    12.0f, minDistance, new Color(255, 255, 255, 128));
            }
            else if (internalState == InternalState.ChargingPower)
            {
                Color arrowColor = ChargeBarMinigame.CurrentColor;
                arrowColor.A = 128;
                spriteBatch.DrawArrow(target.Position, target.Position +
                    currentDirection * ChargeBarMinigame.Value, 10.0f,
                    arrowColor);
                ChargeBarMinigame.Draw(spriteBatch, target.Position,
                    CHARGE_BAR_LENGTH, CHARGE_BAR_HEIGHT, Color.White);
            }
        }
    }

    public class TeleportPSkill : PlayerSkill
    {
        private const float FADE_TIME = 1.0f;

        private enum InternalState
        {
            ChargingRange,
            SelectingPosition,
            FadingOut,
            FadingIn
        }

        private float minDistance;
        private float maxDistance;
        private float growSpeed;
        private float currentDistance;
        private float fadeTimer;
        private Vector2 targetPosition;
        private InternalState internalState;

        public TeleportPSkill(int level) : base(
            21 + level,
            CharacterType.Normal,
            CharacterType.Mage | CharacterType.Enemy)
        {
            targetPosition = Vector2.Zero;
            currentDistance = 0.0f;

            level = Math.Clamp(level, 1, 4);
            growSpeed = 1.0f + level * 0.25f;
            minDistance = 20.0f + level * 2.5f;
            maxDistance = 40.0f + level * 10.0f;
        }

        public override void Init()
        {
            internalState   = InternalState.ChargingRange;
            currentDistance = 0.0f;
            targetPosition  = Vector2.Zero;
            fadeTimer       = FADE_TIME;

            ChargeCircleMinigame.Init(growSpeed, 1.0f, Color.White, Color.Red,
                minDistance, maxDistance);
        }

        public override SkillState Update(GameWorld world, Entity target)
        {
            if (internalState == InternalState.ChargingRange)
            {
                if (ChargeCircleMinigame.Update(world.Dt) == MinigameState.Finished)
                {
                    internalState = InternalState.SelectingPosition;
                    currentDistance = ChargeCircleMinigame.Radius;
                }

                return SkillState.Executing;
            }
            else if (internalState == InternalState.SelectingPosition)
            {
                Vector2 mousePos = MouseInput.GetPosition(world.Camera);

                if (MouseInput.IsLeftButtonPressed() &&
                    PositionIsValid(world, target.Position, mousePos, currentDistance))
                {
                    targetPosition = mousePos;
                    internalState  = InternalState.FadingOut;
                    fadeTimer      = FADE_TIME;
                }

                return SkillState.Executing;
            }
            else if (internalState == InternalState.FadingOut)
            {
                AIUtil.FadeOut(world, target, FADE_TIME, ref fadeTimer);

                if (fadeTimer <= 0.0f)
                {
                    fadeTimer       = FADE_TIME;
                    target.Position = targetPosition;
                    internalState   = InternalState.FadingIn;
                }

                return SkillState.Executing;
            }
            else
            {
                AIUtil.FadeIn(world, target, FADE_TIME, ref fadeTimer);

                if (fadeTimer <= 0.0f)
                {
                    return SkillState.Finished;
                }

                return SkillState.Executing;
            }
        }

        public override void Draw(GameWorld world,
            SpriteBatch spriteBatch, Entity target)
        {
            if (internalState == InternalState.ChargingRange)
            {
                ChargeCircleMinigame.Draw(spriteBatch, target.Position,
                    1.0f, Color.Purple, 32);
            }
            else if (internalState == InternalState.SelectingPosition)
            {
                DrawCircularSelectPosition(world, spriteBatch,
                    target.Position, currentDistance, Color.Yellow);
            }
        }
    }
    

    public class PathFollowPSkill : PlayerSkill
    {
        private enum InternalState
        {
            ChargingRange,
            SelectingPosition,
            FollowingPosition
        }

        private float minDistance;
        private float maxDistance;
        private float growSpeed;
        private float currentDistance;
        private float speed;
        private float travelTime;
        private float maxTravelDistance;
        private List<Vector2> path;
        private Vector2 targetPosition;
        private InternalState internalState;

        public PathFollowPSkill(int level) : 
            base(9 + level,
            CharacterType.Normal,
            CharacterType.Ranger)
        {
            path            = new List<Vector2>();
            speed           = 50.0f;
            targetPosition  = Vector2.Zero;
            currentDistance = 0.0f;

            level = Math.Clamp(level, 1, 4);

            growSpeed   = 1.0f + level * 0.1f;
            minDistance = 5.0f + level * 5.0f;
            maxDistance = 83.3f + level * 16.7f;
        }

        public override void Init()
        {
            internalState     = InternalState.ChargingRange;
            travelTime        = 0.0f;
            currentDistance   = 0.0f;
            maxTravelDistance = 0.0f;
            targetPosition    = Vector2.Zero;

            ChargeCircleMinigame.Init(growSpeed, 1.0f, Color.White, Color.Red, 
                minDistance, maxDistance);
        }

        public override SkillState Update(GameWorld world, Entity target)
        {
            if(internalState == InternalState.ChargingRange)
            {
                if(ChargeCircleMinigame.Update(world.Dt) == MinigameState.Finished)
                {
                    internalState   = InternalState.SelectingPosition;
                    currentDistance = ChargeCircleMinigame.Radius;
                }

                return SkillState.Executing;
            }
            else if(internalState == InternalState.SelectingPosition)
            {
                Vector2 mousePos = MouseInput.GetPosition(world.Camera);

                if (MouseInput.IsLeftButtonPressed() && 
                    PositionIsValid(world, target.Position, mousePos, currentDistance))
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
            if(internalState == InternalState.ChargingRange)
            {
                ChargeCircleMinigame.Draw(spriteBatch, target.Position,
                    1.0f, Color.White, 32);
            }
            else if(internalState == InternalState.SelectingPosition)
            {
                DrawCircularSelectPosition(world, spriteBatch,
                    target.Position, currentDistance, Color.Yellow);
            }
        }
    }

    #endregion

    #region Warrior

    public class SwordSpinPSkill : PlayerSkill
    {
        private float minRadius;
        private float maxRadius;
        private float growSpeed;
        private float damage;

        public SwordSpinPSkill(int level) : base(
            38 + level,
            CharacterType.Warrior,
            CharacterType.Player)
        {

            level = Math.Clamp(level, 1, 4);
            growSpeed = 1.0f + level * 0.25f;
            minRadius = 10.0f + level * 2.5f;
            maxRadius = 30.0f + level * 10.0f;
            damage    = 5.0f + level * 3.0f;
        }

        public override void Init()
        {
            ChargeCircleMinigame.Init(growSpeed, 1.0f, Color.White, Color.Red,
                minRadius, maxRadius);
        }

        public override SkillState Update(GameWorld world, Entity target)
        {
            if(ChargeCircleMinigame.Update(world.Dt) == MinigameState.Finished)
            {
                float radius = ChargeCircleMinigame.Radius;

                Entity attack = world.EntityFactory.CreateAttack(AttackType.SwordSpin,
                    target.Position, damage, 3000.0f, CollisionBitmask.Enemy);
                TriggerColliderCmp collider = world.EntityManager.
                    GetComponent<TriggerColliderCmp>(attack);
                CircleCollider circle = (CircleCollider)collider.Collider;
                attack.Scale          = radius / circle.Radius;

                return SkillState.Finished;
            }

            return SkillState.Executing;
        }

        public override void Draw(GameWorld world,
            SpriteBatch spriteBatch, Entity target)
        {
            ChargeCircleMinigame.Draw(spriteBatch, target.Position,
                1.0f, Color.Yellow, 32);
        }
    }

    public class SwordStabPSkill : PlayerSkill
    {
        private enum InternalState
        {
            SelectingDirections,
            ExecutingAttacks
        }

        private float rps;
        private int level;
        private int currentNumAttacks;
        private int currentAttackIndex;
        private Entity currentAttack;
        private InternalState internalState;
        private List<Vector2> attackDirections;

        public SwordStabPSkill(int level) :base(
            41 + level,
            CharacterType.Warrior,
            CharacterType.Normal)
        {
            level = Math.Clamp(level, 1, 4);

            this.level         = level;
            rps                = 0.8f + level * 0.2f;
            attackDirections   = new List<Vector2>();
            currentNumAttacks  = level;
            currentAttackIndex = 0;
        }

        public override void Init()
        {
            attackDirections.Clear();
            internalState      = InternalState.SelectingDirections;
            currentNumAttacks  = level;
            currentAttackIndex = 0;

            RotatingArrowMinigame.Init(rps, 0.5f);
        }

        public override SkillState Update(GameWorld world, Entity target)
        {
            if (internalState == InternalState.SelectingDirections)
            {
                if (RotatingArrowMinigame.Update(world.Dt) == MinigameState.Finished)
                {
                    attackDirections.Add(RotatingArrowMinigame.Direction);
                    currentNumAttacks--;

                    if (currentNumAttacks == 0)
                        internalState = InternalState.ExecutingAttacks;
                    else
                        RotatingArrowMinigame.Init(rps, 0.5f);
                }

                return SkillState.Executing;
            }
            else
            {
                if(currentAttack == null)
                {
                    Vector2 direction = attackDirections[currentAttackIndex];
                    currentAttack = world.EntityFactory.CreateAttack(AttackType.SwordStab,
                        target.Position, 5.0f, 500.0f, CollisionBitmask.Enemy);
                    AIUtil.SetProjectilePosition(world.EntityManager, currentAttack,
                        target.Position, direction);
                    AIUtil.SetProjectileDirection(world.EntityManager, currentAttack,
                        direction);

                    currentAttackIndex++;
                }
                else
                {
                    if(!currentAttack.IsValid)
                    {
                        currentAttack = null;
                        if(currentAttackIndex == attackDirections.Count)
                            return SkillState.Finished;
                    }
                }

                return SkillState.Executing;
            }
        }

        public override void Draw(GameWorld world,
            SpriteBatch spriteBatch, Entity target)
        {
            if (internalState == InternalState.SelectingDirections)
            {
                RotatingArrowMinigame.Draw(spriteBatch, target.Position,
                    12.0f, 32.0f, new Color(255, 255, 255, 128));
            }
        }
    }

    #endregion

    #region Mage

    public class FireballPSkill : PlayerSkill
    {
        private enum InternalState
        {
            SelectingDirections,
            ExecutingAttacks
        }

        private float maxAngle;
        private int level;
        private int currentNumAttacks;
        private int currentAttackIndex;
        private Entity currentAttack;
        private InternalState internalState;
        private List<Vector2> attackDirections;

        public FireballPSkill(int level) : base(
            25 + level,
            CharacterType.Mage,
            CharacterType.Normal)
        {
            level = Math.Clamp(level, 1, 4);

            this.level = level;
            maxAngle   = MathHelper.ToRadians(35.0f + level * 5.0f);
            attackDirections = new List<Vector2>();
            currentNumAttacks = level;
            currentAttackIndex = 0;
        }

        public override void Init()
        {
            attackDirections.Clear();
            internalState = InternalState.SelectingDirections;
            currentNumAttacks = level;
            currentAttackIndex = 0;

            ZigZagArrowMinigame.Init(maxAngle, 1.0f, 0.5f);
        }

        public override SkillState Update(GameWorld world, Entity target)
        {
            if (internalState == InternalState.SelectingDirections)
            {
                if (ZigZagArrowMinigame.Update(world.Dt, target, world.Camera) == MinigameState.Finished)
                {
                    attackDirections.Add(ZigZagArrowMinigame.Direction);
                    currentNumAttacks--;

                    if (currentNumAttacks == 0)
                        internalState = InternalState.ExecutingAttacks;
                    else
                        ZigZagArrowMinigame.Init(maxAngle, 1.0f, 0.5f);
                }

                return SkillState.Executing;
            }
            else
            {
                if (currentAttack == null)
                {
                    Vector2 direction = attackDirections[currentAttackIndex];
                    currentAttack = world.EntityFactory.CreateAttack(AttackType.Fireball,
                        target.Position, 5.0f, 500.0f, CollisionBitmask.Enemy);
                    AIUtil.SetProjectilePosAndVel(world.EntityManager, currentAttack,
                        target.Position, direction, 150.0f);

                    currentAttackIndex++;
                }
                else
                {
                    if (!currentAttack.IsValid)
                    {
                        currentAttack = null;
                        if (currentAttackIndex == attackDirections.Count)
                            return SkillState.Finished;
                    }
                }

                return SkillState.Executing;
            }
        }

        public override void Draw(GameWorld world,
            SpriteBatch spriteBatch, Entity target)
        {
            if (internalState == InternalState.SelectingDirections)
            {
                ZigZagArrowMinigame.Draw(spriteBatch, target.Position,
                    12.0f, 24.0f, new Color(255, 255, 255, 128));
            }
        }
    }

    public class WaterballPSkill : PlayerSkill
    {
        private int levels;

        public WaterballPSkill(int level) :
            base(28 + level,
            CharacterType.Mage,
            CharacterType.Player)
        {
            level = Math.Clamp(level, 1, 3);
            levels = level + 1;
        }

        public override void Init()
        {
            ZigZagChargeArrowMinigame.Init(levels, MathHelper.ToRadians(25.0f + (levels - 1) * 5.0f),
                0.75f, 1.0f, 8.0f, 24.0f + levels * 4.0f, Color.Yellow, Color.Red);
        }

        public override SkillState Update(GameWorld world, Entity target)
        {
            if (ZigZagChargeArrowMinigame.Update(world.Dt, target, world.Camera) ==
                MinigameState.Finished)
            {
                Entity attack = world.EntityFactory.CreateAttack(AttackType.Waterball,
                    target.Position, 5.0f + ZigZagChargeArrowMinigame.CurrentLevelPower * 2.0f,
                    1000.0f, CollisionBitmask.Enemy);
                AIUtil.SetProjectilePosAndVel(world.EntityManager, attack,
                    target.Position, ZigZagChargeArrowMinigame.Direction, 150.0f);

                return SkillState.Finished;
            }

            return SkillState.Executing;
        }

        public override void Draw(GameWorld world,
            SpriteBatch spriteBatch, Entity target)
        {
            ZigZagChargeArrowMinigame.Draw(spriteBatch, target.Position,
                8.0f, 24.0f, CHARGE_BAR_LENGTH, CHARGE_BAR_HEIGHT,
                new Color(255, 255, 255, 128), Color.White);
        }
    };

    public class LightningballPSkill : PlayerSkill
    {
        private float rps;
        private float damage;

        public LightningballPSkill(int level) :
            base(31 + level,
            CharacterType.Mage,
            CharacterType.Player)
        {
            level = Math.Clamp(level, 1, 3);
            rps    = 0.5f + level * 0.1f;
            damage = 5.0f + level * 3.0f;
        }

        public override void Init()
        {
            RotatingArrowMinigame.Init(rps, 1.0f);
        }

        public override SkillState Update(GameWorld world, Entity target)
        {
            if (RotatingArrowMinigame.Update(world.Dt) == MinigameState.Finished)
            {
                Entity attack = world.EntityFactory.CreateAttack(AttackType.LightningBall,
                    target.Position, damage, 1000.0f, CollisionBitmask.Enemy);
                AIUtil.SetProjectilePosAndVel(world.EntityManager, attack,
                    target.Position, RotatingArrowMinigame.Direction, 200.0f);

                return SkillState.Finished;
            }

            return SkillState.Executing;
        }

        public override void Draw(GameWorld world,
            SpriteBatch spriteBatch, Entity target)
        {
            RotatingArrowMinigame.Draw(spriteBatch, target.Position,
                8.0f, 16.0f, new Color(255, 255, 255, 128));
        }
    }

    #endregion

    #region Ranger

    public class ArrowPSkill : PlayerSkill
    {
        private int levels;

        public ArrowPSkill(int level) : 
            base(13 + level,
            CharacterType.Ranger,
            CharacterType.Player)
        {
            level     = Math.Clamp(level, 1, 3);
            levels    = level + 1;
        }

        public override void Init()
        {
            ZigZagChargeArrowMinigame.Init(levels, MathHelper.ToRadians(25.0f + (levels - 1) * 5.0f),
                0.75f, 1.0f, 8.0f, 24.0f + levels * 4.0f, Color.Yellow, Color.Red);
        }

        public override SkillState Update(GameWorld world, Entity target)
        {
            if(ZigZagChargeArrowMinigame.Update(world.Dt, target, world.Camera) == 
                MinigameState.Finished)
            {
                Entity arrow = world.EntityFactory.CreateAttack(AttackType.Arrow,
                    target.Position, 5.0f + ZigZagChargeArrowMinigame.CurrentLevelPower * 2.0f, 
                    0.0f, CollisionBitmask.Enemy);
                AIUtil.SetProjectilePosAndVel(world.EntityManager, arrow,
                    target.Position, ZigZagChargeArrowMinigame.Direction, 150.0f);

                return SkillState.Finished;
            }

            return SkillState.Executing;
        }

        public override void Draw(GameWorld world,
            SpriteBatch spriteBatch, Entity target)
        {
            ZigZagChargeArrowMinigame.Draw(spriteBatch, target.Position,
                8.0f, 24.0f, CHARGE_BAR_LENGTH, CHARGE_BAR_HEIGHT,
                new Color(255, 255, 255, 128), Color.White);
        }
    };

    public class HealingArrowPSkill : PlayerSkill
    {
        private float rps;
        private float health;

        public HealingArrowPSkill(int level) : 
            base(19 + level,
            CharacterType.Ranger,
            CharacterType.Player)
        {
            level  = Math.Clamp(level, 1, 2);
            rps    = 0.75f + level * 0.25f;
            health = 5.0f  + level * 10.0f;
        }

        public override void Init()
        {
            RotatingArrowMinigame.Init(rps, 1.0f);
        }

        public override SkillState Update(GameWorld world, Entity target)
        {
            if(RotatingArrowMinigame.Update(world.Dt) == MinigameState.Finished)
            {
                Entity arrow = world.EntityFactory.CreateAttack(AttackType.HealingArrow,
                    target.Position, health, 0.0f, CollisionBitmask.Player);
                arrow.Scale  = 0.8f;
                AIUtil.SetProjectilePosAndVel(world.EntityManager, arrow,
                    target.Position, RotatingArrowMinigame.Direction, 100.0f);

                return SkillState.Finished;
            }

            return SkillState.Executing;
        }

        public override void Draw(GameWorld world,
            SpriteBatch spriteBatch, Entity target)
        {
            RotatingArrowMinigame.Draw(spriteBatch, target.Position,
                8.0f, 16.0f, new Color(255, 255, 255, 128));
        }
    }

    public class PullingArrowPSkill : PlayerSkill
    {
        private enum InternalState
        {
            SelectingDirection,
            ChargingPower
        }

        private InternalState internalState;
        private float minForce;
        private float maxForce;
        private float damage;

        public PullingArrowPSkill(int level) : 
            base(16 + level,
            CharacterType.Ranger,
            CharacterType.Player)
        {
            level    = Math.Clamp(level, 1, 3);
            internalState = InternalState.SelectingDirection;
            minForce      = 2000.0f;
            maxForce      = minForce + 1000.0f + 2000.0f * level;
            damage        = 3.0f + level * 2f;
        }

        public override void Init()
        {
            internalState = InternalState.SelectingDirection;

            ZigZagArrowMinigame.Init(0.0f, 1.0f, 0.0f);
            ChargeBarMinigame.Init(1.0f, 1.0f, Color.Blue, Color.Cyan,
                minForce, maxForce);
        }

        public override SkillState Update(GameWorld world, Entity target)
        {
            if(internalState == InternalState.SelectingDirection)
            {
                if(ZigZagArrowMinigame.Update(world.Dt, target, world.Camera) == 
                    MinigameState.Finished)
                {
                    internalState = InternalState.ChargingPower;
                }

                return SkillState.Executing;
            }
            else
            {
                if(ChargeBarMinigame.Update(world.Dt) == MinigameState.Finished)
                {
                    Entity arrow = world.EntityFactory.CreateAttack(AttackType.PullingArrow,
                        target.Position, damage, ChargeBarMinigame.Value, 
                        CollisionBitmask.Enemy);
                    AIUtil.SetProjectilePosAndVel(world.EntityManager, arrow,
                        target.Position, ZigZagArrowMinigame.Direction, 125.0f);

                    return SkillState.Finished;
                }

                return SkillState.Executing;
            }
        }

        public override void Draw(GameWorld world,
            SpriteBatch spriteBatch, Entity target)
        {
            ZigZagArrowMinigame.Draw(spriteBatch, target.Position,
                8.0f, 16.0f, new Color(255, 255, 255, 128));

            if(internalState == InternalState.ChargingPower)
            {
                ChargeBarMinigame.Draw(spriteBatch, target.Position,
                    CHARGE_BAR_LENGTH, CHARGE_BAR_HEIGHT, Color.White);
            }
        }
    }

    #endregion

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
                    AttackType.Arrow, position, 10.0f, 2000.0f, 
                    CollisionBitmask.Enemy);

                AIUtil.SetProjectileVelocity(world.EntityManager, projectile,
                    currentDirection);
                
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
            //ZigZagChargeArrowMinigame.Init(6, MathHelper.ToRadians(45.0f), 1.0f, 
            //    1.0f, 16.0f, 48.0f, Color.Yellow, Color.Red);
            //ChargeCircleMinigame.Init(1.0f, 2.0f, Color.Yellow, Color.Red,
            //    16.0f, 32.0f);
        }

        public override SkillState Update(GameWorld world, Entity target)
        {
            //MinigameState minigameState = ChargeBarMinigame.Update(dt);
            //if (minigameState == MinigameState.Finished)
            //{
            //    return SkillState.Finished;
            //}
            //ZigZagChargeArrowMinigame.Update(world.Dt, target, world.Camera);

            world.EntityFactory.CreateAttack(AttackType.SwordStab, target.Position,
                100.0f, 10000.0f, CollisionBitmask.Enemy);

            return SkillState.Finished;
        }

        public override void Draw(GameWorld world, 
            SpriteBatch spriteBatch, Entity target)
        {
            //ChargeBarMinigame.Draw(spriteBatch, target.Position, 48.0f, 8.0f, 
            //    Color.White);
            //ZigZagChargeArrowMinigame.Draw(spriteBatch, target.Position, 16.0f, 32.0f, 
            //    32.0f, 8.0f, new Color(255, 255, 255, 128), Color.White);
            //ChargeCircleMinigame.Draw(spriteBatch, target.Position, 1.0f,
            //    Color.White, 32);
        }
    }

    public class DamageEntityPSkill : PlayerSkill
    {
        public float Damage;

        public DamageEntityPSkill(float damage) : base(new Rectangle(32, 0, 32, 32),
            CharacterType.AllTypes,
            CharacterType.AllTypes)
        {
            Damage = damage;
        }

        public override SkillState Update(GameWorld world, Entity target)
        {
            if (world.EntityManager.TryGetComponent(target, out HealthCmp health))
                health.CurrentHealth -= Damage;

            return SkillState.Finished;
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
