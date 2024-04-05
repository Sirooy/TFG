
using System;
using Engine.Ecs;
using Microsoft.Xna.Framework;

namespace Core
{
    [Flags]
    public enum EntityTags
    {
        None   = 0x00,
        Player = 0x01,
        Enemy  = 0x02,
        Attack = 0x04
    }

    public class Entity : EntityBase
    {
        public Vector2 Position;
        public float Rotation;
        public float Scale;
        public EntityTags Tags;

        public Entity() : base()
        {
            Position = Vector2.Zero;
            Rotation = 0.0f;
            Scale    = 1.0f;
        }

        public void AddTag(EntityTags tags)
        {
            Tags |= tags;
        }

        public void RemoveTag(EntityTags tags)
        {
            Tags &= (~tags);
        }

        public bool HasTag(EntityTags tags)
        {
            return (Tags & tags) == tags; 
        }
    }
}
