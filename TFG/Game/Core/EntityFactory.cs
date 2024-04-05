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
        Enemy1
    }

    public enum AttackType
    {
        Projectile1
    }

    public class EntityFactory
    {
        private static Dictionary<EnemyType, 
            Func<EntityManager<Entity>, ContentManager, Vector2, Entity>> 
            createEnemyFunctions;
        private static Dictionary<AttackType,
            Func<EntityManager<Entity>, ContentManager, Vector2, Entity>>
            createAttackFunctions;

        private EntityManager<Entity> entityManager;
        private ContentManager content;

        static EntityFactory()
        {
            createEnemyFunctions = new Dictionary<EnemyType,
                Func<EntityManager<Entity>, ContentManager, Vector2, Entity>>
            {
                { EnemyType.Enemy1, CreateEnemyEnemy1 }
            };

            createAttackFunctions = new Dictionary<AttackType,
                Func<EntityManager<Entity>, ContentManager, Vector2, Entity>>
            {
                { AttackType.Projectile1, CreateAttackProjectile1 }
            };
        }

        public EntityFactory(EntityManager<Entity> entityManager, 
            ContentManager content)
        {
            this.entityManager = entityManager;
            this.content       = content;
        }

        public Entity CreateEnemy(EnemyType type, Vector2 position)
        {
            DebugAssert.Success(createEnemyFunctions.ContainsKey(type),
                "Enemy type not found");

            return createEnemyFunctions[type](entityManager, content, position);
        }

        public Entity CreateAttack(AttackType type, Vector2 position)
        {
            DebugAssert.Success(createAttackFunctions.ContainsKey(type),
                "Attack type not found");

            return createAttackFunctions[type](entityManager, content, position);
        }

        private static Entity CreateEnemyEnemy1(EntityManager<Entity> entityManager,
            ContentManager content, Vector2 position)
        {
            Texture2D platformTexture = content.Load<Texture2D>("EntityPlatform");
            Texture2D enemyTexture    = content.Load<Texture2D>("Enemy1SpriteSheet");

            Entity e   = entityManager.CreateEntity();
            e.Position = position;
            e.AddTag(EntityTags.Enemy);

            PhysicsCmp phy = entityManager.AddComponent(e, new PhysicsCmp());
            phy.Inertia = 0.0f;
            phy.LinearDamping = 1.5f;
            ColliderCmp col = entityManager.AddComponent(e, new ColliderCmp(
                new CircleCollider(16.0f), new Material(1.0f, 0.0f, 0.0f),
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
            anim.AddAnimation("Idle", new SpriteAnimation(new List<Rectangle>()
            {
                new Rectangle(0, 0, 48, 40),
                new Rectangle(48, 0, 48, 40),
                new Rectangle(96, 0, 48, 40),
                new Rectangle(144, 0, 48, 40)
            }, 0.5f));
            anim.Play("Idle");

            AICmp ai = entityManager.AddComponent(e, new AICmp());
            ai.DecisionTree = new TargetChangerDecisionNode(
                new NearestEntitySelector(EntityTags.Player),
                new StraightDashSkill(400.0f));

            entityManager.AddComponent(e, new HealthCmp(15.0f));

            return e;
        }

        private static Entity CreateAttackProjectile1(EntityManager<Entity> entityManager,
            ContentManager content, Vector2 position)
        {
            Texture2D texture = content.Load<Texture2D>("FireballSpriteSheet");

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
                        health.AddHealth(-10.0f);
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
            anim.AddAnimation("Default", new SpriteAnimation(new List<Rectangle>()
            {
                new Rectangle(0, 0, 35, 17),
                new Rectangle(35, 0, 35, 17),
                new Rectangle(70, 0, 35, 17),
                new Rectangle(105, 0, 35, 17)
            }, 0.2f));
            anim.Play("Default");

            return e;
        }
    }
}
