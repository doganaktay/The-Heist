using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Diagnostics;

public class GraphFinder : MonoBehaviour
{
    public Maze maze;

    int index;
    Queue<MazeCell> frontier = new Queue<MazeCell>();
    List<MazeCell> currentArea = new List<MazeCell>();
    List<MazeCell> ends = new List<MazeCell>();
    bool[,] visited;
    static Dictionary<int, (List<MazeCell> all, List<MazeCell> ends)> GraphAreas;
    static Dictionary<int, MazeCell> indexedJunctions = new Dictionary<int, MazeCell>();
    public static List<int[]> cycles = new List<int[]>();
    static EdgeData[] LabelledGraphEdges;

    [SerializeField]
    int loopSearchLimit = 50, recursionLimit = 20;
    static int maxLoopSearchDepth;
    static int maxRecursionDepth;

    [SerializeField]
    bool showDebugDisplay = false;

    #region Graph Search

    private void Awake()
    {
        maxLoopSearchDepth = loopSearchLimit;
        maxRecursionDepth = recursionLimit;

        GameManager.MazeGenFinished += CreateGraph;
    }

    private void OnDisable()
    {
        GameManager.MazeGenFinished -= CreateGraph;
    }

    public void CreateGraph()
    {
        var timer = new Stopwatch();
        timer.Start();

        GraphAreas = new Dictionary<int, (List<MazeCell> all, List<MazeCell> ends)>();
        visited = new bool[maze.size.x, maze.size.y];
        frontier.Enqueue(GameManager.StartCell);

        index = 0;

        // PASS 1
        //UnityEngine.Debug.Log("Pass 1:");

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
            
            GraphAreas.Add(index, (new List<MazeCell>(currentArea), new List<MazeCell>(ends)));

            index++;
        }

        // PASS 2

        //UnityEngine.Debug.Log($"Pass 2: {GraphAreas.Count} partitions");

        List<MazeCell> cellsToTest = new List<MazeCell>();

        foreach (var part in GraphAreas)
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
                {
                    //if (cell.GraphAreaIndices.Contains(coveredIndex))
                    if (cell.GraphAreaIndices.Contains(coveredIndex))
                        i++;
                }

                if (i == 1 || (i == 2 && GraphAreas[coveredIndex].all.Count >= 3))
                {
                    temp.Add(coveredIndex);
                }
            }

            foreach(var t in temp)
            {
                indicesToMerge.Remove(t);
            }

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
                {
                    foreach(var indexToMerge in indicesToMerge)
                    {
                        cell.RemoveGraphArea(indexToMerge);

                        //if (cell.GraphAreaIndices.Contains(indexToMerge))
                        //    cell.GraphAreaIndices.Remove(indexToMerge);
                    }
                }
                else
                {
                    cell.GraphAreaIndices.Clear();

                    //cell.GraphAreaIndices.Clear();
                }

                cell.SetGraphArea(index);

                //cell.GraphAreaIndices.Add(index);
            }

            foreach(var indexToMerge in indicesToMerge)
            {
                if (GraphAreas.ContainsKey(indexToMerge))
                    GraphAreas.Remove(indexToMerge);
            }

            if (!GraphAreas.ContainsKey(index))
            {
                GraphAreas.Add(index, (new List<MazeCell>(currentArea), new List<MazeCell>(ends)));
            }

            index++;
        }

        //partitionKeys = "Pass 2 Keys: ";

        //foreach (var key in GraphAreas.Keys)
        //    partitionKeys += key.ToString() + ", ";

        //UnityEngine.Debug.Log(partitionKeys);

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

                if (junctionCount == 1 && GraphAreas[index].all.Count == 2)
                {
                    var smallestAreaIndex = GetSmallestAreaIndex(junctions[0], index);
                    MergeAreas(index, smallestAreaIndex, true);
                }
                else if (junctionCount == 2 && GraphAreas[index].all.Count == 3)
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

        foreach(var area in GraphAreas)
        {
            foreach(var cell in area.Value.all)
            {
                cell.SetDistanceToJunctions(area.Value.ends);
            }

            if(area.Value.ends.Count == 1)
            {
                var end = area.Value.ends[0];
                end.DeadConnectionCount++;

                if (!pruningList.Contains(end))
                    pruningList.Add(end);
            }
            else
            {
                foreach (var end in area.Value.ends)
                {
                    if (end.IsDeadEnd && !pruningList.Contains(end))
                    {
                        pruningList.Add(end);
                    }
                }
            }

            foreach (var end in area.Value.ends)
            {
                if (end.IsLockedConnection && !currentEnds.Contains(end))
                {
                    currentEnds.Add(end);
                    end.EndIndex = endCounter;

                    endCounter++;
                }
            }
        }

        PruneTree(pruningList);

        var unloopables = new List<MazeCell>();
        foreach(var end in currentEnds)
        {
            if (end.IsUnloopable)
                unloopables.Add(end);
        }

        foreach (var t in unloopables)
        {
            currentEnds.Remove(t);
            //UnityEngine.Debug.Log($"Removing {t.gameObject.name} from loop junctions");
        }

        indexedJunctions.Clear();

        foreach(var end in currentEnds)
        {
            var junctionIndex = end.EndIndex;

            if (!indexedJunctions.ContainsKey(junctionIndex))
                indexedJunctions.Add(junctionIndex, end);

            var labelledConnections = GetLabelledConnections(end, false);

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

        // Construct edge graph for cycle detection
        cycles.Clear();

        int edgeCount = 0;
        LabelledGraphEdges = new EdgeData[labelledEdges.Count];
        edgeCount = 0;
        foreach(var edge in labelledEdges)
        {
            LabelledGraphEdges[edgeCount].from = edge.from;
            LabelledGraphEdges[edgeCount].to = edge.to;
            LabelledGraphEdges[edgeCount].graphIndex = edge.graphIndex;

            edgeCount++;
        }

        List<int> usedIndices = new List<int>();

        for(int i = 0; i < LabelledGraphEdges.Length; i++)
        {
            for(int j = 0; j <= 1; j++)
            {
                var label = LabelledGraphEdges[i][j];

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

        cycles = cycles.OrderBy(x => x.Length).ToList();

        timer.Stop();
        UnityEngine.Debug.Log($"Graph and Loop finding took: {timer.ElapsedMilliseconds}ms");

        // DISPLAY

        if (showDebugDisplay)
        {
            Color junctionColor = new Color(1f, 0f, 0.1f);

            foreach(var area in GraphAreas)
            {
                var randomColor = new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));

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

        //UnityEngine.Debug.Log($"Searching {currentCell.gameObject.name}");

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

        var currentConnectedCount = currentCell.connectedCells.Count;

        foreach (var cell in currentCell.connectedCells)
        {
            //if (currentConnectedCount == 3 && cell.connectedCells.Count == 3)
            //{
            //    var bitPatternToCheck = currentCell.cardinalBits.RotatePattern(4, 2);

            //    if ((bitPatternToCheck ^ cell.cardinalBits) == 0)
            //        continue;
            //}

            SearchCellForMerge(cell, coveredIndices);
        }

    }

    void MergeAreas(int from, int to, bool dissolve = false)
    {
        var junctionCells = GetJunctionCells(from, to);

        var endsToMerge = new List<MazeCell>(GraphAreas[from].ends);
        var cellsToMerge = new List<MazeCell>(GraphAreas[from].all);
        var junctionsToDissolve = new List<MazeCell>();

        foreach(var cell in junctionCells)
            if (cell.GraphAreaIndices.Count <= 2)
                junctionsToDissolve.Add(cell);

        if (dissolve)
            endsToMerge.Clear();

        AddToGraphArea(to, cellsToMerge, endsToMerge);

        foreach (var junction in junctionsToDissolve)
            GraphAreas[to].ends.Remove(junction);

        foreach(var cell in GraphAreas[from].all.ToList())
        {
            cell.GraphAreaIndices.Remove(from);

            if(!cell.GraphAreaIndices.Contains(to))
                cell.GraphAreaIndices.Add(to);
        }

        GraphAreas.Remove(from);
    }

    bool HasMadeConnection(MazeCell from, MazeCell to)
    {
        foreach(var key in from.GraphAreaIndices)
        {
            if (GraphAreas[key].ends.Contains(to))
                return true;
        }

        return false;
    }

    void PruneTree(List<MazeCell> ends)
    {
        foreach (var end in ends)
            CheckJunction(end);
    }

    void CheckJunction(MazeCell cell)
    {
        //UnityEngine.Debug.Log($"Checking from {cell.gameObject.name} for pruning");

        foreach (var connection in GetConnections(cell))
        {
            connection.DeadConnectionCount++;
            //UnityEngine.Debug.Log($"{connection.gameObject.name} dead connection count {connection.DeadConnectionCount}");

            if (!connection.IsDeadEnd && connection.DeadConnectionCount == GetConnections(connection).Count - 1)
            {
                //UnityEngine.Debug.Log($"{connection.gameObject.name} is unloopable");
                connection.IsUnloopable = true;
                CheckJunction(connection);
            }
        }
    }

    #endregion

    #region Getters

    public int GetJunctionCellCount(int from, int to)
    {
        return GetJunctionCells(from, to).Count;
    }

    public List<MazeCell> GetJunctionCells(int from, int to)
    {
        var junctionCells = new List<MazeCell>();

        foreach(var endFrom in GraphAreas[from].ends)
        {
            if(GraphAreas[to].ends.Contains(endFrom) && !junctionCells.Contains(endFrom))
                junctionCells.Add(endFrom);
        }

        return junctionCells;
    }

    public int GetJunctionCellCount(int index)
    {
        return GetJunctionCells(index).Count;
    }

    public List<MazeCell> GetJunctionCells(int index)
    {
        var junctionCells = new List<MazeCell>();

        foreach (var end in GraphAreas[index].ends)
            if (end.GraphAreaIndices.Count > 1)
                junctionCells.Add(end);

        return junctionCells;
    }

    public int GetConnectedIndexCount(int index)
    {
        return GetConnectedIndices(index).Count;
    }

    public List<int> GetConnectedIndices(int index)
    {
        List<int> indices = new List<int>();

        foreach(var cell in GraphAreas[index].ends)
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
        if (!GraphAreas.ContainsKey(index))
        {
            UnityEngine.Debug.Log($"{gameObject.name} does not have a graph key for {index}");
            return;
        }

        var cellsToAdd = new List<MazeCell>();
        foreach (var cell in area)
        {
            if (!GraphAreas[index].all.Contains(cell))
                cellsToAdd.Add(cell);
        }

        GraphAreas[index].all.AddRange(cellsToAdd);


        if (ends != null)
        {
            var endsToAdd = new List<MazeCell>();

            foreach (var cell in ends)
            {
                if (!GraphAreas[index].ends.Contains(cell))
                    endsToAdd.Add(cell);
            }

            GraphAreas[index].ends.AddRange(endsToAdd);
        }
    }

    public int GetSmallestAreaIndex(MazeCell cell, int indexToIgnore = -1)
    {
        int lowestIndex = -1;

        // exaggerating value for min check
        int areaCount = 1000;

        foreach (var key in cell.GraphAreaIndices)
        {
            var part = GraphAreas[key];

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
            var part = GraphAreas[key];

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

    public int GetRandomAreaIndex(MazeCell cell)
    {
        var indices = cell.GetGraphAreaIndices();
        return indices[UnityEngine.Random.Range(0, indices.Count)];
    }

    public List<MazeCell> GetConnections(MazeCell cell, bool includeSelf = false)
    {
        if (!cell.IsGraphConnection)
            return new List<MazeCell>(GraphAreas[cell.GetGraphAreaIndices()[0]].ends);
        else
        {
            var connections = new List<MazeCell>();

            foreach (var key in cell.GraphAreaIndices)
            {
                foreach(var item in GraphAreas[key].ends)
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

        foreach(var key in cell.GraphAreaIndices)
        {
            foreach(var item in GraphAreas[key].ends)
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

            return new List<MazeCell>(GraphAreas[existingIndex].ends);
        }
        else
        {
            var connections = new List<MazeCell>();

            foreach (var item in GraphAreas[index].ends)
            {
                if (!includeSelf && item == cell)
                    continue;

                connections.Add(item);
            }

            return connections;
        }
    }

    public int GetOtherConnectionCount(MazeCell cell, int index) => GetOtherConnections(cell, index).Count;

    public List<MazeCell> GetOtherConnections(MazeCell cell, int index)
    {
        if (!cell.IsGraphConnection)
        {
            UnityEngine.Debug.LogError($"{gameObject.name} is not a connection point. Returning ends of area");

            return new List<MazeCell>(GraphAreas[cell.GetGraphAreaIndices()[0]].ends);
        }
        else
        {
            var connections = new List<MazeCell>();

            foreach (var key in cell.GraphAreaIndices)
            {
                if (index == key)
                    continue;

                foreach (var item in GraphAreas[key].ends)
                {
                    if (item == cell)
                        continue;

                    connections.Add(item);
                }
            }

            return connections;
        }
    }

    public int GetAreaCellCount(MazeCell cell, int index = -1) => GetAreaCells(cell, index).Count;

    public List<MazeCell> GetAreaCells(MazeCell cell, int index = -1)
    {
        if (index == -1)
        {
            if (cell.IsJunction)
            {
                UnityEngine.Debug.LogError($"Junction {gameObject.name} received an area cell request without an index, returning null");
                return null;
            }

            return new List<MazeCell>(GraphAreas[cell.GetGraphAreaIndices()[0]].all);
        }
        else
        {
            if (!cell.GraphAreaIndices.Contains(index))
            {
                UnityEngine.Debug.Log($"{gameObject.name} received area cell request for {index} but does not contain the key, returning null");
                return null;
            }

            return new List<MazeCell>(GraphAreas[index].all);
        }

    }

    public MazeCell[] GetLoopWaypoints(int[] indices)
    {
        var wps = new MazeCell[indices.Length];

        for (int i = 0; i < indices.Length; i++)
            wps[i] = indexedJunctions[indices[i]];

        return wps;
    }

    bool IsDeadEnd(int index)
    {
        bool deadEnd = false;

        foreach (var end in GraphAreas[index].ends)
            if (end.IsDeadEnd)
            {
                deadEnd = true;
                break;
            }

        if (deadEnd)
            UnityEngine.Debug.Log($"{index} is dead end");

        return deadEnd;
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

    #endregion

    #region Loop finder

    static void findNewCycles(int[] path, int recursionCount = 0, int lastGraphIndex = -1, int firstGraphIndex = -1, string str = null)
    {
        if (recursionCount >= maxRecursionDepth)
            return;

        int n = path[0];
        int x;
        int[] sub = new int[path.Length + 1];

        //if (lastGraphIndex == -1)
        //    UnityEngine.Debug.Log($"Starting new path search from {indexedJunctions[n].gameObject.name}");
        if (str == null)
            str = $"New search: ";

        for (int i = 0; i < LabelledGraphEdges.Length; i++)
        {
            for (int y = 0; y <= 1; y++)
                if (LabelledGraphEdges[i][y] == n)
                //  edge referes to our current node
                {
                    x = LabelledGraphEdges[i][(y + 1) % 2];

                    int graphIndex = LabelledGraphEdges[i][2];

                    if (firstGraphIndex < 0)
                        firstGraphIndex = graphIndex;

                    if (graphIndex == lastGraphIndex)
                    {
                        //UnityEngine.Debug.Log($"Repeat graph index {graphIndex}, aborting search going from {indexedJunctions[n].gameObject.name} to {indexedJunctions[x].gameObject.name}");
                        continue;
                    }
                    //else
                    //    UnityEngine.Debug.Log($"Connecting {lastGraphIndex} to {graphIndex} cells: {indexedJunctions[n].gameObject.name} to {indexedJunctions[x].gameObject.name}");

                    if (!IsVisited(x, path))
                    //  neighbor node not on path yet
                    {
                        sub[0] = x;
                        Array.Copy(path, 0, sub, 1, path.Length);

                        str += $"({indexedJunctions[n].gameObject.name} - {graphIndex} - {indexedJunctions[x].gameObject.name}) - ";

                        // explore extended path
                        // increase recursion counter for capping max depth;
                        recursionCount++;
                        findNewCycles(sub, recursionCount, graphIndex, firstGraphIndex, str);
                    }
                    else if ((path.Length > 2) && (x == path[path.Length - 1]) && (graphIndex != firstGraphIndex))
                    //  cycle found
                    {
                        int[] p = normalize(path);
                        int[] inv = invert(p);
                        if (isNew(p) && isNew(inv))
                        {
                            cycles.Add(p);

                            UnityEngine.Debug.Log(str);
                            PrintCycle(p);
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

    static int[] invert(int[] path)
    {
        int[] p = new int[path.Length];

        for (int i = 0; i < path.Length; i++)
            p[i] = path[path.Length - 1 - i];

        return normalize(p);
    }

    //  rotate cycle path such that it begins with the smallest node
    static int[] normalize(int[] path)
    {
        int[] p = new int[path.Length];
        int x = smallest(path);
        int n;

        Array.Copy(path, 0, p, 0, path.Length);

        while (p[0] != x)
        {
            n = p[0];
            Array.Copy(p, 1, p, 0, p.Length - 1);
            p[p.Length - 1] = n;
        }

        return p;
    }

    static bool isNew(int[] path)
    {
        bool ret = true;

        foreach (int[] p in cycles)
            if (equals(p, path))
            {
                ret = false;
                break;
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

    void TestAreas()
    {
        //foreach(var area in GraphAreas)
        //{
        //    UnityEngine.Debug.Log($"GraphFinder index {area.Key}");

        //    foreach(var cell in area.Value.ends)
        //        UnityEngine.Debug.Log($"ends contains: {cell.gameObject.name}");

        //    foreach (var cell in area.Value.all)
        //        UnityEngine.Debug.Log($"area contains: {cell.gameObject.name}");
        //}

        //for (int i = 0; i < maze.size.x; i++)
        //{
        //    for (int j = 0; j < maze.size.y; j++)
        //    {
        //        var cell = maze.cells[i, j];

        //        if (cell.state > 1)
        //            continue;

        //        UnityEngine.Debug.Log($"Junction Distances for {cell.gameObject.name}");

        //        foreach(var item in cell.JunctionDistances)
        //        {
        //            foreach(var end in item.Value)
        //            UnityEngine.Debug.Log($"distance to {end.gameObject.name}: {item.Key}");
        //        }
        //    }
        //}

        //for(int j = 0; j < GraphEdges.GetLength(0); j++)
        //{
        //    string str = "Edge: ";
        //    for(int k = 0; k <= 1; k++)
        //    {
        //        str += GraphEdges[j, k] + (k == 0 ? ", " : "");
        //    }

        //    UnityEngine.Debug.Log($"{str}");
        //}

        for (int j = 0; j < LabelledGraphEdges.Length; j++)
        {
            string str = "Edge: ";
            for (int k = 0; k <= 1; k++)
            {
                str += LabelledGraphEdges[j][k] + " ";
                str += indexedJunctions[LabelledGraphEdges[j][k]].gameObject.name + ", ";

            }

            str += " graph index: " + LabelledGraphEdges[j][2];

            UnityEngine.Debug.Log($"{str}");
        }

        foreach (var cycle in cycles)
        {
            PrintCycle(cycle);
        }
    }

    static void PrintCycle(int[] cycle)
    {
        string str = "";
        string strIndices = "Cycle: ";

        //str += indexedJunctions[cycle[0]].gameObject.name + ", ";

        for (int j = cycle.Length - 1; j >= 0; j--)
        {
            strIndices += cycle[j] + "-";
            str += indexedJunctions[cycle[j]].gameObject.name + ", ";
        }


        strIndices += str;

        UnityEngine.Debug.Log($"{strIndices}");
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 250, 80, 60), "Test Search"))
            TestAreas();
    }
}
