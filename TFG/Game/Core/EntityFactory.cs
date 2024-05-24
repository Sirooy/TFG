using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Engine.Core;
using Engine.Debug;
using Engine.Ecs;
using AI;
using Cmps;
using Physics;
using System.IO;

namespace Core
{
    public enum EnemyType
    {
        BlackBat,
        BrownBat,
        Skeleton,
        Goblin,
        RedDragon,
        WizardGoblin,
        Enemy3
    }

    public enum AttackType
    {
        Fireball,
        Waterball,
        LightningBall,
        Arrow,
        HealingArrow,
        PullingArrow,
        SwordSpin,
        SwordStab
    }

    public enum PlayerType
    {
        Warrior,
        Mage,
        Ranger,
        NumTypes
    }

    public enum EffectType
    {
        Slash,
        Health
    }

    public enum EntityType
    {
        Player,
        Enemy,
        Attack,
        Effect
    }

    public class EntityFactory
    {
        private Dictionary<EnemyType, 
            Func<Vector2, Entity>> 
            createEnemyFunctions;
        private Dictionary<AttackType,
            Func<Vector2, float, float, CollisionBitmask, Entity>>
            createAttackFunctions;
        private Dictionary<PlayerType,
            Func<Vector2, Entity>>
            createPlayerFunctions;
        private Dictionary<EffectType,
            Func<Vector2, Entity>>
            createEffectFunctions;

        private EntityManager<Entity> entityManager;
        private ContentManager content;
        private AnimationLoader animationLoader;

        public EntityFactory(EntityManager<Entity> entityManager, 
            ContentManager content)
        {
            this.entityManager   = entityManager;
            this.content         = content;
            this.animationLoader = new AnimationLoader();

            CreateFunctionDictionaries();
        }

        private void CreateFunctionDictionaries()
        {
            createEnemyFunctions = new Dictionary<EnemyType,
                Func<Vector2, Entity>>
            {
                { EnemyType.BlackBat,     CreateEnemyBlackBat     },
                { EnemyType.BrownBat,     CreateEnemyBrownBat     },
                { EnemyType.Skeleton,     CreateEnemySkeleton     },
                { EnemyType.RedDragon,    CreateEnemyRedDragon    },
                { EnemyType.Goblin,       CreateEnemyGoblin       },
                { EnemyType.WizardGoblin, CreateEnemyWizardGoblin }
            };

            createAttackFunctions = new Dictionary<AttackType,
                Func<Vector2, float, float, CollisionBitmask, Entity>>
            {
                { AttackType.Fireball,      CreateAttackFireball      },
                { AttackType.Waterball,     CreateAttackWaterBall     },
                { AttackType.LightningBall, CreateAttackLightningBall },
                { AttackType.Arrow,         CreateAttackArrow         },
                { AttackType.HealingArrow,  CreateAttackHealingArrow  },
                { AttackType.PullingArrow,  CreateAttackPullingArrow  },
                { AttackType.SwordSpin,     CreateAttackSwordSpin     },
                { AttackType.SwordStab,     CreateAttackSwordStab     },
            };

            createPlayerFunctions = new Dictionary<PlayerType,
                Func<Vector2, Entity>>
            {
                { PlayerType.Warrior, CreatePlayerWarrior },
                { PlayerType.Mage,    CreatePlayerMage    },
                { PlayerType.Ranger,  CreatePlayerRanger  },
                //{ PlayerType.Paladin, CreatePlayerPaladin }
            };

            createEffectFunctions = new Dictionary<EffectType,
                Func<Vector2, Entity>>
            {
                { EffectType.Slash, CreateEffectSlash },
                { EffectType.Health, CreateEffectHealth }
            };
        }

        public Entity CreateEnemy(EnemyType type, Vector2 position)
        {
            DebugAssert.Success(createEnemyFunctions.ContainsKey(type),
                "Enemy type not found");

            return createEnemyFunctions[type](position);
        }

        public Entity CreatePlayer(PlayerType type, Vector2 position)
        {
            DebugAssert.Success(createPlayerFunctions.ContainsKey(type),
                "Player type not found");

            return createPlayerFunctions[type](position);
        }

        public Entity CreateAttack(AttackType type, Vector2 position, 
            float damage, float knockback, CollisionBitmask mask)
        {
            DebugAssert.Success(createAttackFunctions.ContainsKey(type),
                "Attack type not found");

            return createAttackFunctions[type](position, damage, knockback, mask);
        }

        public Entity CreateEffect(EffectType type, Vector2 position)
        {
            DebugAssert.Success(createEffectFunctions.ContainsKey(type),
                "Effect type not found");

            return createEffectFunctions[type](position);
        }

        #region Enemies
        private Entity CreateEnemyBlackBat(Vector2 position)
        {
            Entity e = CreateEnemyBaseEntity(position, 
                "BlackBatSpriteSheet",
                CharacterType.Enemy | CharacterType.Normal, 
                CharacterType.Normal, 15.0f);

            AICmp ai = entityManager.AddComponent(e, new AICmp());
            ai.DecisionTree = new BinaryDecisionNode(
                new NearestEntitySelector(EntityTags.Player),
                new IsNearCondition(30.0f),
                new MeleeAttackESkill(10.0f, Color.Red),
                new StraightDashESkill(100.0f));

            return e;
        }

        private Entity CreateEnemyBrownBat(Vector2 position)
        {
            Entity e = CreateEnemyBaseEntity(position,
                "BrownBatSpriteSheet",
                CharacterType.Enemy | CharacterType.Normal,
                CharacterType.Normal, 15.0f);

            AICmp ai = entityManager.AddComponent(e, new AICmp());
            ai.DecisionTree = new BinaryDecisionNode(
                new NearestEntitySelector(EntityTags.Player),
                new IsNearCondition(50.0f),
                new MeleeAttackESkill(10.0f, Color.Brown),
                new DashESkill(120.0f));

            return e;
        }

        private Entity CreateEnemySkeleton(Vector2 position)
        {
            Entity e = CreateEnemyBaseEntity(position,
                "SkeletonSpriteSheet",
                CharacterType.Enemy | CharacterType.Normal,
                CharacterType.Normal, 20.0f);

            AICmp ai = entityManager.AddComponent(e, new AICmp());
            ai.DecisionTree = new BinaryDecisionNode(
                new NearestEntitySelector(EntityTags.Player),
                new IsNearCondition(30.0f),
                new MeleeAttackESkill(5.0f, Color.White),
                new PathFollowESkill(50.0f, 120.0f));

            return e;
        }

        private Entity CreateEnemyRedDragon(Vector2 position)
        {
            Entity e = CreateEnemyBaseEntity(position,
                "RedDragonSpriteSheet",
                CharacterType.Enemy | CharacterType.Normal,
                CharacterType.Normal, 30.0f);

            AICmp ai = entityManager.AddComponent(e, new AICmp());
            ai.DecisionTree = new BinaryDecisionNode(
                new LessHealthEntitySelector(EntityTags.Player),
                new HasVisionCondition(CollisionBitmask.Player),
                new ProjectileAttackESkill(AttackType.Fireball, 10.0f, 1000.0f, 150.0f),
                new PathFollowESkill(75.0f, 75.0f));

            return e;
        }

        private Entity CreateEnemyGoblin(Vector2 position)
        {
            Entity e = CreateEnemyBaseEntity(position,
                "GoblinSpriteSheet",
                CharacterType.Enemy | CharacterType.Normal,
                CharacterType.Normal, 20.0f);

            AICmp ai = entityManager.AddComponent(e, new AICmp());
            ai.DecisionTree = new BinaryDecisionNode(
                new NearestEntitySelector(EntityTags.Player),
                new HasVisionCondition(CollisionBitmask.Player),
                    new BinaryDecisionNode(
                        new NearestEntitySelector(EntityTags.Player),
                        new IsNearCondition(50.0f),
                        new AttackESkill(AttackType.SwordSpin, 8.0f, 2000.0f, 50.0f),
                        new StraightDashESkill(120.0f)),
                new PathFollowESkill(60.0f, 90.0f));

            return e;
        }

        private Entity CreateEnemyWizardGoblin(Vector2 position)
        {
            Entity e = CreateEnemyBaseEntity(position,
                "WizardGoblinSpriteSheet",
                CharacterType.Enemy | CharacterType.Normal,
                CharacterType.Normal, 40.0f);

            AICmp ai = entityManager.AddComponent(e, new AICmp());
            ai.DecisionTree = new BinaryDecisionNode(
                new LessHealthEntitySelector(EntityTags.Enemy),
                new HasLessThanPercentHealth(0.7f),
                    new BinaryDecisionNode(
                        new LessHealthEntitySelector(EntityTags.Enemy),
                        new IsNearCondition(80.0f),
                        new HealESkill(10.0f),
                        new TeleportToTargetESkill(100.0f)),
                new TargetChangerDecisionNode(
                    new RandomEntitySelector(EntityTags.Player),
                    new ProjectileAttackESkill(AttackType.Waterball, 10.0f, 500.0f, 120.0f)));

            return e;
        }

        #endregion

        #region Attacks
        private Entity CreateAttackFireball(Vector2 position, 
            float damage, float knockback, CollisionBitmask mask)
        {
            Texture2D texture = content.Load<Texture2D>(
                GameContent.TexturePath("FireballSpriteSheet"));

            Entity e = entityManager.CreateEntity();
            e.Position = position;
            e.AddTag(EntityTags.Attack);

            DeathCmp death = entityManager.AddComponent(e, new DeathCmp());
            death.OnEnterDeath = (GameWorld world, Entity entity) =>
            {
                AnimationControllerCmp anim = world.EntityManager.
                    GetComponent<AnimationControllerCmp>(entity);
                anim.Play("Death", AnimationPlayState.None);
            };
            death.OnDying = (GameWorld world, Entity entity, float dt) =>
            {
                EntityManager<Entity> entityManager = world.EntityManager;
                AnimationControllerCmp anim = entityManager.
                    GetComponent<AnimationControllerCmp>(entity);

                if (anim.AnimationHasFinished)
                    return DyingState.Kill;
                else
                    return DyingState.KeepAlive;
            };

            PhysicsCmp phy    = entityManager.AddComponent(e, new PhysicsCmp());
            phy.Inertia       = 0.0f;
            phy.LinearDamping = 0.0f;
            TriggerColliderCmp col = entityManager.AddComponent(e, new TriggerColliderCmp(
                new RectangleCollider(20.0f, 6.0f),
                CollisionBitmask.Attack, mask | CollisionBitmask.Wall));
            col.OnTriggerEnter += (Entity e1, TriggerColliderCmp c1, 
                                   Entity e2, ColliderBody c2, 
                                   ColliderType type, in Manifold manifold) =>
            {
                DeathCmp death = entityManager.GetComponent<DeathCmp>(e1);
                if (death.State != DeathState.Alive) return;

                if (type != ColliderType.Static)
                {
                    if(entityManager.TryGetComponent(e2, out HealthCmp health))
                    {
                        health.CurrentHealth -= damage;
                    }

                    if(entityManager.TryGetComponent(e2, out PhysicsCmp physics))
                    {
                        physics.Force += -manifold.Normal * knockback;
                    }
                }

                c1.CollisionLayer  = CollisionBitmask.None;
                c1.CollisionMask   = CollisionBitmask.None;
                phy.LinearVelocity = Vector2.Zero;
                death.Kill();
            };

            AnimationControllerCmp anim = entityManager.AddComponent(e,
                new AnimationControllerCmp(0));
            anim.AddAnimations(animationLoader.Load(
                GameContent.AnimationPath("Fireball.anim")));
            anim.Play("Default");

            SpriteCmp spr = entityManager.AddComponent(e,
                new SpriteCmp(texture));
            spr.SourceRect = anim.GetCurrentFrameSource();
            spr.LayerOrder = LayerOrder.Ordered;
            spr.Origin = new Vector2(
                spr.SourceRect.Value.Width * 0.5f,
                spr.SourceRect.Value.Height * 0.5f);
            //spr.Transform.LocalPosition = new Vector2(-10.0f, 0.0f);

            return e;
        }

        private Entity CreateAttackWaterBall(Vector2 position,
            float damage, float knockback, CollisionBitmask mask)
        {
            Texture2D texture = content.Load<Texture2D>(
                GameContent.TexturePath("WaterballSpriteSheet"));

            Entity e = entityManager.CreateEntity();
            e.Position = position;
            e.AddTag(EntityTags.Attack);

            DeathCmp death = entityManager.AddComponent(e, new DeathCmp());
            death.OnEnterDeath = (GameWorld world, Entity entity) =>
            {
                AnimationControllerCmp anim = world.EntityManager.
                    GetComponent<AnimationControllerCmp>(entity);
                anim.Play("Death", AnimationPlayState.None);
            };
            death.OnDying = (GameWorld world, Entity entity, float dt) =>
            {
                EntityManager<Entity> entityManager = world.EntityManager;
                AnimationControllerCmp anim = entityManager.
                    GetComponent<AnimationControllerCmp>(entity);

                if (anim.AnimationHasFinished)
                    return DyingState.Kill;
                else
                    return DyingState.KeepAlive;
            };

            PhysicsCmp phy = entityManager.AddComponent(e, new PhysicsCmp());
            phy.Inertia = 0.0f;
            phy.LinearDamping = 0.0f;
            TriggerColliderCmp col = entityManager.AddComponent(e, new TriggerColliderCmp(
                new CircleCollider(14.0f * 0.5f),
                CollisionBitmask.Attack, mask | CollisionBitmask.Wall));
            int bouncesLeft = 2;
            col.OnTriggerEnter += (Entity e1, TriggerColliderCmp c1,
                                   Entity e2, ColliderBody c2,
                                   ColliderType type, in Manifold manifold) =>
            {
                DeathCmp death = entityManager.GetComponent<DeathCmp>(e1);
                if (death.State != DeathState.Alive) return;

                if (type == ColliderType.Static)
                {
                    if(bouncesLeft > 0)
                    {
                        bouncesLeft--;
                        phy.LinearVelocity = Vector2.Reflect(phy.LinearVelocity,
                            manifold.Normal);
                    }
                    else
                    {
                        c1.CollisionLayer = CollisionBitmask.None;
                        c1.CollisionMask = CollisionBitmask.None;
                        phy.LinearVelocity = Vector2.Zero;
                        death.Kill();
                    }
                }
                else
                {
                    if (entityManager.TryGetComponent(e2, out HealthCmp health))
                    {
                        health.CurrentHealth -= damage;
                    }

                    if (entityManager.TryGetComponent(e2, out PhysicsCmp physics))
                    {
                        physics.Force += -manifold.Normal * knockback;
                    }

                    c1.CollisionLayer = CollisionBitmask.None;
                    c1.CollisionMask = CollisionBitmask.None;
                    phy.LinearVelocity = Vector2.Zero;
                    death.Kill();
                }
            };

            AnimationControllerCmp anim = entityManager.AddComponent(e,
                new AnimationControllerCmp(0));
            anim.AddAnimations(animationLoader.Load(
                GameContent.AnimationPath("Waterball.anim")));
            anim.Play("Default");

            SpriteCmp spr = entityManager.AddComponent(e,
                new SpriteCmp(texture));
            spr.SourceRect = anim.GetCurrentFrameSource();
            spr.LayerOrder = LayerOrder.Ordered;
            spr.Origin = new Vector2(
                spr.SourceRect.Value.Width * 0.5f,
                spr.SourceRect.Value.Height * 0.5f);

            return e;
        }

        private Entity CreateAttackLightningBall(Vector2 position,
            float damage, float knockback, CollisionBitmask mask)
        {
            Texture2D texture = content.Load<Texture2D>(
                GameContent.TexturePath("LightningballSpriteSheet"));

            Entity e = entityManager.CreateEntity();
            e.Position = position;
            e.AddTag(EntityTags.Attack);

            DeathCmp death = entityManager.AddComponent(e, new DeathCmp());
            death.OnEnterDeath = (GameWorld world, Entity entity) =>
            {
                AnimationControllerCmp anim = world.EntityManager.
                    GetComponent<AnimationControllerCmp>(entity);
                anim.Play("Death", AnimationPlayState.None);
            };
            death.OnDying = (GameWorld world, Entity entity, float dt) =>
            {
                EntityManager<Entity> entityManager = world.EntityManager;
                AnimationControllerCmp anim = entityManager.
                    GetComponent<AnimationControllerCmp>(entity);

                if (anim.AnimationHasFinished)
                    return DyingState.Kill;
                else
                    return DyingState.KeepAlive;
            };

            PhysicsCmp phy = entityManager.AddComponent(e, new PhysicsCmp());
            phy.Inertia = 0.0f;
            phy.LinearDamping = 0.0f;
            TriggerColliderCmp col = entityManager.AddComponent(e, new TriggerColliderCmp(
                new CircleCollider(16.0f * 0.5f),
                CollisionBitmask.Attack, mask | CollisionBitmask.Wall));
            col.OnTriggerEnter += (Entity e1, TriggerColliderCmp c1,
                                   Entity e2, ColliderBody c2,
                                   ColliderType type, in Manifold manifold) =>
            {
                DeathCmp death = entityManager.GetComponent<DeathCmp>(e1);
                if (death.State != DeathState.Alive) return;

                if (type != ColliderType.Static)
                {
                    if (entityManager.TryGetComponent(e2, out HealthCmp health))
                    {
                        health.CurrentHealth -= damage;
                    }

                    if (entityManager.TryGetComponent(e2, out PhysicsCmp physics))
                    {
                        physics.Force += -manifold.Normal * knockback;
                    }

                    entityManager.ForEachComponent((Entity other, HealthCmp health) =>
                    {
                        if (other == e2) return;

                        if(entityManager.TryGetComponent(other, out ColliderCmp col))
                        {
                            if((col.CollisionLayer & mask) != CollisionBitmask.None &&
                                Vector2.DistanceSquared(e2.Position, other.Position) <= 64.0f * 64.0f)
                            {
                                health.CurrentHealth -= damage * 0.4f;
                                Entity slash = this.CreateEffectSlash(other.Position);
                                SpriteCmp slashSpr = entityManager.GetComponent<SpriteCmp>(slash);
                                slashSpr.Color = Color.Yellow;
                            }
                        }
                    });
                }

                c1.CollisionLayer = CollisionBitmask.None;
                c1.CollisionMask = CollisionBitmask.None;
                phy.LinearVelocity = Vector2.Zero;
                death.Kill();
            };

            AnimationControllerCmp anim = entityManager.AddComponent(e,
                new AnimationControllerCmp(0));
            anim.AddAnimations(animationLoader.Load(
                GameContent.AnimationPath("Lightningball.anim")));
            anim.Play("Default");

            SpriteCmp spr = entityManager.AddComponent(e,
                new SpriteCmp(texture));
            spr.SourceRect = anim.GetCurrentFrameSource();
            spr.LayerOrder = LayerOrder.Ordered;
            spr.Origin = new Vector2(
                spr.SourceRect.Value.Width * 0.5f,
                spr.SourceRect.Value.Height * 0.5f);

            return e;
        }

        private Entity CreateAttackArrow(Vector2 position, 
            float damage, float knockback, CollisionBitmask mask)
        {
            Entity e = CreateAttackArrowBase(position, mask);

            TriggerColliderCmp col = entityManager.GetComponent<TriggerColliderCmp>(e);
            col.OnTriggerEnter += (Entity e1, TriggerColliderCmp c1,
                                   Entity e2, ColliderBody c2,
                                   ColliderType type, in Manifold manifold) =>
            {
                DeathCmp death = entityManager.GetComponent<DeathCmp>(e1);
                if (death.State != DeathState.Alive) return;
                PhysicsCmp phy = entityManager.GetComponent<PhysicsCmp>(e1);

                if (type != ColliderType.Static)
                {
                    if (entityManager.TryGetComponent(e2, out HealthCmp health))
                    {
                        health.CurrentHealth -= damage;
                    }

                    if (entityManager.TryGetComponent(e2, out PhysicsCmp physics))
                    {
                        physics.Force += -manifold.Normal * knockback;
                    }
                }
                else
                {
                    c1.CollisionLayer = CollisionBitmask.None;
                    c1.CollisionMask = CollisionBitmask.None;
                    phy.LinearVelocity = Vector2.Zero;
                    death.Kill();
                }
            };

            return e;
        }

        private Entity CreateAttackHealingArrow(Vector2 position,
            float damage, float knockback, CollisionBitmask mask)
        {
            Entity e = CreateAttackArrowBase(position, mask);

            TriggerColliderCmp col = entityManager.GetComponent<TriggerColliderCmp>(e);
            col.OnTriggerEnter += (Entity e1, TriggerColliderCmp c1,
                                   Entity e2, ColliderBody c2,
                                   ColliderType type, in Manifold manifold) =>
            {
                DeathCmp death = entityManager.GetComponent<DeathCmp>(e1);
                if (death.State != DeathState.Alive) return;
                PhysicsCmp phy = entityManager.GetComponent<PhysicsCmp>(e1);

                if (type != ColliderType.Static)
                {
                    if (entityManager.TryGetComponent(e2, out HealthCmp health))
                    {
                        health.CurrentHealth += damage;
                    }

                    if (entityManager.TryGetComponent(e2, out PhysicsCmp physics))
                    {
                        physics.Force += -manifold.Normal * knockback;
                    }

                    this.CreateEffect(EffectType.Health, e2.Position);
                }

                c1.CollisionLayer  = CollisionBitmask.None;
                c1.CollisionMask   = CollisionBitmask.None;
                phy.LinearVelocity = Vector2.Zero;
                death.Kill();
            };

            SpriteCmp spr = entityManager.GetComponent<SpriteCmp>(e);
            spr.Color     = new Color(0.4f, 1.0f, 0.4f);

            return e;
        }

        private Entity CreateAttackPullingArrow(Vector2 position,
            float damage, float knockback, CollisionBitmask mask)
        {
            Entity e = CreateAttackArrowBase(position, mask);

            TriggerColliderCmp col = entityManager.GetComponent<TriggerColliderCmp>(e);
            col.OnTriggerEnter += (Entity e1, TriggerColliderCmp c1,
                                   Entity e2, ColliderBody c2,
                                   ColliderType type, in Manifold manifold) =>
            {
                DeathCmp death = entityManager.GetComponent<DeathCmp>(e1);
                if (death.State != DeathState.Alive) return;
                PhysicsCmp phy = entityManager.GetComponent<PhysicsCmp>(e1);

                if (type != ColliderType.Static)
                {
                    if (entityManager.TryGetComponent(e2, out HealthCmp health))
                    {
                        health.CurrentHealth -= damage;
                    }

                    if (entityManager.TryGetComponent(e2, out PhysicsCmp physics))
                    {
                        physics.Force += manifold.Normal * knockback;
                    }
                }

                c1.CollisionLayer  = CollisionBitmask.None;
                c1.CollisionMask   = CollisionBitmask.None;
                phy.LinearVelocity = Vector2.Zero;
                death.Kill();
            };

            SpriteCmp spr = entityManager.GetComponent<SpriteCmp>(e);
            spr.Color = new Color(0.4f, 0.4f, 1.0f);

            return e;
        }

        private Entity CreateAttackArrowBase(Vector2 position, 
            CollisionBitmask mask)
        {
            Texture2D texture = content.Load<Texture2D>(
                GameContent.TexturePath("ArrowSpriteSheet"));

            Entity e = entityManager.CreateEntity();
            e.Position = position;
            e.AddTag(EntityTags.Attack);

            DeathCmp death = entityManager.AddComponent(e, new DeathCmp());
            death.OnEnterDeath = (GameWorld world, Entity entity) =>
            {
                AnimationControllerCmp anim = world.EntityManager.
                    GetComponent<AnimationControllerCmp>(entity);
                anim.Play("Death", AnimationPlayState.None);
            };
            death.OnDying = (GameWorld world, Entity entity, float dt) =>
            {
                EntityManager<Entity> entityManager = world.EntityManager;
                AnimationControllerCmp anim = entityManager.
                    GetComponent<AnimationControllerCmp>(entity);

                if (anim.AnimationHasFinished)
                    return DyingState.Kill;
                else
                    return DyingState.KeepAlive;
            };

            PhysicsCmp phy = entityManager.AddComponent(e, new PhysicsCmp());
            phy.Inertia = 0.0f;
            phy.LinearDamping = 0.0f;
            TriggerColliderCmp col = entityManager.AddComponent(e, new TriggerColliderCmp(
                new RectangleCollider(30.0f, 6.0f),
                CollisionBitmask.Attack, mask | CollisionBitmask.Wall));

            AnimationControllerCmp anim = entityManager.AddComponent(e,
                new AnimationControllerCmp(0));
            anim.AddAnimations(animationLoader.Load(
                GameContent.AnimationPath("Arrow.anim")));
            anim.Play("Default");

            SpriteCmp spr = entityManager.AddComponent(e,
                new SpriteCmp(texture));
            spr.SourceRect = anim.GetCurrentFrameSource();
            spr.LayerOrder = LayerOrder.Ordered;
            spr.Origin = new Vector2(
                spr.SourceRect.Value.Width * 0.5f,
                spr.SourceRect.Value.Height * 0.5f);

            return e;
        }

        private Entity CreateAttackSwordSpin(Vector2 position,
            float damage, float knockback, CollisionBitmask mask)
        {
            Texture2D texture = content.Load<Texture2D>(
                GameContent.TexturePath("SwordSpinSpriteSheet"));

            Entity e = entityManager.CreateEntity();
            e.Position = position;
            e.AddTag(EntityTags.Attack);

            DeathCmp death = entityManager.AddComponent(e, new DeathCmp());
            death.OnDying = (GameWorld world, Entity entity, float dt) =>
            {
                EntityManager<Entity> entityManager = world.EntityManager;
                AnimationControllerCmp anim = entityManager.
                    GetComponent<AnimationControllerCmp>(entity);

                if (anim.AnimationHasFinished)
                    return DyingState.Kill;
                else
                    return DyingState.KeepAlive;
            };

            TriggerColliderCmp col = entityManager.AddComponent(e, new TriggerColliderCmp(
                new CircleCollider(24.0f),
                CollisionBitmask.Attack, mask));
            col.OnTriggerEnter += (Entity e1, TriggerColliderCmp c1,
                                   Entity e2, ColliderBody c2,
                                   ColliderType type, in Manifold manifold) =>
            {
                if (type != ColliderType.Static)
                {
                    if (entityManager.TryGetComponent(e2, out HealthCmp health))
                    {
                        health.CurrentHealth -= damage;
                    }

                    if (entityManager.TryGetComponent(e2, out PhysicsCmp physics))
                    {
                        physics.Force += -manifold.Normal * knockback;
                    }
                }

            };

            AnimationControllerCmp anim = entityManager.AddComponent(e,
                new AnimationControllerCmp(0));
            anim.AddAnimations(animationLoader.Load(
                GameContent.AnimationPath("SwordSpin.anim")));
            anim.Play("Default");

            SpriteCmp spr = entityManager.AddComponent(e,
                new SpriteCmp(texture));
            spr.SourceRect = anim.GetCurrentFrameSource();
            spr.LayerOrder = LayerOrder.Ordered;
            spr.Origin     = new Vector2(
                spr.SourceRect.Value.Width * 0.5f,
                spr.SourceRect.Value.Height * 0.5f);

            death.Kill();

            return e;
        }

        private Entity CreateAttackSwordStab(Vector2 position,
            float damage, float knockback, CollisionBitmask mask)
        {
            Texture2D texture = content.Load<Texture2D>(
                GameContent.TexturePath("SwordStabSpriteSheet"));

            Entity e = entityManager.CreateEntity();
            e.Position = position;
            e.AddTag(EntityTags.Attack);

            DeathCmp death = entityManager.AddComponent(e, new DeathCmp());
            death.OnDying = (GameWorld world, Entity entity, float dt) =>
            {
                EntityManager<Entity> entityManager = world.EntityManager;
                AnimationControllerCmp anim = entityManager.
                    GetComponent<AnimationControllerCmp>(entity);

                if (anim.AnimationHasFinished)
                    return DyingState.Kill;
                else
                    return DyingState.KeepAlive;
            };

            TriggerColliderCmp col = entityManager.AddComponent(e, new TriggerColliderCmp(
                new RectangleCollider(36.0f, 10.0f),
                CollisionBitmask.Attack, mask));
            col.OnTriggerEnter += (Entity e1, TriggerColliderCmp c1,
                                   Entity e2, ColliderBody c2,
                                   ColliderType type, in Manifold manifold) =>
            {
                if (type != ColliderType.Static)
                {
                    if (entityManager.TryGetComponent(e2, out HealthCmp health))
                    {
                        health.CurrentHealth -= damage;
                    }

                    if (entityManager.TryGetComponent(e2, out PhysicsCmp physics))
                    {
                        physics.Force += -manifold.Normal * knockback;
                    }
                }

            };

            AnimationControllerCmp anim = entityManager.AddComponent(e,
                new AnimationControllerCmp(0));
            anim.AddAnimations(animationLoader.Load(
                GameContent.AnimationPath("SwordStab.anim")));
            anim.Play("Default");

            SpriteCmp spr = entityManager.AddComponent(e,
                new SpriteCmp(texture));
            spr.SourceRect = anim.GetCurrentFrameSource();
            spr.LayerOrder = LayerOrder.Ordered;
            spr.Origin = new Vector2(
                spr.SourceRect.Value.Width * 0.5f,
                spr.SourceRect.Value.Height * 0.5f);

            death.Kill();

            return e;
        }

        #endregion

        #region Player

        private Entity CreatePlayerWarrior(Vector2 position)
        {
            Entity e = CreatePlayerBaseEntity(position, 
                "WarriorSpriteSheet", 
                CharacterType.Player | CharacterType.Normal | CharacterType.Warrior,
                CharacterType.Player | CharacterType.Normal | CharacterType.Warrior, 
                40.0f);

            return e;
        }

        private Entity CreatePlayerMage(Vector2 position)
        {
            Entity e = CreatePlayerBaseEntity(position, 
                "MageSpriteSheet",
                CharacterType.Player | CharacterType.Normal | CharacterType.Mage,
                CharacterType.Player | CharacterType.Normal | CharacterType.Mage, 
                20.0f);

            return e;
        }

        private Entity CreatePlayerRanger(Vector2 position)
        {
            Entity e = CreatePlayerBaseEntity(position,
                "RangerSpriteSheet",
                CharacterType.Player | CharacterType.Normal | CharacterType.Ranger,
                CharacterType.Player | CharacterType.Normal | CharacterType.Ranger,
                30.0f);

            return e;
        }

        private Entity CreatePlayerPaladin(Vector2 position)
        {
            Entity e = CreatePlayerBaseEntity(position,
                "PaladinSpriteSheet",
                CharacterType.Player | CharacterType.Normal | CharacterType.Paladin,
                CharacterType.Player | CharacterType.Normal | CharacterType.Paladin,
                40.0f);

            return e;
        }

        #endregion

        #region Effect

        private Entity CreateEffectSlash(Vector2 position)
        {
            return CreateEffectBasetEntity(position, 
                "SlashSpriteSheet", "Slash.anim");
        }

        private Entity CreateEffectHealth(Vector2 position)
        {
            return CreateEffectBasetEntity(position,
                "HealthEffectSpriteSheet", "HealthEffect.anim");
        }

        #endregion

        #region Common
        private HealthCmp AddHealthCmp(Entity e, float maxHealth)
        {
            HealthCmp health = entityManager.AddComponent(e, new HealthCmp(maxHealth));
            health.Texture   = content.Load<Texture2D>(
                GameContent.TexturePath("GameplayUI"));
            health.HealthBorderSourceRect  = new Rectangle(
                0, 32, 64, 16);
            health.CurrentHealthSourceRect = new Rectangle(
                0, 48, 48, 16);

            return health;
        }

        private DeathCmp AddCharacterDeathCmp(Entity e)
        {
            DeathCmp death = entityManager.AddComponent(e, new DeathCmp());
            death.OnEnterDeath = (GameWorld world, Entity entity) =>
            {
                AnimationControllerCmp anim = world.EntityManager.
                    GetComponent<AnimationControllerCmp>(entity);
                anim.Play("Death", AnimationPlayState.None);

                SpriteCmp spr = world.EntityManager.
                    GetComponent<SpriteCmp>(entity);
                spr.Color     = Color.Red;
            };

            const float DEATH_TIME = 1.0f;
            float deathTimer       = DEATH_TIME;
            death.OnDying = (GameWorld world, Entity entity, float dt) =>
            {
                EntityManager<Entity> entityManager = world.EntityManager;
                AnimationControllerCmp anim = entityManager.
                    GetComponent<AnimationControllerCmp>(entity);

                if (anim.AnimationHasFinished)
                {
                    deathTimer -= dt;

                    if (deathTimer <= 0.0f)
                        return DyingState.Kill;
                    else
                    {
                        SpriteCmp spr      = entityManager.
                            GetComponent<SpriteCmp>(entity);
                        CharacterCmp chara = entityManager.
                            GetComponent<CharacterCmp>(entity);
                        byte alpha = (byte)((deathTimer / DEATH_TIME) * byte.MaxValue);

                        spr.Color.A   = alpha;
                        chara.Color.A = alpha;

                        return DyingState.KeepAlive;
                    }
                }
                else
                    return DyingState.KeepAlive;
            };

            return death;
        }

        private Entity CreateEnemyBaseEntity(Vector2 position, 
            string textureName, CharacterType type, CharacterType skills,
            float health)
        {
            Texture2D platformTexture = content.Load<Texture2D>(
                GameContent.TexturePath("EntityPlatform"));
            Texture2D spriteTexture   = content.Load<Texture2D>(
                GameContent.TexturePath(textureName));

            Entity e   = entityManager.CreateEntity();
            e.Position = position;
            e.AddTag(EntityTags.Enemy);

            //Physics
            PhysicsCmp phy    = entityManager.AddComponent(e, new PhysicsCmp());
            phy.Inertia       = 0.0f;
            phy.LinearDamping = 1.5f;

            //Collision
            entityManager.AddComponent(e, new ColliderCmp(
                new CircleCollider(8.0f), new Material(1.0f, 0.0f, 0.0f),
                CollisionBitmask.Enemy, CollisionBitmask.All));

            //Animation
            AnimationControllerCmp anim = entityManager.AddComponent(e,
                new AnimationControllerCmp(0));
            anim.AddAnimations(animationLoader.Load(
                GameContent.AnimationPath("Character.anim")));
            anim.Play("Idle");

            SpriteCmp spr  = entityManager.AddComponent(e,
                new SpriteCmp(spriteTexture));
            spr.SourceRect = anim.GetCurrentFrameSource();
            spr.LayerOrder = LayerOrder.Ordered;
            spr.Origin     = new Vector2(
                spr.SourceRect.Value.Width * 0.5f,
                spr.SourceRect.Value.Height);

            CharacterCmp charCmp = entityManager.AddComponent(e,
                new CharacterCmp(platformTexture, type, skills));
            charCmp.PlatformSourceRect = new Rectangle(16, 0, 16, 16);
            charCmp.SelectSourceRect   = new Rectangle(32, 0, 20, 20);

            AddHealthCmp(e, health);
            AddCharacterDeathCmp(e);

            return e;
        }

        private Entity CreatePlayerBaseEntity(Vector2 position, 
            string textureName, CharacterType type, CharacterType skills,
            float health)
        {
            Texture2D platformTexture = content.Load<Texture2D>(
                GameContent.TexturePath("EntityPlatform"));
            Texture2D spriteTexture = content.Load<Texture2D>(
                GameContent.TexturePath(textureName));

            Entity e = entityManager.CreateEntity();
            e.Position = position;
            e.AddTag(EntityTags.Player);

            //Physics
            PhysicsCmp phy = entityManager.AddComponent(e, new PhysicsCmp());
            phy.Inertia = 0.0f;
            phy.LinearDamping = 1.5f;

            //Collision
            entityManager.AddComponent(e, new ColliderCmp(
                new CircleCollider(8.0f), new Material(1.0f, 0.0f, 0.0f),
                CollisionBitmask.Player, CollisionBitmask.All));

            //Animation
            AnimationControllerCmp anim = entityManager.AddComponent(e,
                new AnimationControllerCmp(0));
            anim.AddAnimations(animationLoader.Load(
                GameContent.AnimationPath("Character.anim")));
            anim.Play("Idle");

            SpriteCmp spr = entityManager.AddComponent(e,
                new SpriteCmp(spriteTexture));
            spr.SourceRect = anim.GetCurrentFrameSource();
            spr.LayerOrder = LayerOrder.Ordered;
            spr.Origin = new Vector2(
                spr.SourceRect.Value.Width * 0.5f,
                spr.SourceRect.Value.Height);

            CharacterCmp charCmp = entityManager.AddComponent(e,
                new CharacterCmp(platformTexture, type, skills));
            charCmp.PlatformSourceRect = new Rectangle(0, 0, 16, 16);
            charCmp.SelectSourceRect   = new Rectangle(32, 0, 20, 20);

            AddHealthCmp(e, health);
            AddCharacterDeathCmp(e);

            return e;
        }

        private Entity CreateEffectBasetEntity(Vector2 position, 
            string textureName, string animationName)
        {
            Texture2D spriteTexture = content.Load<Texture2D>(
                GameContent.TexturePath(textureName));

            Entity e   = entityManager.CreateEntity();
            e.Position = position;

            //Animation
            AnimationControllerCmp anim = entityManager.AddComponent(e,
                new AnimationControllerCmp(0));
            anim.AddAnimations(animationLoader.Load(
                GameContent.AnimationPath(animationName)));
            anim.Play("Default");

            SpriteCmp spr = entityManager.AddComponent(e,
                new SpriteCmp(spriteTexture));
            spr.SourceRect = anim.GetCurrentFrameSource();
            spr.LayerOrder = LayerOrder.AlwaysTop;
            spr.Origin = new Vector2(
                spr.SourceRect.Value.Width * 0.5f,
                spr.SourceRect.Value.Height * 0.5f);

            DeathCmp death = entityManager.AddComponent(e, new DeathCmp());
            death.OnDying = (GameWorld world, Entity entity, float dt) =>
            {
                AnimationControllerCmp anim = world.EntityManager.
                    GetComponent<AnimationControllerCmp>(entity);

                if (anim.AnimationHasFinished)
                    return DyingState.Kill;
                else
                    return DyingState.KeepAlive;
            };
            death.Kill();

            return e;
        }

        private Entity CreateAttackBasetEntity(Vector2 position,
            string textureName, string animationName)
        {
            Texture2D spriteTexture = content.Load<Texture2D>(
                GameContent.TexturePath(textureName));

            Entity e = entityManager.CreateEntity();
            e.Position = position;

            //Animation
            AnimationControllerCmp anim = entityManager.AddComponent(e,
                new AnimationControllerCmp(0));
            anim.AddAnimations(animationLoader.Load(
                GameContent.AnimationPath(animationName)));
            anim.Play("Default");

            SpriteCmp spr = entityManager.AddComponent(e,
                new SpriteCmp(spriteTexture));
            spr.SourceRect = anim.GetCurrentFrameSource();
            spr.LayerOrder = LayerOrder.AlwaysTop;
            spr.Origin = new Vector2(
                spr.SourceRect.Value.Width * 0.5f,
                spr.SourceRect.Value.Height * 0.5f);

            DeathCmp death = entityManager.AddComponent(e, new DeathCmp());
            death.OnDying = (GameWorld world, Entity entity, float dt) =>
            {
                AnimationControllerCmp anim = world.EntityManager.
                    GetComponent<AnimationControllerCmp>(entity);

                if (anim.AnimationHasFinished)
                    return DyingState.Kill;
                else
                    return DyingState.KeepAlive;
            };
            death.Kill();

            return e;
        }

        #endregion
    }
}
