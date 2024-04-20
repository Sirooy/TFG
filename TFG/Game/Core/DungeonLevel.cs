using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Systems;
using Physics;
using Microsoft.Xna.Framework.Graphics;
using static Core.TileMap;
using System;

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
            PathFindingMap = new PathFindingMap(this);
            SpawnPoints    = new List<Vector2>();
            Content        = content;
        }

        public void Load(string path, PhysicsSystem physicsSystem, EntityFactory entityFactory)
        {
            Physics = physicsSystem;
            Physics.StaticColliders.Clear();
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

            CreateColliders(tiles);
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

        private void CreateColliders(byte[,] tiles)
        {
            int x = 0;
            int y = 0;

            while (y < NumTilesY)
            {
                while (x < NumTilesX)
                {
                    int value = tiles[x, y];

                    if (value != 0)
                        CreateBoxCollider(tiles, x, y);

                    ++x;
                }

                x = 0;
                ++y;
            }
        }

        private void CreateBoxCollider(byte[,] tiles, int startX, int startY)
        {
            int endX = startX;

            while (endX < NumTilesX && tiles[endX, startY] != 0)
            {
                tiles[endX, startY] = 0;
                ++endX;
            }

            int tileCountX = endX - startX;
            int tileDownCountX = tileCountX;
            int endY = startY + 1;

            while (endY < NumTilesY && tileCountX == tileDownCountX)
            {
                endX = startX;

                while (endX < NumTilesX && tiles[endX, endY] != 0)
                {
                    endX++;
                }

                tileDownCountX = endX - startX;

                if (tileDownCountX == tileCountX)
                {
                    //Fill with zeroes
                    endX = startX;
                    while (endX < NumTilesX && tiles[endX, endY] != 0)
                    {
                        tiles[endX, endY] = 0;
                        endX++;
                    }

                    ++endY;
                }
            }

            int tileCountY = (endY - startY);
            float blockWidth = tileCountX * TileSize;
            float blockHeight = tileCountY * TileSize;

            StaticCollider collider = Physics.AddStaticCollider(new RectangleCollider(
                blockWidth, blockHeight), new Material(1.0f, 0.0f, 0.0f),
                CollisionBitmask.Wall, CollisionBitmask.All);
            collider.Position = new Vector2(
                startX * TileSize + blockWidth * 0.5f,
                startY * TileSize + blockHeight * 0.5f);
        }
    }
}
