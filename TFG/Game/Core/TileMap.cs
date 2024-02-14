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

        public Tile[,] PreEntitiesTiles;
        public Tile[,] PostEntitiesTiles;
        public int TileSize;

        public TileMap()
        {
            PreEntitiesTiles  = null;
            PostEntitiesTiles = null;
            TileSize          = 0;
        }

        /*
        public void Load(Texture2D tilesetTexture, string path, PhysicsSystem physics)
        {
            string json = File.ReadAllText(path);
            
            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                JsonElement root = doc.RootElement;
                JsonElement layers = root.GetProperty("layers");
                width    = root.GetProperty("width").GetInt32();
                height   = root.GetProperty("height").GetInt32();
                tileSize = root.GetProperty("tilewidth").GetInt32();

                foreach(JsonElement layer in layers.EnumerateArray())
                {
                    string name = layer.GetProperty("name").GetString();
                    if(name == "PreEntities")
                    {
                        preEntitiesTiles = CreateTiles(layer, 
                            tilesetTexture, width, height);
                    }
                    else if(name == "PostEntities")
                    {
                        postEntitiesTiles = CreateTiles(layer, 
                            tilesetTexture, width, height);
                    }
                    else if(name == "Collision")
                    {
                        CreateCollisionLayer(layer,
                            width, height, physics);
                    }
                }
            }

            Console.WriteLine(width + " " + height + " " + tileSize);
        }

        private Tile[,] CreateTiles(JsonElement layer, Texture2D tilesetTexture, 
            int width, int height)
        {
            Tile[,] tiles = new Tile[width, height];
            float startX  = layer.GetProperty("x").GetInt32();
            float startY  = layer.GetProperty("y").GetInt32();
            int numTiles  = tilesetTexture.Width / tileSize;
            int index = 0;

            JsonElement data = layer.GetProperty("data");
            foreach (JsonElement element in data.EnumerateArray())
            {
                int texIndex = element.GetInt32() - 1;
                if (texIndex == -1)
                {
                    ++index;
                    continue;
                }

                int x = (index % width);
                int y = (index / width);

                Point texCoords = new Point(
                    (texIndex % numTiles) * tileSize, 
                    (texIndex / numTiles) * tileSize);
                Vector2 position = new Vector2(
                    x * tileSize + startX,
                    y * tileSize + startY);

                tiles[x, y] = new Tile(tilesetTexture, position,
                    new Rectangle(texCoords, new Point(tileSize, tileSize)));

                ++index;
            }

            return tiles;
        } 
        */

        public Tile GetPreEntitiesTile(Vector2 pos)
        {
            int x = (int) (pos.X / TileSize);
            int y = (int) (pos.Y / TileSize);

            if (x < 0 || x > PreEntitiesTiles.GetLength(0) - 1 ||
               y < 0 || y > PreEntitiesTiles.GetLength(1) - 1)
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

                    if (t != null && cameraBounds.Contains(t.Position, TileSize, TileSize))
                        spriteBatch.Draw(t.Texture, t.Position, t.Source, Color.White);
                }
            }
        }
    }
}
