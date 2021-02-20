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

    public ChartedPath RequestPathLoop() => graph.GetChartedPath();
    public bool MapHasCycles => graph.HasCycles;

    public void PrintPathLoop()
    {
        var pathLoop = RequestPathLoop();
        pathLoop.DebugPath();
        pathLoop.ReversePath();
        pathLoop.DebugPath();
    }


    private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 190, 80, 60), "Get Queue"))
            GetDestinationQueue(GameManager.StartCell);

        if (GUI.Button(new Rect(10, 310, 80, 60), "Print Loop"))
            PrintPathLoop();
    }
}
