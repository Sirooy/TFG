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

        private Tile[,] preEntitiesTiles;
        private Tile[,] postEntitiesTiles;
        private int tileSize;
        private int width;
        private int height;

        public TileMap()
        {
            preEntitiesTiles  = null;
            postEntitiesTiles = null;
            tileSize          = 0;
            width             = 0;
            height            = 0;
        }

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

        private void CreateCollisionLayer(JsonElement layer,
            int width, int height, PhysicsSystem physics)
        {
            int[,] tiles = new int[width, height];
            int index = 0;
            
            JsonElement data = layer.GetProperty("data");
            foreach (JsonElement element in data.EnumerateArray())
            {
                int n = element.GetInt32();
                int x = (index % width);
                int y = (index / width);

                tiles[x, y] = n;

                ++index;
            }

            CreateColliders(tiles, width, height, physics);
        }

        private void CreateColliders(int[,] tiles, int width, int height, 
            PhysicsSystem physics)
        {
            int x = 0;
            int y = 0;

            while(y < height)
            {
                while(x < width)
                {
                    int value = tiles[x, y];

                    if(value != 0)
                        CreateBoxCollider(tiles, width, height, x, y, physics);
                    
                    ++x;
                }

                x = 0;
                ++y;
            }
        }

        private void CreateBoxCollider(int[,] tiles, int width, int height, 
            int startX, int startY, PhysicsSystem physics)
        {
            int endX = startX;

            while (endX < width && tiles[endX, startY] != 0)
            {
                tiles[endX, startY] = 0;
                ++endX;
            }

            int tileCountX     = endX - startX;
            int tileDownCountX = tileCountX;
            int endY           = startY + 1;

            while (endY < height && tileCountX == tileDownCountX)
            {
                endX = startX;
                
                while(endX < width && tiles[endX, endY] != 0)
                {
                    endX++;
                }

                tileDownCountX = endX - startX;

                if (tileDownCountX == tileCountX)
                { 
                    //Fill with zeroes
                    endX = startX;
                    while (endX < width && tiles[endX, endY] != 0)
                    {
                        tiles[endX, endY] = 0;
                        endX++;
                    }

                    ++endY;
                }
            }

            int tileCountY = (endY - startY);
            float blockWidth  = tileCountX * tileSize;
            float blockHeight = tileCountY * tileSize;

            StaticCollider collider = physics.AddStaticCollider(new RectangleCollider(
                blockWidth, blockHeight), new Material(1.0f, 0.0f, 0.0f), 
                CollisionBitmask.Wall, CollisionBitmask.All);
            collider.Position = new Vector2(
                startX * tileSize + blockWidth * 0.5f,
                startY * tileSize + blockHeight * 0.5f);
        }

        public void DrawPreEntitiesLayer(Camera2D camera, SpriteBatch spriteBatch)
        {
            DrawTiles(preEntitiesTiles, camera, spriteBatch);
        }

        public void DrawPostEntitiesLayer(Camera2D camera, SpriteBatch spriteBatch)
        {
            DrawTiles(postEntitiesTiles, camera, spriteBatch);
        }

        private void DrawTiles(Tile[,] tiles, Camera2D camera, SpriteBatch spriteBatch)
        {
            AABB cameraBounds = camera.GetBounds();

            for (int i = 0; i < tiles.GetLength(0); ++i)
            {
                for (int j = 0; j < tiles.GetLength(1); ++j)
                {
                    Tile t = tiles[i, j];

                    if (t != null && cameraBounds.Contains(t.Position, tileSize, tileSize))
                        spriteBatch.Draw(t.Texture, t.Position, t.Source, Color.White);
                }
            }
        }
    }
}
