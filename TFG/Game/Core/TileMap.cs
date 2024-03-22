using System;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Engine.Debug;
using Engine.Graphics;
using Engine.Core;
using TFG;
using Microsoft.Xna.Framework.Content;
using Systems;
using Physics;

namespace Core
{
    public class TileMap
    {
        public class Tile
        {
            public Texture2D Texture;
            public Vector2 Position;
            public Rectangle Source;

            public Tile(Texture2D texture, Vector2 position, Rectangle source)
            {
                Texture  = texture;
                Position = position;
                Source   = source;
            }
        }

        private DungeonLevel level;
        public Tile[,] PreEntitiesTiles;
        public Tile[,] PostEntitiesTiles;

        public TileMap(DungeonLevel level)
        {
            this.level        = level;
            PreEntitiesTiles  = null;
            PostEntitiesTiles = null;
        }

        public Tile GetPreEntitiesTile(Vector2 pos)
        {
            int x = (int) (pos.X / level.TileSize);
            int y = (int) (pos.Y / level.TileSize);

            if (x < 0 || x > level.NumTilesX - 1 ||
               y < 0 || y > level.NumTilesY - 1)
                return null;

            return PreEntitiesTiles[x, y];
        }

        public void DrawPreEntitiesLayer(Camera2D camera, SpriteBatch spriteBatch)
        {
            DrawTiles(PreEntitiesTiles, camera, spriteBatch);
        }

        public void DrawPostEntitiesLayer(Camera2D camera, SpriteBatch spriteBatch)
        {
            DrawTiles(PostEntitiesTiles, camera, spriteBatch);
        }

        private void DrawTiles(Tile[,] tiles, Camera2D camera, SpriteBatch spriteBatch)
        {
            AABB cameraBounds = camera.GetBounds();

            for (int i = 0; i < tiles.GetLength(0); ++i)
            {
                for (int j = 0; j < tiles.GetLength(1); ++j)
                {
                    Tile t = tiles[i, j];

                    if (t != null && cameraBounds.Contains(t.Position, 
                        level.TileSize, level.TileSize))
                    {
                        spriteBatch.Draw(t.Texture, t.Position, 
                            t.Source, Color.White);
                    }
                }
            }
        }
    }
}
