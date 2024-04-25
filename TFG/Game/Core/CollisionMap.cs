using Microsoft.Xna.Framework;
using Physics;
using Systems;

namespace Core
{
    public class CollisionMap
    {
        public class Tile
        {
            public bool HasCollision;
        }

        private DungeonLevel level;
        private byte[,] tiles;

        public CollisionMap(DungeonLevel level)
        {
            this.level = level;
        }

        public void Create(byte[,] tiles, PhysicsSystem physicsSystem)
        {
            this.tiles = tiles;

            physicsSystem.StaticColliders.Clear();
            CreateColliders(tiles);
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
