using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Engine.Debug;
using Engine.Ecs;
using Cmps;
using Engine.Core;
using Physics;
using Microsoft.Xna.Framework.Graphics;

namespace Core
{
    public enum EnemyType
    {
        Enemy1
    }

    public class EntityFactory
    {
        private static Dictionary<EnemyType, 
            Func<EntityManager<Entity>, ContentManager, Vector2, Entity>> 
            createEnemyFunctions;

        private EntityManager<Entity> entityManager;
        private ContentManager content;

        static EntityFactory()
        {
            createEnemyFunctions = new Dictionary<EnemyType,
                Func<EntityManager<Entity>, ContentManager, Vector2, Entity>>
            {
                { EnemyType.Enemy1, CreateEnemy1 }
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

        private static Entity CreateEnemy1(EntityManager<Entity> entityManager,
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
                CollisionBitmask.Player, CollisionBitmask.All));
            SpriteCmp enemySpr = entityManager.AddComponent(e,
                new SpriteCmp(enemyTexture));
            enemySpr.SourceRect = new Rectangle(0, 0, 48, 40);
            enemySpr.LayerOrder = LayerOrder.Ordered;
            enemySpr.Origin = new Vector2(
                enemySpr.SourceRect.Value.Width * 0.5f,
                enemyTexture.Height);

            SpriteCmp platformSpr = entityManager.AddComponent(e, 
                new SpriteCmp(platformTexture));
            platformSpr.Transform.LocalPosition = new Vector2(
                -platformTexture.Width * 0.5f,
                -platformTexture.Height * 0.5f);
            platformSpr.LayerOrder = LayerOrder.AlwaysBottom;

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

            return e;
        }
    }
}
