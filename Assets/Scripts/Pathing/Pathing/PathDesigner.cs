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

    private void Awake()
    {
        if(Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public ChartedPath GetPursuitPath(AI ai, MazeCell current, MazeCell observation)
    {
        var start = GetSearchStart(current, observation);

        if (start.cell == null)
            return new ChartedPath();

        List<MazeCell> cells = new List<MazeCell>();
        List<int> indices = new List<int>();
        Queue<(MazeCell cell, int index)> cellsToExplore = new Queue<(MazeCell cell, int index)>();

        int maxTravelDist = Mathf.RoundToInt(GameManager.CellCount * ai.fitness);
        int dist = 0;
        
        cellsToExplore.Enqueue((start.cell, start.index));
        cells.Add(current);
        indices.Add(GraphFinder.GetClosestSharedIndex(current, start.cell));

        dist += PathRequestManager.RequestPathImmediate(current, start.cell).Count - 1;

        while(cellsToExplore.Count != 0)
        {
            var next = cellsToExplore.Dequeue();
            cells.Add(next.cell);

            var candidates = graph.GetOtherConnections(next.cell, next.index);

            if (candidates.Count == 1)
            {
                dist += next.cell.GetJunctionDistance(candidates[0].cell);
                if (dist > maxTravelDist)
                    break;

                cellsToExplore.Enqueue(candidates[0]);
                indices.Add(candidates[0].index);
            }
            else if (candidates.Count > 1)
            {
                var selected = candidates[GameManager.rngFree.Range(0, candidates.Count)];

                dist += next.cell.GetJunctionDistance(selected.cell);
                if (dist > maxTravelDist)
                    break;

                cellsToExplore.Enqueue(selected);
                indices.Add(selected.index);
            }
        }

        return new ChartedPath(cells.ToArray(), indices.ToArray());
    }

    public ChartedPath ChartPath(MazeCell from, MazeCell to)
    {
        if (graph.BiDirSearch(from, to, out ChartedPath foundPath))
            return foundPath;
        else
            Debug.LogError("Charted path could not be found");

        return new ChartedPath();
    }

    public (MazeCell cell, int index) GetSearchStart(MazeCell current, MazeCell observation)
    {
        var shareIndex = GraphFinder.GetClosestSharedIndex(current, observation);

        if(shareIndex != -1)
        {
            var ends = GraphFinder.Areas[shareIndex].ends;

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
                {
                    var currentClosest = current.GetClosestJunction();
                    var observationClosest = observation.GetClosestJunction();

                    if(currentClosest == observationClosest)
                    {
                        if (current.GetJunctionDistance(currentClosest) < observation.GetJunctionDistance(observationClosest))
                            return (observation.GetFarthestJunction(), shareIndex);
                        else
                            return (observation.GetClosestJunction(), shareIndex);
                    }
                    else
                        return (observation.GetClosestJunction(current.GetClosestJunction()), shareIndex);
                }
            }
            else
            {
                if (observation.MeasuredEnds[0].Value <= endProximityThreshold)
                    return (observation.MeasuredEnds[0].Key, shareIndex);
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
                    return (path[i], GraphFinder.GetClosestSharedIndex(path[i], path[i - 1]));
            }
        }

        return (null, -1);
    }

    public ChartedPath RequestPathLoop() => graph.GetLoop();
    public bool MapHasCycles => GraphFinder.HasCycles;
}
