using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class PathDesigner : MonoBehaviour
{
    [HideInInspector]
    public static PathDesigner Instance;
    [SerializeField]
    GraphFinder graph;
    [SerializeField]
    int endProximityThreshold = 3;

    Player player = GameManager.player;

    private void Awake()
    {
        if(Instance == null)
            Instance = this;
        else
        {
            Debug.LogError("Trying to create a second instance of path designer. Destroying game object");
            Destroy(gameObject);
        }
    }

    public ChartedPath ChartPath(MazeCell from, MazeCell to)
    {
        if (graph.BiDirSearch(from, to))
            return graph.ChartedPath;
        else
            Debug.LogError("Charted path could not be found");

        return new ChartedPath(null, new int[1]);
    }

    public (MazeCell cell, int avoidIndex) GetSearchStart(MazeCell current, MazeCell observation)
    {
        var shareIndex = graph.GetClosestSharedIndex(current, observation);

        if(shareIndex != -1)
        {
            var ends = GraphFinder.GraphAreas[shareIndex].ends;

            if (ends.Count == 1 && ends[0] == current)
                return (null, shareIndex);
            else if (ends.Contains(observation))
            {
                return (observation, shareIndex);
            }
            else if (ends.Count <= 2)
            {
                if (current.IsGraphConnection)
                    return (observation.GetClosestJunction(current), shareIndex);
                else
                    return (observation.GetClosestJunction(), shareIndex);
            }
            else
            {
                if (observation.MeasuredJunctions[0].Value <= endProximityThreshold)
                    return (observation.MeasuredJunctions[0].Key, shareIndex);
                else
                    return (null, shareIndex);
            }
        }
        else
        {
            var path = PathRequestManager.RequestPathImmediate(current, observation);

            for(int i = path.Count - 1; i >= 0; i--)
            {
                if (path[i].IsGraphConnection)
                {
                    if (path[i] == observation)
                        return (path[i], graph.GetClosestSharedIndex(path[i], path[i - 1]));
                    else
                        return (path[i], graph.GetClosestSharedIndex(path[i], path[i + 1]));
                }
            }
        }

        return (null, -1);

        // MazeCell search;
        // if current and obs share index
            // if index.ends.count == 1 && index.ends[0] == current
                // return null;
            // else if index.ends.count <= 2
                // if obs is end
                    // return obs
                // else
                    // return end closest to obs
            // else
                // if closest end is closer to obs than dist threshold
                    // return closest end
                // else
                    // return multiple possibles (eliminate current or closest to current)
        // else
            // if current and obs share junction
                // if obsIndex.ends.count == 1
                    // return junction
                // else if obsIndex.ends.count == 2
                    // return end that isn't shared (can be the obs itself or not)
                // else
                    // return multiple possibles
            // else
                // get A* path to obs
                // find last end visited
                // if last end is obs
                    // return obs
                // else if obsIndex.ends.count == 2
                    // return end of obsIndex that isn't last end visited on path
                // else
                    // return multiple possibles (exclude last end visited on path)



    }

    public Queue<MazeCell> GetDestinationQueue(MazeCell currentPos, MazeCell observationPoint = null, BehaviorType type = BehaviorType.Wander)
    {
        int count = 10;
        var cell = currentPos;

        var queue = new Queue<MazeCell>();

        for (int i = 0; i < count; i++)
        {
            bool isJunction = cell.GraphAreaIndices.Count > 1;
            MazeCell destination;

            if (isJunction)
            {
                var connections = graph.GetConnections(cell);
                destination = connections[Random.Range(0, connections.Count)];
            }
            else
            {
                var areaExits = graph.GetJunctionCells(cell.GetGraphAreaIndices()[0]);
                destination = areaExits[Random.Range(0, areaExits.Count)];
            }

            queue.Enqueue(destination);

            cell = destination;
        }

        Debug.Log($"Queue request start at: {currentPos.gameObject.name}");

        while (queue.Count > 0)
        {
            var next = queue.Dequeue();
            Debug.Log($"Next in queue: {next.gameObject.name}");
        }

        return queue;
    }

    public ChartedPath RequestPathLoop() => graph.GetLoop();
    public bool MapHasCycles => graph.HasCycles;

    public void PrintPathLoop()
    {
        var pathLoop = RequestPathLoop();
        pathLoop.DebugPath();
        pathLoop.ReversePath();
        pathLoop.DebugPath();
    }


    //private void OnGUI()
    //{
    //    if (GUI.Button(new Rect(10, 190, 80, 60), "Get Queue"))
    //        GetDestinationQueue(GameManager.StartCell);

    //    if (GUI.Button(new Rect(10, 310, 80, 60), "Print Loop"))
    //        PrintPathLoop();
    //}
}
