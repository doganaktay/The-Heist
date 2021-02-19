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

    public Queue<(MazeCell start, int index)> RequestPathLoop()
    {
        var cycles = GraphFinder.cycles;
        var random = Random.Range(0, cycles.Count);
        var randomCycle = cycles[random];
        var waypoints = graph.GetLoopWaypoints(randomCycle.nodes);
        var indices = randomCycle.edges;
        var queue = new Queue<(MazeCell start, int index)>();

        for(int i = randomCycle.nodes.Length - 1; i >= 0; i--)
        {
            queue.Enqueue((waypoints[i], indices[i]));
        }

        return queue;
    }

    public void PrintPathLoop()
    {
        var queue = RequestPathLoop();
        string str = "";

        while(queue.Count > 0)
        {
            var pair = queue.Dequeue();
            str += pair.start.gameObject.name + " - " + pair.index + " - ";
        }

        Debug.Log($"Loop path: {str}");
    }


    private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 190, 80, 60), "Get Queue"))
            GetDestinationQueue(GameManager.StartCell);

        if (GUI.Button(new Rect(10, 310, 80, 60), "Print Loop"))
            PrintPathLoop();
    }
}
