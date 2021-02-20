using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AStar
{
    public Maze maze;

    public static Dictionary<MazeCell, MazeCell> cameFrom = new Dictionary<MazeCell, MazeCell>();
    public static Dictionary<MazeCell, float> costSoFar = new Dictionary<MazeCell, float>();

    public AStar(Maze maze)
    {
        this.maze = maze;
    }

    static public float Heuristic(MazeCell a, MazeCell b)
    {
        return Mathf.Abs(a.pos.x - b.pos.x) + Mathf.Abs(a.pos.y - b.pos.y);
    }

    bool pathFound;
    public List<MazeCell> aStarPath = new List<MazeCell>();
    FastPriorityQueue<MazeCell> frontier;

    public List<MazeCell> GetPath(PathLayer layer, MazeCell start, MazeCell end, int forcedGraphIndex = -1)
    {
        // clearing dictionaries for re-use
        cameFrom.Clear();
        costSoFar.Clear();

        pathFound = false;

        if (frontier == null)
            frontier = new FastPriorityQueue<MazeCell>(maze.size.x * maze.size.y);

        frontier.Enqueue(start, 0);

        cameFrom[start] = start;
        costSoFar[start] = 0;

        while(frontier.Count > 0)
        {
            var current = frontier.Dequeue();

            if (current == end)
            { pathFound = true; break; }

            foreach(var next in current.connectedCells)
            {
                if (next.state > 1 || (forcedGraphIndex >= 0 && !next.GetGraphAreaIndices().Contains(forcedGraphIndex)))
                    continue;

                float newCost = costSoFar[current] + Heuristic(current, next) + maze.Cost(current, next);

                if (!costSoFar.ContainsKey(next) || newCost < costSoFar[next])
                {
                    costSoFar[next] = newCost;
                    float priority = newCost + Heuristic(next, end);

                    if (!frontier.Contains(next))
                        frontier.Enqueue(next, priority);
                    else
                        frontier.UpdatePriority(next, priority);

                    cameFrom[next] = current;
                }
            }

            if(layer == PathLayer.Special)
            {
                foreach (var next in current.specialConnectedCells)
                {
                    if (next.state > 1)
                        continue;

                    float newCost = costSoFar[current] + Heuristic(current, next) + maze.Cost(current, next);

                    if (!costSoFar.ContainsKey(next) || newCost < costSoFar[next])
                    {
                        costSoFar[next] = newCost;
                        float priority = newCost + Heuristic(next, end);

                        if (!frontier.Contains(next))
                            frontier.Enqueue(next, priority);
                        else
                            frontier.UpdatePriority(next, priority);

                        cameFrom[next] = current;
                    }
                }
            }
        }

        // resetting the nodes still left in queue after astar completes
        while(frontier.Count > 0)
        {
            var node = frontier.Dequeue();
            frontier.ResetNode(node);
        }

        if (!pathFound)
        {
            string str = "";
            str += "no available path from " + start + " to " + end;

            if (forcedGraphIndex > -1)
                str += " on " + forcedGraphIndex;

            Debug.Log(str);
            return null;
        }

        aStarPath.Clear();

        aStarPath.Add(end);
        var previous = cameFrom[end];
        while(previous != start)
        {
            aStarPath.Add(previous);
            previous = cameFrom[previous];
        }
        aStarPath.Add(start);
        aStarPath.Reverse();

        // resetting the nodes in path for re-use
        foreach(var node in aStarPath)
        {
            frontier.ResetNode(node);
        }

        return aStarPath;
    }
}
