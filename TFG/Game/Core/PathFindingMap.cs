using Engine.Debug;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Core
{
    [Flags]
    public enum NodeState
    {
        None    = 0x00,
        OnQueue = 0x01,
        Visited = 0x02,
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
        private PriorityQueue<Node, int> queue;

        public PathFindingMap(DungeonLevel level) 
        {
            this.level     = level;
            this.nodes     = null;
            this.currentId = 0;
            this.queue     = new PriorityQueue<Node, int>();
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
            Tuple<Node, Node> nodes = GetStartAndEndNodes(from, to);

            List<Vector2> path = new List<Vector2>();
            SolveAStar(path, nodes.Item1, nodes.Item2);

            return path;
        }

        public void FindPath(List<Vector2> path, Vector2 from, Vector2 to)
        {
            Tuple<Node, Node> nodes = GetStartAndEndNodes(from, to);

            path.Clear();
            SolveAStar(path, nodes.Item1, nodes.Item2);
        }

        public Tuple<Node, Node> GetStartAndEndNodes(Vector2 from, Vector2 to)
        {
            Point fromTile = level.GetTileCoords(from);
            Point toTile   = level.GetTileCoords(to);

            //Invert the nodes because the path returned is inverted
            Node fromNode = nodes[toTile.X, toTile.Y];
            Node toNode = nodes[fromTile.X, fromTile.Y];

            return new Tuple<Node, Node>(fromNode, toNode);
        }

        private void CreateNodes(byte[,] tiles, int width, int height)
        {
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    byte val         = tiles[x, y];
                    Point arrayPos   = new Point(x, y);
                    Vector2 worldPos = level.GetWorldCoords(x, y);

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

        public void SolveDijkstra(List<Vector2> ret, Node start, Node end)
        {
            ResetNodes();

            queue.Clear();
            queue.Enqueue(start, 0);
            start.TotalCost = 0;

            Node current = null;
            while (queue.Count != 0 && current != end)
            {
                current       = queue.Dequeue();
                current.State = NodeState.Visited;

                foreach (Node next in current.Neighbours)
                {
                    if (next.State == NodeState.Visited) continue;

                    int totalCost = current.TotalCost;
                    //Comprobamos si es un nodo diagonal
                    if (current.ArrayPos.X != next.ArrayPos.X &&
                        current.ArrayPos.Y != next.ArrayPos.Y)
                        totalCost += 14; //Sqrt(2) * 10
                    else
                        totalCost += 10;

                    if (totalCost < next.TotalCost)
                    {
                        next.TotalCost  = totalCost;
                        next.PathParent = current;

                        queue.Enqueue(next, totalCost);
                    }
                }
            }

            while (end != null)
            {
                ret.Add(end.WorldPos);
                end = end.PathParent;
            }
        }

        public void Draw()
        {
            foreach(Node node in nodes)
            {
                if(node.PathParent != null)
                {
                    Vector2 dir = Vector2.Normalize(node.PathParent.WorldPos - node.WorldPos);
                    DebugDraw.Line(node.WorldPos, node.PathParent.WorldPos - dir * 5.0f, Color.Yellow);
                    DebugDraw.Point(node.WorldPos, Color.Red);
                }
            }
        }

        private void SolveAStar(List<Vector2> ret, Node start, Node end)
        {
            ResetNodes();
            start.TotalCost = 0;

            queue.Clear();
            queue.Enqueue(start, 0);

            Node current = null;
            while(queue.Count != 0 && current != end)
            {
                current       = queue.Dequeue();
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
                            next.State     = NodeState.OnQueue;
                        }

                        int priority    = totalCost + next.Heuristic;
                        next.TotalCost  = totalCost;
                        next.PathParent = current;

                        //Add the node even if it is already in the list
                        queue.Enqueue(next, priority);
                    }
                }
            }

            while(end != null)
            {
                ret.Add(end.WorldPos);
                end = end.PathParent;
            }
        }

        //Manhattan distance
        private int CalculateHeuristic(Node n1, Node n2)
        {
            return (Math.Abs(n1.ArrayPos.X - n2.ArrayPos.X) +
                    Math.Abs(n1.ArrayPos.Y - n2.ArrayPos.Y)) * 10;
        }
    }
}
