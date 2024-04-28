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

namespace Core
{
    public enum EnemyType
    {
        Enemy1,
        Enemy2,
        Enemy3
    }

    public enum AttackType
    {
        Projectile1
    }

    public enum PlayerType
    {
        Warrior,
        Mage,
        Type3,
        Type4,
        NumTypes
    }

    public enum EntityType
    {
        Player,
        Enemy,
        Attack
    }

    public class EntityFactory
    {
        private Dictionary<EnemyType, 
            Func<Vector2, Entity>> 
            createEnemyFunctions;
        private Dictionary<AttackType,
            Func<Vector2, Entity>>
            createAttackFunctions;
        private Dictionary<PlayerType,
            Func<Vector2, Entity>>
            createPlayerFunctions;

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
                { EnemyType.Enemy1, CreateEnemyEnemy1 }
            };

            createAttackFunctions = new Dictionary<AttackType,
                Func<Vector2, Entity>>
            {
                { AttackType.Projectile1, CreateAttackProjectile1 }
            };

            createPlayerFunctions = new Dictionary<PlayerType,
                Func<Vector2, Entity>>
            {
                { PlayerType.Warrior, CreatePlayerWarrior },
                { PlayerType.Mage,    CreatePlayerMage }
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
                "Enemy type not found");

            return createPlayerFunctions[type](position);
        }

        public Entity CreateAttack(AttackType type, Vector2 position)
        {
            DebugAssert.Success(createAttackFunctions.ContainsKey(type),
                "Attack type not found");

            return createAttackFunctions[type](position);
        }

        #region Enemies
        private Entity CreateEnemyEnemy1(Vector2 position)
        {
            Texture2D platformTexture = content.Load<Texture2D>(
                GameContent.TexturePath("EntityPlatform"));
            Texture2D enemyTexture    = content.Load<Texture2D>(
                GameContent.TexturePath("Enemy1SpriteSheet"));

            Entity e = CreateEnemyBaseEntity(position, 
                "SkeletonSpriteSheet", 15.0f);

            /*
            Entity e   = entityManager.CreateEntity();
            e.Position = position;
            e.AddTag(EntityTags.Enemy);

            PhysicsCmp phy = entityManager.AddComponent(e, new PhysicsCmp());
            phy.Inertia = 0.0f;
            phy.LinearDamping = 1.5f;
            ColliderCmp col = entityManager.AddComponent(e, new ColliderCmp(
                new CircleCollider(8.0f), new Material(1.0f, 0.0f, 0.0f),
                CollisionBitmask.Enemy, CollisionBitmask.All));
            SpriteCmp enemySpr = entityManager.AddComponent(e,
                new SpriteCmp(enemyTexture));
            enemySpr.SourceRect = new Rectangle(0, 0, 48, 40);
            enemySpr.LayerOrder = LayerOrder.Ordered;
            enemySpr.Origin = new Vector2(
                enemySpr.SourceRect.Value.Width * 0.5f,
                enemyTexture.Height);

            CharacterCmp charCmp = entityManager.AddComponent(e,
                new CharacterCmp(platformTexture));
            charCmp.PlatformSourceRect = new Rectangle(0, 0, 32, 32);
            charCmp.SelectSourceRect = new Rectangle(32, 0, 36, 36);

            AnimationControllerCmp anim = entityManager.AddComponent(e,
                new AnimationControllerCmp(0));
            anim.AddAnimation("Idle", new SpriteAnimation("Idle", new List<Rectangle>()
            {
                new Rectangle(0, 0, 48, 40),
                new Rectangle(48, 0, 48, 40),
                new Rectangle(96, 0, 48, 40),
                new Rectangle(144, 0, 48, 40)
            }, 0.5f));
            anim.Play("Idle");
            */

            AICmp ai = entityManager.AddComponent(e, new AICmp());
            ai.DecisionTree = new TargetChangerDecisionNode(
                new NearestEntitySelector(EntityTags.Player),
                new PathFollowESkill(100.0f));

            return e;
        }
        #endregion

        #region Attacks
        private Entity CreateAttackProjectile1(Vector2 position)
        {
            Texture2D texture = content.Load<Texture2D>(
                GameContent.TexturePath("FireballSpriteSheet"));

            Entity e = entityManager.CreateEntity();
            e.Position = position;
            e.AddTag(EntityTags.Attack);

            PhysicsCmp phy = entityManager.AddComponent(e, new PhysicsCmp());
            phy.Inertia = 0.0f;
            phy.LinearDamping = 0.0f;
            TriggerColliderCmp col = entityManager.AddComponent(e, new TriggerColliderCmp(
                new CircleCollider(17 * 0.5f),
                CollisionBitmask.Attack, CollisionBitmask.Wall));
            col.OnTriggerEnter += (Entity e1, TriggerColliderCmp c1, 
                                   Entity e2, ColliderBody c2, 
                                   ColliderType type, in Manifold manifold) =>
            {
                if(type != ColliderType.Static)
                {
                    if(entityManager.TryGetComponent(e2, out HealthCmp health))
                    {
                        health.CurrentHealth -= 10.0f;
                    }

                    if(entityManager.TryGetComponent(e2, out PhysicsCmp physics))
                    {
                        physics.Force += -manifold.Normal * 2000.0f;
                    }
                }

                entityManager.RemoveEntity(e1);
            };

            SpriteCmp spriteCmp = entityManager.AddComponent(e,
                new SpriteCmp(texture));
            spriteCmp.SourceRect = new Rectangle(0, 0, 35, 17);
            spriteCmp.LayerOrder = LayerOrder.Ordered;
            spriteCmp.Origin = new Vector2(
                spriteCmp.SourceRect.Value.Width * 0.5f,
                spriteCmp.SourceRect.Value.Height * 0.5f);
            spriteCmp.Transform.LocalPosition = new Vector2(-10.0f, 0.0f);

            AnimationControllerCmp anim = entityManager.AddComponent(e,
                new AnimationControllerCmp(0));
            anim.AddAnimation("Default", new SpriteAnimation("Default", new List<Rectangle>()
            {
                new Rectangle(0, 0, 35, 17),
                new Rectangle(35, 0, 35, 17),
                new Rectangle(70, 0, 35, 17),
                new Rectangle(105, 0, 35, 17)
            }, 0.2f));
            anim.Play("Default");

            return e;
        }
        #endregion

        #region Player

        private Entity CreatePlayerWarrior(Vector2 position)
        {
            Entity e = CreatePlayerBaseEntity(position, 
                "WarriorSpriteSheet", 20.0f);

            //Modify the attacks the player can make
            //CharacterCmp charCmp = entityManager.GetComponent<CharacterCmp>(e);

            return e;
        }

        private Entity CreatePlayerMage(Vector2 position)
        {
            Entity e = CreatePlayerBaseEntity(position, 
                "MageSpriteSheet", 20.0f);

            //Modify the attacks the player can make
            //CharacterCmp charCmp = entityManager.GetComponent<CharacterCmp>(e);

            return e;
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
            string textureName, float health)
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
                "../../../Content/Animations/Character.anim"));
            anim.Play("Idle");

            SpriteCmp spr  = entityManager.AddComponent(e,
                new SpriteCmp(spriteTexture));
            spr.SourceRect = anim.GetCurrentFrameSource();
            spr.LayerOrder = LayerOrder.Ordered;
            spr.Origin     = new Vector2(
                spr.SourceRect.Value.Width * 0.5f,
                spr.SourceRect.Value.Height);

            CharacterCmp charCmp = entityManager.AddComponent(e,
                new CharacterCmp(platformTexture));
            charCmp.PlatformSourceRect = new Rectangle(16, 0, 16, 16);
            charCmp.SelectSourceRect   = new Rectangle(32, 0, 20, 20);

            AddHealthCmp(e, health);
            AddCharacterDeathCmp(e);

            return e;
        }

        private Entity CreatePlayerBaseEntity(Vector2 position, 
            string textureName, float health)
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
                "../../../Content/Animations/Character.anim"));
            anim.Play("Idle");

            SpriteCmp spr = entityManager.AddComponent(e,
                new SpriteCmp(spriteTexture));
            spr.SourceRect = anim.GetCurrentFrameSource();
            spr.LayerOrder = LayerOrder.Ordered;
            spr.Origin = new Vector2(
                spr.SourceRect.Value.Width * 0.5f,
                spr.SourceRect.Value.Height);

            CharacterCmp charCmp = entityManager.AddComponent(e,
                new CharacterCmp(platformTexture));
            charCmp.PlatformSourceRect = new Rectangle(0, 0, 16, 16);
            charCmp.SelectSourceRect   = new Rectangle(32, 0, 20, 20);

            AddHealthCmp(e, health);
            AddCharacterDeathCmp(e);

            return e;
        }

        #endregion
    }
}
