using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Systems;
using static Core.TileMap;

namespace Core
{
    public class DungeonLevel
    {
        public int TileSize   { get; private set; }
        public int NumTilesX  { get; private set; }
        public int NumTilesY  { get; private set; }
        public float Width    { get; private set; }
        public float Height   { get; private set; }
        public TileMap TileMap  { get; private set; }
        public CollisionMap CollisionMap { get; private set; }
        public PathFindingMap PathFindingMap { get; private set; }
        public List<Vector2> SpawnPoints { get; private set; }
        public PhysicsSystem Physics { get; set; }
        public ContentManager Content { get; set; }

        public DungeonLevel(ContentManager content)
        {
            TileSize       = 0;
            NumTilesX      = 0;
            NumTilesY      = 0;
            Width          = 0;
            Height         = 0;
            TileMap        = new TileMap(this);
            CollisionMap   = new CollisionMap(this);
            PathFindingMap = new PathFindingMap(this);
            SpawnPoints    = new List<Vector2>();
            Content        = content;
        }

        public Point GetTileCoords(Vector2 worldCoords)
        {
            return new Point(
                Math.Clamp((int)(worldCoords.X / TileSize), 0, NumTilesX - 1),
                Math.Clamp((int)(worldCoords.Y / TileSize), 0, NumTilesY - 1)
            );
        }

        public Point GetTileCoords(float x, float y)
        {
            return GetTileCoords(new Vector2(x, y));
        }

        public Vector2 GetWorldCoords(Point tileCoords)
        {
            return new Vector2(
                tileCoords.X * TileSize + TileSize * 0.5f,
                tileCoords.Y * TileSize + TileSize * 0.5f
            );
        }

        public Vector2 GetWorldCoords(int x, int y)
        {
            return GetWorldCoords(new Point(x, y));
        }

        public void Load(string path, PhysicsSystem physicsSystem, EntityFactory entityFactory)
        {
            Physics = physicsSystem;
            SpawnPoints.Clear();

            using(Stream stream = File.OpenRead(path))
            {
                using(BinaryReader reader = new BinaryReader(stream))
                {
                    TileSize  = reader.ReadInt32();
                    NumTilesX = reader.ReadInt32();
                    NumTilesY = reader.ReadInt32();
                    Width     = NumTilesX * TileSize;
                    Height    = NumTilesY * TileSize;

                    Texture2D tileset         = Content.Load<Texture2D>(
                        GameContent.TexturePath(reader.ReadString()));
                    TileMap.PreEntitiesTiles  = ReadTileLayer(reader, tileset);
                    TileMap.PostEntitiesTiles = ReadTileLayer(reader, tileset);
                    ReadCollisionLayer(reader);
                    ReadSpawnPoints(reader);
                    ReadEnemies(reader, entityFactory);
                }
            }
        }

        private Tile[,] ReadTileLayer(BinaryReader reader, Texture2D tileset)
        {
            Tile[,] tiles = new Tile[NumTilesX, NumTilesY];
            int textureNumTilesX = tileset.Width / TileSize;

            for (int y = 0; y < NumTilesY; ++y)
            {
                for (int x = 0; x < NumTilesX; ++x)
                {
                    short index = (short)(reader.ReadInt16() - 1);
                    if (index < 0) continue;

                    Point texCoords = new Point(
                        (index % textureNumTilesX) * TileSize,
                        (index / textureNumTilesX) * TileSize);
                    Vector2 position = new Vector2(
                        x * TileSize,
                        y * TileSize);

                    tiles[x, y] = new Tile(tileset, position,
                    new Rectangle(texCoords, new Point(TileSize, TileSize)));
                }
            }

            return tiles;
        }

        private void ReadCollisionLayer(BinaryReader reader)
        {
            byte[,] tiles = new byte[NumTilesX, NumTilesY];
            
            for (int y = 0; y < NumTilesY; ++y)
            {
                for (int x = 0;x < NumTilesX; ++x)
                {
                    tiles[x, y] = reader.ReadByte();
                }
            }

            PathFindingMap.Create(tiles);
            CollisionMap.Create(tiles);
        }

        private void ReadSpawnPoints(BinaryReader reader)
        {
            const int NUM_SPAWN_POINTS = 3;

            for(int i = 0;i < NUM_SPAWN_POINTS; ++i)
            {
                float x = reader.ReadSingle();
                float y = reader.ReadSingle();
                SpawnPoints.Add(new Vector2(x, y));
            }
        }

        private void ReadEnemies(BinaryReader reader, EntityFactory entityFactory)
        {
            int enemyCount = reader.ReadInt32();

            for(int i = 0;i < enemyCount; ++i)
            {
                EnemyType type = (EnemyType) reader.ReadInt32();
                float x        = reader.ReadSingle();
                float y        = reader.ReadSingle();

                entityFactory.CreateEnemy(type, new Vector2(x, y));
            }
        }
    }
}
