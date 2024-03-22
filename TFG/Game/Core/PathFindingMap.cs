using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    [Flags]
    public enum NodeState
    {
        None       = 0x00,
        OnOpenList = 0x01,
        Visited    = 0x02,
    }

    public class Node
    {
        public int Id;
        public bool IsSolid;
        public Point ArrayPos;
        public Vector2 WorldPos;
        public List<Node> Neighbours;
        public Node PathParent;
        public NodeState State;
        public int TotalCost; //G
        public int Heuristic; //H

        public Node(int id, bool isSolid, 
            Point arrayPos, Vector2 worldPos)
        {
            Id         = id;
            IsSolid    = isSolid;
            ArrayPos   = arrayPos;
            WorldPos   = worldPos;
            Neighbours = new List<Node>();
            PathParent = null;
            State      = NodeState.None;
            TotalCost  = 0;
            Heuristic  = 0;
        }
    }

    //PathFinding using A* algorithim
    public class PathFindingMap
    {
        private DungeonLevel level;
        private Node[,] nodes;
        private int currentId;
        private PriorityQueue<Node, int> openList;

        public PathFindingMap(DungeonLevel level) 
        {
            this.level     = level;
            this.nodes     = null;
            this.currentId = 0;
            this.openList  = new PriorityQueue<Node, int>();
        }

        public void Create(byte[,] tiles)
        {
            int width  = tiles.GetLength(0);
            int height = tiles.GetLength(1);
            nodes      = new Node[width, height];
            currentId  = 0;

            CreateNodes(tiles, width, height);
            SetNodesNeighbours(width, height);
        }

        public List<Vector2> FindPath(Vector2 from, Vector2 to)
        {
            int fromX = Math.Clamp((int) (from.X / level.TileSize),
                0, level.NumTilesX - 1);
            int fromY = Math.Clamp((int)(from.Y / level.TileSize),
                0, level.NumTilesY - 1);
            int toX   = Math.Clamp((int)(to.X / level.TileSize),
                0, level.NumTilesX - 1);
            int toY   = Math.Clamp((int)(to.Y / level.TileSize),
                0, level.NumTilesY - 1);

            //Invert the nodes because the path returned is inverted
            Node fromNode = nodes[toX, toY];
            Node toNode   = nodes[fromX, fromY];

            return Solve(fromNode, toNode);
        }

        private void CreateNodes(byte[,] tiles, int width, int height)
        {
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    byte val         = tiles[x, y];
                    Point arrayPos   = new Point(x, y);
                    Vector2 worldPos = new Vector2(x * level.TileSize + level.TileSize * 0.5f,
                                                   y * level.TileSize + level.TileSize * 0.5f);

                    nodes[x, y] = new Node(currentId, val >= 1, arrayPos, worldPos);

                    currentId++;
                }
            }
        }

        private void SetNodesNeighbours(int width, int height)
        {
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    SetNodeNeighbours(nodes[x, y], 
                        width, height, x, y);
                }
            }
        }

        private void SetNodeNeighbours(Node node, int width, int height, 
            int nodeX, int nodeY)
        {
            if (node.IsSolid) return;

            for(int otherY = nodeY - 1; otherY < nodeY + 2; ++otherY)
            {
                if (otherY < 0 || otherY > height - 1) continue;

                for(int otherX = nodeX - 1; otherX < nodeX + 2; otherX++)
                {
                    if (otherX < 0 || otherX > width - 1) continue;

                    Node other = nodes[otherX, otherY];

                    if (node == other || other.IsSolid) continue;

                    int xDiff = nodeX - otherX;
                    int yDiff = nodeY - otherY;

                    //Is a diagonal node
                    if (xDiff != 0 && yDiff != 0)
                    {
                        if (!nodes[otherX + xDiff, otherY].IsSolid &&
                            !nodes[otherX, otherY + yDiff].IsSolid)
                            node.Neighbours.Add(other);
                    }
                    else
                    {
                        node.Neighbours.Add(other);
                    }
                }
            }
        }

        private void ResetNodes()
        {
            int width  = nodes.GetLength(0);
            int height = nodes.GetLength(1);

            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    Node n = nodes[x, y];

                    n.State      = NodeState.None;
                    n.TotalCost  = int.MaxValue;
                    n.Heuristic  = 0;
                    n.PathParent = null;
                }
            }
        }

        private List<Vector2> Solve(Node start, Node end)
        {
            ResetNodes();
            start.TotalCost = 0;

            openList.Clear();
            openList.Enqueue(start, 0);

            Node current = null;
            while(openList.Count != 0 && current != end)
            {
                current       = openList.Dequeue();
                current.State = NodeState.Visited;

                foreach(Node next in current.Neighbours)
                {
                    if (next.State == NodeState.Visited) continue;

                    int totalCost = current.TotalCost;
                    //Check if is a diagonal node
                    if (current.ArrayPos.X != next.ArrayPos.X &&
                        current.ArrayPos.Y != next.ArrayPos.Y)
                        totalCost += 14; //Sqrt(2) * 10
                    else
                        totalCost += 10;

                    if(totalCost < next.TotalCost)
                    {
                        //If is a new node, calculate the heuristic
                        //and mark it as "on the open list"
                        if(next.State == NodeState.None) 
                        {
                            next.Heuristic = CalculateHeuristic(next, end);
                            next.State     = NodeState.OnOpenList;
                        }

                        int priority    = totalCost + next.Heuristic;
                        next.TotalCost  = totalCost;
                        next.PathParent = current;

                        //Add the node even if it is already in the list
                        openList.Enqueue(next, priority);
                    }
                }
            }

            List<Vector2> ret = new List<Vector2>();

            while(end != null)
            {
                ret.Add(end.WorldPos);
                end = end.PathParent;
            }

            return ret;
        }

        //Manhattan distance
        private int CalculateHeuristic(Node n1, Node n2)
        {
            return (Math.Abs(n1.ArrayPos.X - n2.ArrayPos.X) +
                    Math.Abs(n1.ArrayPos.Y - n2.ArrayPos.Y)) * 10;
        }
    }
}
