using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Diagnostics;

public enum IndexPriority
{
    None = -1,
    High,
    Critical
}

public enum SortType
{
    Weight,
    Score,
    WeightedScore
}

public class GraphFinder : MonoBehaviour
{
    #region Data

    // STATIC

    // PUBLIC
    public static Dictionary<int, AreaData> Areas;
    public static List<KeyValuePair<HashSet<int>, IsolatedAreaData>> IsolatedAreas;
    public static List<int> FinalGraphIndices;
    //properties
    public static bool HasCycles => cycles.Count > 0;

    // PRIVATE
    static Dictionary<int, MazeCell> indexedJunctions = new Dictionary<int, MazeCell>();
    static List<(int[] nodes, int[] edges)> cycles = new List<(int[] nodes, int[] edges)>();
    static EdgeData[] LabelledGraphConnections;
    static int maxLoopSearchDepth;
    static int maxRecursionDepth;

    // INSTANCE

    // PUBLIC
    [HideInInspector] public Maze maze;
    [HideInInspector] public Spotfinder spotfinder;
    public List<KeyValuePair<int, float>> weightedAreas;

    // SERIALIZED
    [SerializeField, Tooltip("Min max percent thresholds for isolated areas")]
    MinMaxData areaSizeThreshold;
    [SerializeField] int loopSearchLimit = 50, recursionLimit = 20;
    [SerializeField] bool showDebugDisplay = false;

    // PRIVATE
    int index;
    Queue<MazeCell> frontier = new Queue<MazeCell>();
    List<MazeCell> currentArea = new List<MazeCell>();
    List<MazeCell> ends = new List<MazeCell>();
    bool[,] visited;
    List<HashSet<int>> isolatedAreas = new List<HashSet<int>>();
    // bidirectional BFS search collections
    bool[] fromVisited;
    bool[] toVisited;
    (int node, int graphEdge)[] fromParent;
    (int node, int graphEdge)[] toParent;

    #endregion

    #region MonoBehaviour

    private void Awake()
    {
        maxLoopSearchDepth = loopSearchLimit;
        maxRecursionDepth = recursionLimit;

        //GameManager.MazeGenFinished += Initialize;
        //GameManager.MazeGenFinished += CalculateAllVantageScores;
    }

    private void OnDisable()
    {
        //GameManager.MazeGenFinished -= Initialize;
        //GameManager.MazeGenFinished -= CalculateAllVantageScores;
    }

    #endregion

    #region Getters and Setters

    public void SetAllAreaParams()
    {
        foreach(var area in Areas)
        {
            area.Value.SetWeight(GetGraphAreaWeight(area.Key));

            var vantagePoints = new List<KeyValuePair<float, MazeCell>>();
            foreach (var cell in area.Value.all)
                vantagePoints.Add(new KeyValuePair<float, MazeCell>(cell.VantageScore, cell));
            vantagePoints.Sort((a, b) => a.Key.CompareTo(b.Key));
            area.Value.SetVantagePoints(vantagePoints);

            if (area.Value.all.Contains(GameManager.StartCell) || area.Value.all.Contains(GameManager.EndCell))
                area.Value.SetIsEntranceOrExit(true);

            area.Value.FindAndSetPlacement();
            area.Value.CalculateWeightedScore();
        }

        foreach(var isolated in IsolatedAreas)
        {
            foreach (var index in isolated.Key)
                Areas[index].SetIsIsolated(true);

            isolated.Value.CalculatePlacementScore(isolated.Key);
            isolated.Value.CalculateWeightedScore(isolated.Key);
        }
    }

    public static List<KeyValuePair<int, AreaData>> GetAreasSorted(SortType type, bool descending = false)
    {
        List<KeyValuePair<int, AreaData>> list = new List<KeyValuePair<int, AreaData>>(Areas);

        switch (type)
        {
            case SortType.Weight:
                if(!descending)
                    list = list.OrderBy(kvp => kvp.Value.weight).ToList();
                else
                    list = list.OrderByDescending(kvp => kvp.Value.weight).ToList();
                break;
            case SortType.Score:
                if(!descending)
                    list = list.OrderBy(kvp => kvp.Value.placementScore).ToList();
                else
                    list = list.OrderByDescending(kvp => kvp.Value.placementScore).ToList();
                break;
            case SortType.WeightedScore:
                if (!descending)
                    list = list.OrderBy(kvp => kvp.Value.weightedScore).ToList();
                else
                    list = list.OrderByDescending(kvp => kvp.Value.weightedScore).ToList();
                break;
            default:
                break;
        }

        return list;
    }

    public static List<KeyValuePair<HashSet<int>, IsolatedAreaData>> GetIsolatedAreasSorted(SortType type, bool descending = false)
    {
        List<KeyValuePair<HashSet<int>, IsolatedAreaData>> list = new List<KeyValuePair<HashSet<int>, IsolatedAreaData>>(IsolatedAreas);

        switch (type)
        {
            case SortType.Weight:
                if (!descending)
                    list = list.OrderBy(kvp => kvp.Value.weight).ToList();
                else
                    list = list.OrderByDescending(kvp => kvp.Value.weight).ToList();
                break;
            case SortType.Score:
                if (!descending)
                    list = list.OrderBy(kvp => kvp.Value.placementScore).ToList();
                else
                    list = list.OrderByDescending(kvp => kvp.Value.placementScore).ToList();
                break;
            case SortType.WeightedScore:
                if (!descending)
                    list = list.OrderBy(kvp => kvp.Value.weightedScore).ToList();
                else
                    list = list.OrderByDescending(kvp => kvp.Value.weightedScore).ToList();
                break;
            default:
                break;
        }

        return list;
    }

    public HashSet<MazeCell> GetIsolatedAreaEntryPoints(HashSet<int> isolatedArea)
    {
        var points = new HashSet<MazeCell>();

        foreach (var index in isolatedArea)
            foreach (var end in Areas[index].ends)
                foreach (var connectedIndex in end.GetGraphAreaIndices())
                    if (!isolatedArea.Contains(connectedIndex))
                        points.Add(end);

        return points;
    }

    public void CalculateAllVantageScores()
    {
        foreach (var cell in AreaFinder.walkableArea)
            cell.CalculateVantageScore();
    }

    public void RegisterPriorityIndex(int index, IndexPriority priority)
    {
        UnityEngine.Debug.Assert(Areas.ContainsKey(index));
        Areas[index].SetPriority(priority);
    }

    public List<int> RequestPriorityIndices(IndexPriority priority)
    {
        var indices = new List<int>();

        foreach (var area in Areas)
            if (area.Value.priority == priority)
                indices.Add(area.Key);

        return indices;
    }

    List<(int node, int edge)> GetChartedConnections(int index)
    {
        var connections = new List<(int node, int edge)>();

        for (int i = 0; i < LabelledGraphConnections.Length; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                if (LabelledGraphConnections[i][j] == index)
                    connections.Add((LabelledGraphConnections[i][(j + 1) % 2], LabelledGraphConnections[i][2]));
            }
        }

        return connections;
    }

    public int GetJunctionCellCount(int from, int to) => GetJunctionCells(from, to).Count;
    public List<MazeCell> GetJunctionCells(int from, int to)
    {
        var junctionCells = new List<MazeCell>();

        foreach (var endFrom in Areas[from].ends)
        {
            if (Areas[to].ends.Contains(endFrom) && !junctionCells.Contains(endFrom))
                junctionCells.Add(endFrom);
        }

        return junctionCells;
    }

    public int GetJunctionCellCount(int index) => GetJunctionCells(index).Count;
    public List<MazeCell> GetJunctionCells(int index)
    {
        var junctionCells = new List<MazeCell>();

        foreach (var end in Areas[index].ends)
            if (end.GraphAreaIndices.Count > 1)
                junctionCells.Add(end);

        return junctionCells;
    }

    public int GetConnectedIndexCount(int index) => GetConnectedIndices(index).Count;
    public List<int> GetConnectedIndices(int index)
    {
        List<int> indices = new List<int>();

        foreach (var cell in Areas[index].ends)
        {
            //UnityEngine.Debug.Log($"Searching {cell.gameObject.name} for indices connected except {index}");

            foreach (var key in cell.GraphAreaIndices)
            {
                if (key != index && !indices.Contains(key))
                    indices.Add(key);
            }
        }

        return indices;
    }

    public void AddToGraphArea(int index, List<MazeCell> area, List<MazeCell> ends = null)
    {
        if (!Areas.ContainsKey(index))
        {
            UnityEngine.Debug.Log($"{gameObject.name} does not have a graph key for {index}");
            return;
        }

        var cellsToAdd = new List<MazeCell>();
        foreach (var cell in area)
        {
            if (!Areas[index].all.Contains(cell))
                cellsToAdd.Add(cell);
        }

        Areas[index].all.AddRange(cellsToAdd);


        if (ends != null)
        {
            var endsToAdd = new List<MazeCell>();

            foreach (var cell in ends)
            {
                if (!Areas[index].ends.Contains(cell))
                    endsToAdd.Add(cell);
            }

            Areas[index].ends.AddRange(endsToAdd);
        }
    }

    public int GetSmallestAreaIndex(MazeCell cell, int indexToIgnore = -1)
    {
        int lowestIndex = -1;

        // exaggerating value for min check
        int areaCount = 1000;

        foreach (var key in cell.GraphAreaIndices)
        {
            var part = Areas[key];

            if (indexToIgnore > -1 && key == indexToIgnore)
                continue;

            if (part.all.Count < areaCount)
            {
                areaCount = part.all.Count;
                lowestIndex = key;
            }
        }

        if (lowestIndex < 0)
        {
            UnityEngine.Debug.Log($"Smallest area index not found for {gameObject.name}");
            return -1;
        }

        return lowestIndex;
    }

    public int GetLargestAreaIndex(MazeCell cell, int indexToIgnore = -1)
    {
        int highestIndex = -1;

        // exaggerating value for min check
        int areaCount = 0;

        foreach (var key in cell.GraphAreaIndices)
        {
            var part = Areas[key];

            if (indexToIgnore > -1 && key == indexToIgnore)
                continue;

            if (part.all.Count > areaCount)
            {
                areaCount = part.all.Count;
                highestIndex = key;
            }
        }

        if (highestIndex < 0)
        {
            UnityEngine.Debug.Log($"Largest area index not found for {gameObject.name}");
            return -1;
        }

        return highestIndex;
    }

    public static int GetRandomAreaIndex() => FinalGraphIndices[GameManager.rngFree.Range(0, FinalGraphIndices.Count)];
    public static int GetRandomAreaIndex(int avoidIndex)
    {
        var list = new List<int>(FinalGraphIndices);
        if (list.Contains(avoidIndex))
            list.Remove(avoidIndex);

        return list[GameManager.rngFree.Range(0, list.Count)];
    }

    public List<MazeCell> GetConnections(MazeCell cell, bool includeSelf = false)
    {
        if (!cell.IsGraphConnection)
            return new List<MazeCell>(Areas[cell.GetGraphAreaIndices()[0]].ends);
        else
        {
            var connections = new List<MazeCell>();

            foreach (var key in cell.GraphAreaIndices)
            {
                foreach (var item in Areas[key].ends)
                {
                    if (!includeSelf && item == cell)
                        continue;

                    connections.Add(item);
                }
            }

            return connections;
        }
    }

    public List<(MazeCell cell, int graphIndex)> GetLabelledConnections(MazeCell cell, bool includeDeadEnds = true)
    {
        if (!cell.IsGraphConnection)
        {
            UnityEngine.Debug.Log($"{cell.gameObject.name} is not a graph connection, returning null");
            return null;
        }

        var connections = new List<(MazeCell cell, int graphIndex)>();

        foreach (var key in cell.GraphAreaIndices)
        {
            foreach (var item in Areas[key].ends)
            {
                if (item == cell || (!includeDeadEnds && !item.IsLockedConnection))
                    continue;

                connections.Add((item, key));
            }
        }

        return connections;
    }

    public List<MazeCell> GetConnections(MazeCell cell, int index, bool includeSelf = false)
    {
        if (!cell.IsGraphConnection)
        {
            var existingIndex = cell.GetGraphAreaIndices()[0];

            if (index != existingIndex)
                UnityEngine.Debug.Log($"{gameObject.name} does not belong to {index} returning only available index at {existingIndex}");

            return new List<MazeCell>(Areas[existingIndex].ends);
        }
        else
        {
            var connections = new List<MazeCell>();

            foreach (var item in Areas[index].ends)
            {
                if (!includeSelf && item == cell)
                    continue;

                connections.Add(item);
            }

            return connections;
        }
    }

    public int GetOtherConnectionCount(MazeCell cell, int fromIndex, bool getDeadEnds = true, int requiredIndex = -1)
        => GetOtherConnections(cell, fromIndex, getDeadEnds, requiredIndex).Count;

    public List<(MazeCell cell, int index)> GetOtherConnections(MazeCell cell, int fromIndex, bool getDeadEnds = true, int requiredIndex = -1)
    {
        if (!cell.IsGraphConnection)
        {
            int selected = cell.GetGraphAreaIndices()[0];
            var list = new List<(MazeCell cell, int index)>();
            foreach (var end in Areas[selected].ends)
                list.Add((end, selected));

            return list;
        }
        else
        {
            var connections = new List<(MazeCell cell, int index)>();

            foreach (var key in cell.GraphAreaIndices)
            {
                if (key == fromIndex || (requiredIndex > -1 && key != requiredIndex))
                    continue;


                foreach (var item in Areas[key].ends)
                {
                    if (item == cell || (!getDeadEnds && !item.IsLockedConnection))
                        continue;

                    connections.Add((item, key));
                }
            }

            return connections;
        }
    }

    public static int GetAreaCellCount(int index) => Areas[index].all.Count;
    public static int GetAreaCellCount(List<int> indices)
    {
        int total = 0;

        foreach (var area in Areas)
            if (indices.Contains(area.Key))
                total += area.Value.all.Count;

        return total;
    }

    public int GetAreaCellCount(MazeCell cell, int index = -1) => GetAreaCells(cell, index).Count;

    public List<MazeCell> GetAreaCells(MazeCell cell, int index = -1)
    {
        if (index == -1)
        {
            if (cell.IsJunction)
                return null;

            return new List<MazeCell>(Areas[cell.GetGraphAreaIndices()[0]].all);
        }
        else
        {
            if (!cell.GraphAreaIndices.Contains(index))
                return null;

            return new List<MazeCell>(Areas[index].all);
        }

    }

    public ChartedPath GetLoop(int index = -1)
    {
        int indexToUse = index >= 0 ? index : GameManager.rngFree.Range(0, cycles.Count);

        var cycle = cycles[indexToUse];
        var waypoints = new MazeCell[cycle.nodes.Length];
        var graphIndices = new int[cycle.edges.Length];

        for (int i = 0; i < cycle.nodes.Length; i++)
        {
            waypoints[i] = indexedJunctions[cycle.nodes[cycle.nodes.Length - 1 - i]];
            graphIndices[i] = cycle.edges[cycle.edges.Length - 1 - i];
        }

        return new ChartedPath(waypoints, graphIndices);
    }

    public ChartedPath GetLoop((int[] nodes, int[] edges) cycle)
    {
        var waypoints = new MazeCell[cycle.nodes.Length];
        var graphIndices = new int[cycle.edges.Length];

        for (int i = 0; i < cycle.nodes.Length; i++)
        {
            waypoints[i] = indexedJunctions[cycle.nodes[cycle.nodes.Length - 1 - i]];
            graphIndices[i] = cycle.edges[cycle.edges.Length - 1 - i];
        }

        return new ChartedPath(waypoints, graphIndices);
    }

    public static bool IsDeadEnd(int index)
    {
        foreach (var end in Areas[index].ends)
            if (end.IsDeadEnd)
            {
                return true;
            }

        return false;
    }

    static int GetSharedIndexCount(MazeCell from, MazeCell to) => GetSharedIndices(from, to).Count;

    static List<int> GetSharedIndices(MazeCell from, MazeCell to)
    {
        var indices = new List<int>();

        foreach (var index in from.GetGraphAreaIndices())
            foreach (var otherIndex in to.GetGraphAreaIndices())
                if (index == otherIndex)
                    indices.Add(index);

        return indices;
    }

    public static int GetClosestSharedIndex(MazeCell from, MazeCell to)
    {
        var indices = new List<int>();

        foreach (var index in from.GetGraphAreaIndices())
            foreach (var otherIndex in to.GetGraphAreaIndices())
                if (index == otherIndex)
                    indices.Add(index);

        if (indices.Count == 0)
            return -1;
        else if (indices.Count == 1)
            return indices[0];
        else
        {
            int closestIndex = -1;
            int shortestPathCount = 10000;

            foreach (var index in indices)
            {
                var path = PathRequestManager.RequestPathImmediate(from, to, index);

                if (path.Count < shortestPathCount || closestIndex == -1)
                {
                    closestIndex = index;
                    shortestPathCount = path.Count;
                }
            }

            return closestIndex;
        }
    }

    public MazeCell GetSharedJunction(int from, int to)
    {
        foreach (var end in Areas[from].ends)
            foreach (var other in Areas[to].ends)
                if (end == other)
                    return end;

        return null;
    }

    public bool ShareIndex(MazeCell from, MazeCell to)
    {
        foreach (var index in from.GraphAreaIndices)
            foreach (var otherIndex in to.GraphAreaIndices)
                if (index == otherIndex)
                    return true;

        return false;
    }

    public static MazeCell GetRandomCellFromGraphArea(int index)
    {
        var cells = Areas[index].all;
        return cells[GameManager.rngFree.Range(0, cells.Count - 1)];
    }

    public static MazeCell GetRandomCellFromGraphArea(List<int> indices)
    {
        var index = indices[GameManager.rngFree.Range(0, indices.Count)];
        var cells = Areas[index].all;
        return cells[GameManager.rngFree.Range(0, cells.Count)];
    }

    public HashSet<int> GetIsolatedArea() => new HashSet<int>(isolatedAreas[GameManager.rngFree.Range(0, isolatedAreas.Count)]);

    public HashSet<int> GetIsolatedArea(HashSet<int> exclude)
    {
        var temp = new List<HashSet<int>>(isolatedAreas);
        var mark = new HashSet<int>();

        foreach (var t in temp)
            if (t.Overlaps(exclude))
            {
                mark = t;
                break;
            }

        temp.Remove(mark);

        return new HashSet<int>(temp[GameManager.rngFree.Range(0, temp.Count)]);
    }

    public HashSet<int> GetIsolatedArea(List<HashSet<int>> excludes)
    {
        var temp = new List<HashSet<int>>(isolatedAreas);
        var marks = new List<HashSet<int>>();

        foreach (var t in temp)
        {
            foreach (var e in excludes)
            {
                if (t.Overlaps(e))
                {
                    marks.Add(t);
                    break;
                }
            }
        }

        foreach (var mark in marks)
            temp.Remove(mark);

        if (temp.Count == 0)
            return null;
        else
            return new HashSet<int>(temp[GameManager.rngFree.Range(0, temp.Count)]);
    }

    public static float GetGraphAreaWeight(int index) => Areas[index].all.Count / (float)AreaFinder.WalkableCellCount;
    public static float GetGraphAreaWeight(List<int> indices)
    {
        var finalCells = new List<MazeCell>();

        foreach (var index in indices)
        {
            foreach (var cell in Areas[index].all)
                if (!finalCells.Contains(cell))
                    finalCells.Add(cell);
        }

        return finalCells.Count / (float)AreaFinder.WalkableCellCount;
    }

    public List<(HashSet<int> area, int count)> GetMatchingIsolatedAreas(List<int> indicesToMatch)
    {
        var areas = new List<(HashSet<int> area, int count)>();

        foreach (var area in isolatedAreas)
        {
            int count = 0;
            bool includes = false;

            foreach (var index in indicesToMatch)
            {
                if (area.Contains(index))
                {
                    includes = true;
                    count++;
                }
            }

            if (includes)
                areas.Add((area, count));
        }

        areas.OrderBy(x => x.count);

        return areas;
    }

    public List<(ChartedPath loop, int count)> GetMatchingLoops(List<int> indicesToMatch)
    {
        var loops = new List<(ChartedPath loop, int count)>();

        foreach (var cycle in cycles)
        {
            int count = 0;
            bool includes = false;

            foreach (var index in indicesToMatch)
            {
                if (cycle.edges.Contains(index))
                {
                    includes = true;
                    count++;
                }
            }

            if (includes)
                loops.Add((GetLoop(cycle), count));
        }

        loops.OrderBy(x => x.count);

        return loops;
    }

    public HashSet<int> GetFloodCoverage(int startIndex, float coverageLimit)
    {
        var final = new HashSet<int> { startIndex };

        Queue<int> candidates = new Queue<int>();
        var visited = new List<int>();

        var neighbors = GetConnectedIndices(startIndex);
        neighbors.Shuffle();

        float currentCoverage = GetGraphAreaWeight(startIndex);

        foreach (var neighbor in neighbors)
            candidates.Enqueue(neighbor);


        while (currentCoverage < coverageLimit)
        {
            var next = candidates.Dequeue();
            var nextWeight = GetGraphAreaWeight(next);

            visited.Add(next);

            if (currentCoverage + nextWeight < coverageLimit)
            {
                final.Add(next);
                currentCoverage += nextWeight;

                var others = GetConnectedIndices(next);

                foreach (var other in others)
                    if (!visited.Contains(other))
                        candidates.Enqueue(other);
            }
            else
                break;
        }

        return final;
    }

    public KeyValuePair<int, float> GetWeightedArea(int index)
    {
        foreach (var pair in weightedAreas)
            if (pair.Key == index)
                return pair;

        return new KeyValuePair<int, float>(-1, 0);
    }

    #endregion

    #region Graph Search

    public void Initialize()
    {
        CreateGraph();
        InitSearchCollections();
    }

    public void CreateGraph()
    {
        var timer = new Stopwatch();
        timer.Start();

        Areas = new Dictionary<int, AreaData>();
        visited = new bool[maze.size.x, maze.size.y];
        frontier.Enqueue(GameManager.StartCell);

        index = 0;

        // PASS 1

        while(frontier.Count > 0)
        {
            currentArea.Clear();
            ends.Clear();

            var nextCell = frontier.Dequeue();
            SearchCell(nextCell);

            if (currentArea.Count <= 1)
                continue;

            foreach (var end in ends)
                end.IsGraphConnection = true;

            foreach (var cell in currentArea)
            {
                cell.SetGraphArea(index);
            }
            
            Areas.Add(index, new AreaData(new List<MazeCell>(currentArea), new List<MazeCell>(ends)));

            index++;
        }

        // PASS 2

        List<MazeCell> cellsToTest = new List<MazeCell>();

        foreach (var part in Areas)
        {
            var count = part.Value.all.Count;

            if(count > 3)
            {

                foreach (var cell in part.Value.ends)
                    if (cell.connectedCells.Count >= 3)
                    {
                        cell.IsLockedConnection = true;
                        visited[cell.pos.x, cell.pos.y] = false;
                    }

                if (part.Value.ends.Count != 2)
                    continue;

                if (part.Value.ends[0].connectedCells.Contains(part.Value.ends[1]))
                    foreach (var cell in part.Value.all)
                        visited[cell.pos.x, cell.pos.y] = false;
            }
            else if(count == 3)
            {
                MazeCell middleCell = null;
                foreach(var cell in part.Value.all)
                {
                    if (cell.connectedCells.Count == 2)
                    { middleCell = cell; break; }
                }

                if (!middleCell.IsInternalCorner())
                {
                    foreach (var cell in part.Value.ends)
                        if (cell.connectedCells.Count >= 3)
                        {
                            cell.IsLockedConnection = true;
                            visited[cell.pos.x, cell.pos.y] = false;
                        }
                }
                else
                {
                    foreach(var cell in part.Value.all)
                        if(cell.connectedCells.Count >= 2)
                            visited[cell.pos.x, cell.pos.y] = false;
                }
            }
            else if (count == 2)
            {
                bool isSingle = false;
                MazeCell mainNode = null;
                MazeCell deadEnd = null;

                foreach(var end in part.Value.ends)
                {
                    if(end.connectedCells.Count == 1)
                    {
                        isSingle = true;
                        deadEnd = end;
                    }
                    else
                    {
                        mainNode = end;
                        visited[end.pos.x, end.pos.y] = false;
                    }

                }

                if (isSingle)
                {
                    mainNode.IsLockedConnection = true;

                    // this is to clear the single cell dead end from the ends lists before the merging pass
                    // this way, any 2-cell dead-end area is merged without keeping the dead-end
                    part.Value.ends.Remove(deadEnd);
                }
                else
                    foreach (var end in part.Value.ends)
                        cellsToTest.Add(end);
            }
        }

        HashSet<int> indicesToMerge = new HashSet<int>();

        foreach(var testCell in cellsToTest)
        {
            currentArea.Clear();
            ends.Clear();
            indicesToMerge.Clear();

            SearchCellForMerge(testCell, indicesToMerge);

            if (currentArea.Count <= 1)
                continue;

            var temp = new HashSet<int>();
            foreach (var coveredIndex in indicesToMerge)
            {
                int i = 0;
                
                foreach (var cell in currentArea)
                    if (cell.GraphAreaIndices.Contains(coveredIndex))
                        i++;

                if (i == 1 || (i == 2 && Areas[coveredIndex].all.Count >= 3))
                    temp.Add(coveredIndex);
                
            }

            foreach(var t in temp)
                indicesToMerge.Remove(t);
            

            foreach(var cell in currentArea)
            {
                if(cell.IsLockedConnection && ends.Contains(cell))
                {
                    bool isInternal = true;

                    foreach (var other in cell.connectedCells)
                    {
                        if (!currentArea.Contains(other))
                        {
                            isInternal = false;
                            break;
                        }
                    }

                    if (isInternal)
                    {
                        ends.Remove(cell);
                        cell.IsLockedConnection = false;
                    }
                }
            }

            foreach(var cell in currentArea)
            {
                if (cell.IsLockedConnection)
                    foreach(var indexToMerge in indicesToMerge)
                        cell.RemoveGraphArea(indexToMerge);
                else
                    cell.GraphAreaIndices.Clear();

                cell.SetGraphArea(index);
            }

            foreach(var indexToMerge in indicesToMerge)
                if (Areas.ContainsKey(indexToMerge))
                    Areas.Remove(indexToMerge);
            

            if (!Areas.ContainsKey(index))
                Areas.Add(index, new AreaData(new List<MazeCell>(currentArea), new List<MazeCell>(ends)));
            

            index++;
        }

        // PASS 3

        List<int> coveredIndices = new List<int>();
        List<int> internalCornerIndices = new List<int>();

        for(int i = 0; i < maze.size.x; i++)
        {
            for(int j = 0; j < maze.size.y; j++)
            {
                var cell = maze.cells[i, j];

                if (cell.state > 1 || cell.GraphAreaIndices.Count > 1)
                    continue;

                var index = cell.GetGraphAreaIndices()[0];

                if (coveredIndices.Contains(index))
                    continue;
                 
                var junctions = GetJunctionCells(index);
                var junctionCount = junctions.Count;

                if (junctionCount == 1 && Areas[index].all.Count == 2)
                {
                    var smallestAreaIndex = GetSmallestAreaIndex(junctions[0], index);
                    MergeAreas(index, smallestAreaIndex, true);
                }
                else if (junctionCount == 2 && Areas[index].all.Count == 3)
                {
                    if (cell.IsInternalCorner() && !internalCornerIndices.Contains(index))
                    {
                        internalCornerIndices.Add(index);

                        for(int k = 0; k < MazeDirections.diagonalVectors.Length; k++)
                        {
                            var newX = i + MazeDirections.diagonalVectors[k].x;
                            var newY = j + MazeDirections.diagonalVectors[k].y;

                            if (newX < 0 || newY < 0 || newX >= maze.size.x || newY >= maze.size.y)
                                continue;

                            var otherCell = maze.cells[newX, newY];

                            bool hasConnection = (cell.diagonalBits & 1 << k) != 0;

                            if(hasConnection && otherCell.IsInternalCorner())
                            {
                                var otherIndex = otherCell.GetGraphAreaIndices()[0];
                                MergeAreas(index, otherIndex);
                                coveredIndices.Add(otherIndex);

                                if (!internalCornerIndices.Contains(otherIndex))
                                    internalCornerIndices.Add(otherIndex);
                            }
                        }

                    }
                }

                coveredIndices.Add(index);
            }
        }

        // Pass 4
        // Record sorted distance list of ends for each cell for quick decision making later
        List<(int from, int to)> edges = new List<(int from, int to)>();
        int endCounter = 0;
        var currentEnds = new List<MazeCell>();

        List<EdgeData> labelledEdges = new List<EdgeData>();
        List<MazeCell> pruningList = new List<MazeCell>();

        foreach(var area in Areas)
        {
            foreach(var cell in area.Value.all)
            {
                cell.SetDistanceToJunctions(area.Value.ends);
            }

            if(area.Value.ends.Count == 1)
            {
                var end = area.Value.ends[0];
                end.DeadConnectionCount++;

                if (!pruningList.Contains(end) && end.GetGraphAreaIndices().Count <= 2)
                    pruningList.Add(end);
            }
            else
            {
                foreach (var end in area.Value.ends)
                    if (end.IsDeadEnd && !pruningList.Contains(end))
                        pruningList.Add(end);
            }

            foreach (var end in area.Value.ends)
            {
                if (!currentEnds.Contains(end))
                {
                    currentEnds.Add(end);
                    end.EndIndex = endCounter;

                    endCounter++;
                }
            }
        }

        indexedJunctions.Clear();

        foreach(var end in currentEnds)
        {
            var junctionIndex = end.EndIndex;

            if (!indexedJunctions.ContainsKey(junctionIndex))
                indexedJunctions.Add(junctionIndex, end);

            var labelledConnections = GetLabelledConnections(end, true);

            foreach(var connection in labelledConnections)
            {
                var connectionIndex = connection.cell.EndIndex;
                var graphIndex = connection.graphIndex;

                if (!currentEnds.Contains(connection.cell)
                    || labelledEdges.Contains(new EdgeData(junctionIndex, connectionIndex, graphIndex))
                    || labelledEdges.Contains(new EdgeData(connectionIndex, junctionIndex, graphIndex)))
                    continue;

                if (junctionIndex < connectionIndex)
                    labelledEdges.Add(new EdgeData(junctionIndex, connectionIndex, graphIndex));
                else
                    labelledEdges.Add(new EdgeData(connectionIndex, junctionIndex, graphIndex));
            }
        }

        // clear lists of isolated areas
        isolatedAreas.Clear();

        // a bit poorly structured so making note
        // the recursive pruning of the loop tree
        // also tracks the isolated areas and puts them in hashsets for future use
        PruneTreeAndTrackIsolated(pruningList);

        // remove isolated areas that are subsets of others
        TrimIsolatedAreas();

        // assign area weights
        AssignAreaWeights();

        // assign isolated entry points to cells in isolated areas
        AssignIsolatedEntryCells();

        int edgeCount = 0;
        LabelledGraphConnections = new EdgeData[labelledEdges.Count];
        edgeCount = 0;

        HashSet<int> prunedIndices = new HashSet<int>();
        foreach(var edge in labelledEdges)
        {
            LabelledGraphConnections[edgeCount].from = edge.from;
            LabelledGraphConnections[edgeCount].to = edge.to;
            LabelledGraphConnections[edgeCount].graphIndex = edge.graphIndex;

            var from = indexedJunctions[edge.from];
            var to = indexedJunctions[edge.to];


            if (from.IsUnloopable || to.IsUnloopable
                || !from.IsLockedConnection || !to.IsLockedConnection)
                prunedIndices.Add(edgeCount);

            edgeCount++;
        }

        var prunedEdges = new EdgeData[LabelledGraphConnections.Length - prunedIndices.Count];
        int h = 0;
        for(int i = 0; i < LabelledGraphConnections.Length; i++)
        {
            if (!prunedIndices.Contains(i))
            {
                prunedEdges[h] = LabelledGraphConnections[i];
                h++;
            }
        }

        // Construct edge graph for cycle detection
        cycles.Clear();

        List<int> usedIndices = new List<int>();

        for(int i = 0; i < prunedEdges.Length; i++)
        {
            for(int j = 0; j <= 1; j++)
            {
                var label = prunedEdges[i][j];

                if (!indexedJunctions[label].IsJunction || usedIndices.Contains(label))
                    continue;

                findNewCycles(new int[] { label });
                usedIndices.Add(label);
            }

            if (cycles.Count >= maxLoopSearchDepth)
            {
                //UnityEngine.Debug.Log("Loop limit reached, breaking.");
                break;
            }
        }

        LabelledGraphConnections.OrderBy(x => x.from);
        cycles.OrderBy(x => x.nodes.Length);

        timer.Stop();
        UnityEngine.Debug.Log($"Graph and Loop finding took: {timer.ElapsedMilliseconds}ms");

        // keeping track of the final graph indices used on map
        // for easy access. these are the keys to GraphAreas
        FinalGraphIndices = new List<int>();
        foreach (var area in Areas)
            FinalGraphIndices.Add(area.Key);

        // DISPLAY

        if (showDebugDisplay)
        {
            Color junctionColor = new Color(1f, 0f, 0.1f);

            foreach(var area in Areas)
            {
                var randomColor = new Color(GameManager.rngFree.Range(0f, 1f), GameManager.rngFree.Range(0f, 1f), GameManager.rngFree.Range(0f, 1f));

                foreach(var cell in area.Value.all)
                {
                    string indices = "";

                    foreach (var key in cell.GraphAreaIndices)
                        indices += key.ToString() + " ";

                    if (!cell.IsLockedConnection || cell.GraphAreaIndices.Count == 1)
                        cell.DisplayText(indices, randomColor);
                    else
                        cell.DisplayText(indices, junctionColor);
                }
            }
        }

    }

    void SearchCell(MazeCell currentCell, MazeCell cameFrom = null)
    {
        if (visited[currentCell.pos.x, currentCell.pos.y])
            return;

        if (currentCell.connectedCells.Count < 3 || (currentCell.connectedCells.Count >= 3 && currentCell.UnexploredDirectionCount == 1))
            visited[currentCell.pos.x, currentCell.pos.y] = true;

        if(!currentArea.Contains(currentCell))
            currentArea.Add(currentCell);

        int count = currentCell.connectedCells.Count;

        if (count == 1)
        {
            if(!ends.Contains(currentCell))
                ends.Add(currentCell);

            if (currentArea.Count == 1)
                foreach (var cell in currentCell.connectedCells)
                    SearchCell(cell, currentCell);
                
        }
        else if (count == 2)
        {
            foreach(var cell in currentCell.connectedCells)
            {
                if (cameFrom != null && cell == cameFrom)
                    continue;

                SearchCell(cell, currentCell);
            }
        }
        else
        {
            if (!ends.Contains(currentCell))
                ends.Add(currentCell);

            if(currentArea.Count == 1)
            {
                foreach(var cell in currentCell.connectedCells)
                {
                    if ((cell.connectedCells.Count < 3 && !visited[cell.pos.x, cell.pos.y]) || (cell.connectedCells.Count >= 3 && !HasMadeConnection(currentCell, cell)))
                    {
                        SearchCell(cell, currentCell);
                        break;
                    }
                }
            }

            if(currentCell.UnexploredDirectionCount > 0)
                currentCell.UnexploredDirectionCount--;

            if (currentCell.UnexploredDirectionCount > 0 && currentCell.LastIndexAddedToQueue != index)
            {
                frontier.Enqueue(currentCell);
                currentCell.LastIndexAddedToQueue = index;
            }
        }
    }

    void SearchCellForMerge(MazeCell currentCell, HashSet<int> coveredIndices)
    {
        if (visited[currentCell.pos.x, currentCell.pos.y])
            return;

        visited[currentCell.pos.x, currentCell.pos.y] = true;

        if(!currentArea.Contains(currentCell))
            currentArea.Add(currentCell);

        if (currentCell.IsLockedConnection && !ends.Contains(currentCell))
            ends.Add(currentCell);

        foreach (var key in currentCell.GraphAreaIndices)
            coveredIndices.Add(key);

        //var currentConnectedCount = currentCell.connectedCells.Count;

        foreach (var cell in currentCell.connectedCells)
        {
            // this is to disallow merging across two junctions
            // across a threshold, meaning that neither neighbor of the junction
            // is connected to the corresponding neighbor of the other
            // this code to separate works
            // but breaks recursive isolated area construction
            // need to fix that to use here

            //if (currentConnectedCount == 3 && cell.connectedCells.Count == 3)
            //{
            //    var bitPatternToCheck = currentCell.cardinalBits;
            //    bitPatternToCheck.RotatePattern(4, 2);

            //    if ((bitPatternToCheck ^ cell.cardinalBits) == 0)
            //        continue;
            //}

            SearchCellForMerge(cell, coveredIndices);
        }

    }

    void MergeAreas(int from, int to, bool dissolve = false)
    {
        var junctionCells = GetJunctionCells(from, to);

        var endsToMerge = new List<MazeCell>(Areas[from].ends);
        var cellsToMerge = new List<MazeCell>(Areas[from].all);
        var junctionsToDissolve = new List<MazeCell>();

        foreach(var cell in junctionCells)
            if (cell.GraphAreaIndices.Count <= 2)
                junctionsToDissolve.Add(cell);

        if (dissolve)
            endsToMerge.Clear();

        AddToGraphArea(to, cellsToMerge, endsToMerge);

        foreach (var junction in junctionsToDissolve)
            Areas[to].ends.Remove(junction);

        foreach(var cell in Areas[from].all.ToList())
        {
            cell.GraphAreaIndices.Remove(from);

            if(!cell.GraphAreaIndices.Contains(to))
                cell.GraphAreaIndices.Add(to);
        }

        Areas.Remove(from);
    }

    bool HasMadeConnection(MazeCell from, MazeCell to)
    {
        foreach(var key in from.GraphAreaIndices)
        {
            if (Areas[key].ends.Contains(to))
                return true;
        }

        return false;
    }

    void PruneTreeAndTrackIsolated(List<MazeCell> ends)
    {
        foreach (var end in ends)
            CheckJunction(end);
    }

    void CheckJunction(MazeCell cell, List<HashSet<int>> areas = null)
    {
        HashSet<int> area;

        bool found = false;

        if (areas == null)
        {
            areas = new List<HashSet<int>>();
            area = new HashSet<int>();
            areas.Add(area);

            var indices = cell.GetGraphAreaIndices();

            foreach (var index in indices)
                if (Areas[index].ends.Count <= 2)
                    area.Add(index);

            if (GetGraphAreaWeight(new List<int>(area)) >= areaSizeThreshold.max)
            {
                areas.Add(new HashSet<int>());
                UnityEngine.Debug.Log("Area size threshold exceeded, starting new hashset");
            }
        }

        area = areas[areas.Count - 1];

        foreach (var connection in GetConnections(cell))
        {
            connection.DeadConnectionCount++;

            if (!connection.IsDeadEnd && connection.DeadConnectionCount == GetConnections(connection).Count - 1)
            {
                found = true;

                // looping through dead ends first
                // in order to prevent isolated areas that skip a dead end
                // and include a corridor
                foreach (var index in connection.GetGraphAreaIndices())
                {
                    if (!IsDeadEnd(index))
                        continue;

                    if (GetGraphAreaWeight(new List<int>(area)) + GetGraphAreaWeight(index) >= areaSizeThreshold.max)
                    {
                        areas.Add(new HashSet<int>());
                        area = areas[areas.Count - 1];

                        //UnityEngine.Debug.Log("Area size threshold exceeded, starting new hashset");
                    }

                    bool duplicate = false;

                    for(int i = 0; i < areas.Count - 1; i++)
                    {
                        if(areas[i].Contains(index))
                        {
                            duplicate = true;
                            break;
                        }
                    }

                    if(!duplicate)
                        area.Add(index);

                }

                foreach (var index in connection.GetGraphAreaIndices())
                {
                    if (IsDeadEnd(index))
                        continue;

                    if (GetGraphAreaWeight(new List<int>(area)) + GetGraphAreaWeight(index) >= areaSizeThreshold.max)
                    {
                        areas.Add(new HashSet<int>());
                        area = areas[areas.Count - 1];

                        //UnityEngine.Debug.Log("Area size threshold exceeded, starting new hashset");
                    }

                    bool duplicate = false;

                    for (int i = 0; i < areas.Count - 1; i++)
                    {
                        if (areas[i].Contains(index))
                        {
                            duplicate = true;
                            break;
                        }
                    }

                    if (!duplicate)
                        area.Add(index);

                }

                connection.IsUnloopable = true;
                CheckJunction(connection, areas);
            }
        }

        if (!found)
        {
            foreach(var final in areas)
            {
                //UnityEngine.Debug.Log($"Adding {areas.Count} new areas");

                if(final.Count > 0)
                    isolatedAreas.Add(final);
            }
        }
    }

    void TrimIsolatedAreas()
    {
        var final = new List<HashSet<int>>();
        var trimPairs = new List<(HashSet<int> area, HashSet<int> other)>();

        foreach (var area in isolatedAreas)
        {
            var setsToMerge = new List<HashSet<int>>();
            bool isSubset = false;
            bool hasOverlap = false;
            bool trim = false;

            foreach (var other in isolatedAreas)
            {
                if (area == other)
                    continue;

                if (area.IsProperSubsetOf(other))
                {
                    isSubset = true;
                    break;
                }
                else if (area.Overlaps(other) && !other.IsSubsetOf(area))
                {
                    var areaWeight = GetGraphAreaWeight(new List<int>(area));
                    var otherWeight = GetGraphAreaWeight(new List<int>(other));

                    if(areaWeight + otherWeight < areaSizeThreshold.max)
                    {
                        setsToMerge.Add(other);
                        hasOverlap = true;
                    }
                    else
                    {
                        if(areaWeight < areaSizeThreshold.max || otherWeight < areaSizeThreshold.max)
                        {
                            if (areaWeight <= otherWeight)
                                other.ExceptWith(area);
                            else
                                area.ExceptWith(other);
                        }
                        else if (!trimPairs.Contains((area, other)) && !trimPairs.Contains((other, area)))
                        {
                            trimPairs.Add((area, other));
                            trim = true;
                        }
                    }
                }
            }

            if (!trim)
            {
                if (!isSubset && !hasOverlap)
                    final.Add(area);
                else if (!isSubset && hasOverlap)
                {
                    var temp = new HashSet<int>(area);
                    foreach (var set in setsToMerge)
                        temp.UnionWith(set);

                    final.Add(temp);
                }
            }
            else
            {
                final.Add(area);
            }
        }

        foreach(var pair in trimPairs)
        {
            var intersect = new HashSet<int>(pair.area);
            intersect.IntersectWith(pair.other);

            string test = "Trim: ";
            test += "First: ";
            foreach (var index in pair.area)
                test += index + ",";
            test += " Second: ";
            foreach (var index in pair.other)
                test += index + ",";
            test += " Intersect: ";
            foreach (var index in intersect)
                test += index + ",";

            UnityEngine.Debug.Log(test);

            pair.area.ExceptWith(intersect);
            pair.other.ExceptWith(intersect);

            if (GetGraphAreaWeight(new List<int>(intersect)) > areaSizeThreshold.min)
                final.Add(intersect);
        }

        isolatedAreas.Clear();

        foreach (var area in final)
        {
            if (area.Count == 0)
                continue;

            bool isMerged = false;
            var merged = new HashSet<int>();

            foreach (var other in final)
            {
                if (area == other)
                    continue;

                // merge dead ends with a single shared junction
                if (area.Count == 1 && other.Count == 1)
                {
                    foreach (var index in area)
                        foreach (var otherIndex in other)
                            if (GetSharedJunction(index, otherIndex) != null
                                && GetGraphAreaWeight(index) + GetGraphAreaWeight(otherIndex) < areaSizeThreshold.max)
                            {
                                merged = new HashSet<int>(area);
                                merged.UnionWith(other);
                                isolatedAreas.Add(merged);
                                isMerged = true;
                            }
                }

                if (other.IsSubsetOf(area))
                    other.ExceptWith(area);

                if (isMerged)
                    other.ExceptWith(merged);
            }

            if(!isMerged)
                isolatedAreas.Add(area);
        }

        //isolatedAreas = final;

        isolatedAreas.Sort((a, b) => a.Count - b.Count);
    }

    void AssignAreaWeights()
    {
        weightedAreas = new List<KeyValuePair<int, float>>();
        IsolatedAreas = new List<KeyValuePair<HashSet<int>, IsolatedAreaData>>();

        foreach (var area in Areas)
        {
            var weight = GetGraphAreaWeight(area.Key);
            weightedAreas.Add(new KeyValuePair<int, float>(area.Key, weight));
        }

        weightedAreas.Sort((a, b) => a.Value.CompareTo(b.Value));

        foreach (var area in isolatedAreas)
        {
            IsolatedAreas.Add(new KeyValuePair<HashSet<int>, IsolatedAreaData>
                                     (area, new IsolatedAreaData(GetGraphAreaWeight(new List<int>(area)), GetIsolatedAreaEntryPoints(area))));
        }

        IsolatedAreas.Sort((a, b) => a.Value.weight.CompareTo(b.Value.weight));
    }

    void AssignIsolatedEntryCells()
    {
        foreach(var area in IsolatedAreas)
        {
            foreach(var index in area.Key)
            {
                foreach(var cell in Areas[index].all)
                {
                    cell.IsolatedEntryPoints = area.Value.entryPoints;
                }
            }
        }
    }

    #endregion

    #region Charted Path & BiDirectional Search

    void InitSearchCollections()
    {
        fromVisited = new bool[indexedJunctions.Count];
        toVisited = new bool[indexedJunctions.Count];
        fromParent = new (int node, int edge)[indexedJunctions.Count];
        toParent = new (int node, int edge)[indexedJunctions.Count];
    }

    public bool BiDirSearch(int from, int to, out ChartedPath foundPath, List<int> avoidIndices = null)
    {
        foundPath = new ChartedPath();

        if (from == -1 || to == -1)
            return false;

        Array.Clear(fromVisited, 0, fromVisited.Length);
        Array.Clear(toVisited, 0, toVisited.Length);
        Array.Clear(fromParent, 0, fromParent.Length);
        Array.Clear(toParent, 0, toParent.Length);

        Queue<int> fromQueue = new Queue<int>();
        Queue<int> toQueue = new Queue<int>();

        int intersectNode = -1;

        fromQueue.Enqueue(from);
        fromVisited[from] = true;
        fromParent[from] = (-1, -1);

        toQueue.Enqueue(to);
        toVisited[to] = true;
        toParent[from] = (-1, -1);

        while(fromQueue.Count > 0 && toQueue.Count > 0)
        {
            if(avoidIndices == null)
            {
                BFS(fromQueue, fromVisited, fromParent);
                BFS(toQueue, toVisited, toParent);
            }
            else
            {
                BFS(fromQueue, fromVisited, fromParent, avoidIndices);
                BFS(toQueue, toVisited, toParent, avoidIndices);
            }

            intersectNode = IsIntersecting(fromVisited, toVisited);

            if(intersectNode != -1)
            {
                foundPath = BuildPath(fromParent, toParent, from, to, intersectNode);
                return true;
            }
            
        }

        return false;
    }

    public bool BiDirSearch(MazeCell fromCell, MazeCell toCell, out ChartedPath foundPath, List<int> avoidIndices = null)
    {
        foundPath = new ChartedPath();

        int from = fromCell.EndIndex;
        int to = toCell.EndIndex;

        if (from == -1 || to == -1)
            return false;

        Array.Clear(fromVisited, 0, fromVisited.Length);
        Array.Clear(toVisited, 0, toVisited.Length);
        Array.Clear(fromParent, 0, fromParent.Length);
        Array.Clear(toParent, 0, toParent.Length);

        Queue<int> fromQueue = new Queue<int>();
        Queue<int> toQueue = new Queue<int>();

        int intersectNode = -1;

        fromQueue.Enqueue(from);
        fromVisited[from] = true;
        fromParent[from] = (-1, -1);

        toQueue.Enqueue(to);
        toVisited[to] = true;
        toParent[from] = (-1, -1);

        while (fromQueue.Count > 0 && toQueue.Count > 0)
        {
            if (avoidIndices == null)
            {
                BFS(fromQueue, fromVisited, fromParent);
                BFS(toQueue, toVisited, toParent);
            }
            else
            {
                BFS(fromQueue, fromVisited, fromParent, avoidIndices);
                BFS(toQueue, toVisited, toParent, avoidIndices);
            }

            intersectNode = IsIntersecting(fromVisited, toVisited);

            if (intersectNode != -1)
            {
                foundPath = BuildPath(fromParent, toParent, from, to, intersectNode);
                return true;
            }

        }

        return false;
    }

    void BFS(Queue<int> queue, bool[] visited, (int, int)[] history, List<int> avoidIndices = null)
    {
        int current = queue.Dequeue();

        foreach(var connection in GetChartedConnections(current))
        {
            if (avoidIndices != null && avoidIndices.Contains(connection.edge))
                continue;

            if (!visited[connection.node])
            {
                history[connection.node] = (current, connection.edge);
                visited[connection.node] = true;
                queue.Enqueue(connection.node);
            }
        }
    }

    int IsIntersecting(bool[] fromVisited, bool[] toVisited)
    {
        for(int i = 0; i < indexedJunctions.Count; i++)
        {
            if (fromVisited[i] && toVisited[i])
                return i;
        }

        return -1;
    }

    ChartedPath BuildPath((int node, int graphEdge)[] fromParent, (int node, int graphEdge)[] toParent, int from, int to, int intersection)
    {
        var cells = new List<MazeCell>();
        var indices = new List<int>();

        int i = intersection;

        cells.Add(indexedJunctions[i]);

        while (i != from)
        {
            cells.Add(indexedJunctions[fromParent[i].node]);
            indices.Add(fromParent[i].graphEdge);
            i = fromParent[i].node;
        }

        cells.Reverse();
        indices.Reverse();

        i = intersection;

        while (i != to)
        {
            cells.Add(indexedJunctions[toParent[i].node]);
            indices.Add(toParent[i].graphEdge);
            i = toParent[i].node;
        }

        //UnityEngine.Debug.Log("Path found");

        //string test = "Path: ";

        //for(int k = 0; k < cells.Count; k++)
        //{
        //    test += cells[k];

        //    if(k < indices.Count)
        //        test += " - " + indices[k] + " - ";
        //}

        //UnityEngine.Debug.Log(test);

        return new ChartedPath(cells.ToArray(), indices.ToArray());
    }

    #endregion

    #region Loop finder

    static void findNewCycles(int[] nodes, int[] edges = null, int recursionCount = 0, int lastGraphIndex = -1, string str = null)
    {
        if (recursionCount >= maxRecursionDepth)
            return;

        int n = nodes[0];
        int x;
        int[] sub = new int[nodes.Length + 1];

        if(edges == null)
            edges = new int[nodes.Length];

        int[] edgeSub = new int[edges.Length + 1];

        if (str == null)
            str = $"New search: ";

        for (int i = 0; i < LabelledGraphConnections.Length; i++)
        {
            for (int y = 0; y <= 1; y++)
                if (LabelledGraphConnections[i][y] == n)
                //  edge refers to our current node
                {
                    x = LabelledGraphConnections[i][(y + 1) % 2];

                    int graphIndex = LabelledGraphConnections[i][2];

                    if (graphIndex == lastGraphIndex)
                        continue;
                    
                    if (!IsVisited(x, nodes))
                    //  neighbor node not on path yet
                    {
                        sub[0] = x;
                        Array.Copy(nodes, 0, sub, 1, nodes.Length);

                        str += $"({indexedJunctions[n].gameObject.name} - {graphIndex} - {indexedJunctions[x].gameObject.name}) - ";

                        edges[0] = graphIndex;
                        Array.Copy(edges, 0, edgeSub, 1, edges.Length);

                        // explore extended path
                        // increase recursion counter for capping max depth;
                        recursionCount++;
                        findNewCycles(sub, edgeSub, recursionCount, graphIndex, str);
                    }
                    else if ((nodes.Length > 2) && (x == nodes[nodes.Length - 1]) && (graphIndex != edges[edges.Length - 1]))
                    //  cycle found
                    {
                        edges[0] = graphIndex;

                        var nodesAndEdges = normalize(nodes, edges);
                        int[] p = nodesAndEdges.nodes;
                        int[] e = nodesAndEdges.edges;

                        var inverted = invert(p, e);
                        int[] inv = inverted.nodes;
                        if (isNew(p) && isNew(inv))
                        {
                            cycles.Add((p, e));
                        }
                    }
                }
        }
    }

    static bool equals(int[] a, int[] b)
    {
        bool ret = (a[0] == b[0]) && (a.Length == b.Length);

        for (int i = 1; ret && (i < a.Length); i++)
            if (a[i] != b[i])
            {
                ret = false;
            }

        return ret;
    }

    static (int[] nodes, int[] edges) invert(int[] path, int[] edges)
    {
        int[] p = new int[path.Length];
        int[] e = new int[edges.Length];

        for (int i = 0; i < path.Length; i++)
        {
            p[i] = path[path.Length - 1 - i];
            e[i] = edges[edges.Length - 1 - i];
        }

        //for (int i = 0; i < edges.Length - 1; i++)
        //    e[i] = edges[edges.Length - 2 - i];


        return normalize(p, e);
    }

    //  rotate cycle path such that it begins with the smallest node
    static (int[] nodes, int[] edges) normalize(int[] nodes, int[] edges)
    {
        int[] p = new int[nodes.Length];
        int x = smallest(nodes);
        int n;

        int[] e = new int[edges.Length];
        int g;

        Array.Copy(nodes, 0, p, 0, nodes.Length);
        Array.Copy(edges, 0, e, 0, edges.Length);

        while (p[0] != x)
        {
            n = p[0];
            Array.Copy(p, 1, p, 0, p.Length - 1);
            p[p.Length - 1] = n;

            g = e[0];
            Array.Copy(e, 1, e, 0, e.Length - 1);
            e[e.Length - 1] = g;
        }

        return (p, e);
    }

    static bool isNew(int[] path)
    {
        bool ret = true;

        foreach (var cycle in cycles)
        {
            if (equals(cycle.nodes, path))
            {
                ret = false;
                break;
            }

        }

        return ret;
    }

    static int smallest(int[] path)
    {
        int min = path[0];

        foreach (int p in path)
            if (p < min)
                min = p;

        return min;
    }

    static bool IsVisited(int n, int[] path)
    {
        bool ret = false;

        foreach (int p in path)
            if (p == n)
            {
                ret = true;
                break;
            }

        return ret;
    }

    #endregion

    #region Local data types

    public struct EdgeData
    {
        public int from;
        public int to;
        public int graphIndex;

        public EdgeData(int from, int to, int graphIndex)
        {
            this.from = from;
            this.to = to;
            this.graphIndex = graphIndex;
        }

        public int this[int index]
        {
            get
            {
                if (index == 0)
                    return from;
                else if (index == 1)
                    return to;
                else if (index == 2)
                    return graphIndex;
                else
                    return -1;
            }
            set
            {
                if (index == 0)
                    from = value;
                else if (index == 1)
                    to = value;
                else if (index == 2)
                    graphIndex = value;
            }
        }
    }

    #endregion

    #region Editor

#if UNITY_EDITOR

    void TestAreas()
    {
        string test;

        //foreach (var area in Areas)
        //{
        //    test = $"Area: {area.Key} ";
        //    test += $" weight: {area.Value.weight} ";

        //    if(area.Value.placement.Count > 0)
        //        test += $" score: {area.Value.placementScore} weighted score: {area.Value.weightedScore}";
        //    if (area.Value.cameras != null)
        //        test += $" camera count: {area.Value.cameras.Count}";

        //    UnityEngine.Debug.Log(test);
        //}

        //foreach (var area in IsolatedAreas)
        //{
        //    test = "Isolated Area: ";

        //    foreach (var index in area.Key)
        //        test += index + " - ";

        //    test += "weight: " + area.Value.weight + " weighted score: " + area.Value.weightedScore;

        //    foreach(var point in area.Value.entryPoints)
        //        test += " entry: " + point.gameObject.name;

        //    UnityEngine.Debug.Log(test);
        //}

        var areaList = GetAreasSorted(SortType.Weight);
        test = "Sorted by weight: ";
        foreach(var item in areaList)
        {
            test += item.Key + $" ({item.Value.weight}) -";
        }
        UnityEngine.Debug.Log(test);

        areaList = GetAreasSorted(SortType.Score);
        test = "Sorted by score: ";
        foreach (var item in areaList)
        {
            test += item.Key + $" ({item.Value.placementScore}) -";
        }
        UnityEngine.Debug.Log(test);

        areaList = GetAreasSorted(SortType.WeightedScore);
        test = "Sorted by weighted score: ";
        foreach (var item in areaList)
        {
            test += item.Key + $" ({item.Value.weightedScore}) -";
        }
        UnityEngine.Debug.Log(test);

        var isolatedList = GetIsolatedAreasSorted(SortType.Weight);
        test = "Isolated sorted by weight: ";
        foreach (var item in isolatedList)
        {
            foreach (var index in item.Key)
                test += index + "-";

            test += $" ({item.Value.weight}) - ";
        }
        UnityEngine.Debug.Log(test);

        isolatedList = GetIsolatedAreasSorted(SortType.Score);
        test = "Isolated sorted by score: ";
        foreach (var item in isolatedList)
        {
            foreach (var index in item.Key)
                test += index + "-";

            test += $" ({item.Value.placementScore}) - ";
        }
        UnityEngine.Debug.Log(test);

        isolatedList = GetIsolatedAreasSorted(SortType.WeightedScore);
        test = "Isolated sorted by weighted score: ";
        foreach (var item in isolatedList)
        {
            foreach (var index in item.Key)
                test += index + "-";

            test += $" ({item.Value.weightedScore}) - ";
        }
        UnityEngine.Debug.Log(test);
    }

    public static void PrintCycle(int[] nodes, int[] edges)
    {
        string strIndices = "Cycle: ";

        //str += indexedJunctions[cycle[0]].gameObject.name + ", ";

        for (int j = nodes.Length - 1; j >= 0; j--)
            strIndices += indexedJunctions[nodes[j]].gameObject.name + " - " + edges[j] + " - ";
        

        UnityEngine.Debug.Log($"{strIndices}");
    }

    //string fromX, fromY, toX, toY, from, to;

    private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 80, 60), "Test Search"))
            TestAreas();

        //if (GUI.Button(new Rect(10, 70, 80, 60), "Vantage"))
        //    CalculateAllVantageScores();

        //fromX = GUI.TextField(new Rect(90, 70, 20, 60), fromX);
        //fromY = GUI.TextField(new Rect(110, 70, 20, 60), fromY);
        //toX = GUI.TextField(new Rect(130, 70, 20, 60), toX);
        //toY = GUI.TextField(new Rect(150, 70, 20, 60), toY);

        //if (GUI.Button(new Rect(10, 70, 80, 60), "Check Path"))
        //{
        //    int a, b, c, d;
        //    int.TryParse(fromX, out a);
        //    int.TryParse(fromY, out b);
        //    int.TryParse(toX, out c);
        //    int.TryParse(toY, out d);

        //    var start = maze.cells[a, b];
        //    var end = maze.cells[c, d];

        //    if (BiDirSearch(start, end, out ChartedPath found))
        //        found.DebugPath();
        //}

        //from = GUI.TextField(new Rect(90, 130, 20, 60), from);
        //to = GUI.TextField(new Rect(110, 130, 20, 60), to);

        //if (GUI.Button(new Rect(10, 130, 80, 60), "Index Path"))
        //{
        //    int a, b;
        //    int.TryParse(from, out a);
        //    int.TryParse(to, out b);

        //    if (BiDirSearch(a, b, out ChartedPath found))
        //        found.DebugPath();
        //}
    }

#endif

    #endregion
}
