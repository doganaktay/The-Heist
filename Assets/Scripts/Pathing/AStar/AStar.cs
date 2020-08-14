using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AStar : MonoBehaviour
{
    public Maze maze;
    public Pathfinder pathfinder;
    public AreaFinder areafinder;

    //public Dictionary<MazeCell, MazeCell> cameFrom = new Dictionary<MazeCell, MazeCell>();
    //public Dictionary<MazeCell, float> costSoFar = new Dictionary<MazeCell, float>();

    static public float Heuristic(MazeCell a, MazeCell b)
    {
        return Mathf.Abs(a.pos.x - b.pos.x) + Mathf.Abs(a.pos.y - b.pos.y);
    }

    bool pathFound;
    public List<MazeCell> aStarPath = new List<MazeCell>();
    FastPriorityQueue<MazeCell> frontier;

    public List<MazeCell> AStarSearch(MazeCell start, MazeCell end)
    {
        Dictionary<MazeCell, MazeCell> cameFrom = new Dictionary<MazeCell, MazeCell>();
        Dictionary<MazeCell, float> costSoFar = new Dictionary<MazeCell, float>();

        pathFound = false;

        if (frontier == null)
            frontier = new FastPriorityQueue<MazeCell>(maze.size.x * maze.size.y);

        //var frontier = new FastPriorityQueue<MazeCell>(maze.size.x * maze.size.y);

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
                float newCost = costSoFar[current] + maze.Cost(current, next);

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

        while(frontier.Count > 0)
        {
            var node = frontier.Dequeue();
            frontier.ResetNode(node);
        }

        if (!pathFound)
        { Debug.Log("no available path from " + start + " to " + end); return null; }

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

        foreach(var node in aStarPath)
        {
            frontier.ResetNode(node);
        }

        return aStarPath;
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 70, 80, 60), "AStar Test"))
        {
            var l = areafinder.GetRandomAreaWeighted();

            var a = l[Random.Range(0, l.Count)];
            var b = l[Random.Range(0, l.Count)];

            var list = AStarSearch(a, b);

            foreach(var item in list)
            {
                Debug.Log(item.gameObject.name);
                item.mat.SetFloat(GameManager.colorIndex, 1);
            }
        }
    }
}
