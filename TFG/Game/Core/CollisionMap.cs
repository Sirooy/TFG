using Microsoft.Xna.Framework;
using Physics;
using System;
using System.Collections.Generic;
using Systems;

namespace Core
{
    public class CollisionMap
    {
        public class Tile
        {
            public bool HasCollision;
            public bool IsVisited;

            public Tile()
            {
                HasCollision = false;
                IsVisited    = false;
            }
        }

        private DungeonLevel level;
        private Tile[,] tiles;
        private Queue<Point> searchQueue;

        public CollisionMap(DungeonLevel level)
        {
            this.level       = level;
            this.searchQueue = new Queue<Point>();
        }

        public void Create(byte[,] tiles)
        {
            level.Physics.StaticColliders.Clear();
            CreateTiles(tiles);
            CreateColliders(tiles);
        }

        public bool HasCollision(Vector2 position)
        {
            Point coords = level.GetTileCoords(position);

            return tiles[coords.X, coords.Y].HasCollision;
        }

        public bool FindClosestPosition(Vector2 position, out Vector2 result) 
        {
            Point start = level.GetTileCoords(position);
            bool found  = false;
            result      = Vector2.Zero;

            ResetVisitedPositions();

            searchQueue.Clear();
            searchQueue.Enqueue(start);
            tiles[start.X, start.Y].IsVisited = true;
            while(searchQueue.Count != 0 && !found)
            {
                Point point = searchQueue.Dequeue();

                if (tiles[point.X, point.Y].HasCollision)
                {
                    AddNeighboursToSearchQueue(point);
                }
                else
                {
                    result = level.GetWorldCoords(point);
                    found  = true;
                }
            }

            return found;
        }

        private void AddNeighboursToSearchQueue(Point point)
        {
            for(int x = -1; x < 2; ++x)
            {
                for(int y = -1; y < 2; ++y)
                {
                    int tileX = point.X + x;
                    int tileY = point.Y + y;

                    if (tileX == point.X && tileY == point.Y)
                        continue;

                    if (tileX < 0 || tileX > level.NumTilesX - 1 ||
                        tileY < 0 || tileY > level.NumTilesY - 1)
                        continue;

                    Tile tile = tiles[tileX, tileY];

                    if (tile.IsVisited) 
                        continue;

                    tile.IsVisited = true;
                    searchQueue.Enqueue(new Point(tileX, tileY));
                }
            }
        }

        private void ResetVisitedPositions()
        {
            for (int y = 0; y < level.NumTilesY; ++y)
            {
                for (int x = 0; x < level.NumTilesX; ++x)
                {
                    tiles[x, y].IsVisited = false;
                }
            }
        }

        private void CreateTiles(byte[,] tiles)
        {
            this.tiles = new Tile[level.NumTilesX, level.NumTilesY];

            for (int y = 0; y < level.NumTilesY; ++y)
            {
                for (int x = 0; x < level.NumTilesX; ++x)
                {
                    Tile tile         = new Tile();
                    tile.HasCollision = tiles[x, y] != 0;
                    this.tiles[x, y]  = tile;
                }
            }
        }

        private void CreateColliders(byte[,] tiles)
        {
            int x = 0;
            int y = 0;

            while (y < level.NumTilesY)
            {
                while (x < level.NumTilesX)
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

            while (endX < level.NumTilesX && tiles[endX, startY] != 0)
            {
                tiles[endX, startY] = 0;
                ++endX;
            }

            int tileCountX = endX - startX;
            int tileDownCountX = tileCountX;
            int endY = startY + 1;

            while (endY < level.NumTilesY && tileCountX == tileDownCountX)
            {
                endX = startX;

                while (endX < level.NumTilesX && tiles[endX, endY] != 0)
                {
                    endX++;
                }

                tileDownCountX = endX - startX;

                if (tileDownCountX == tileCountX)
                {
                    //Fill with zeroes
                    endX = startX;
                    while (endX < level.NumTilesX && tiles[endX, endY] != 0)
                    {
                        tiles[endX, endY] = 0;
                        endX++;
                    }

                    ++endY;
                }
            }

            int tileCountY = (endY - startY);
            float blockWidth = tileCountX * level.TileSize;
            float blockHeight = tileCountY * level.TileSize;

            StaticCollider collider = level.Physics.AddStaticCollider(new RectangleCollider(
                blockWidth, blockHeight), new Material(1.0f, 0.0f, 0.0f),
                CollisionBitmask.Wall, CollisionBitmask.All);
            collider.Position = new Vector2(
                startX * level.TileSize + blockWidth * 0.5f,
                startY * level.TileSize + blockHeight * 0.5f);
        }
    }
}
