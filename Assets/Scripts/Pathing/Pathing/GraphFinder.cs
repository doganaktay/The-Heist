using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GraphFinder : MonoBehaviour
{
    public Maze maze;

    int index;
    Queue<MazeCell> frontier = new Queue<MazeCell>();
    List<MazeCell> currentArea = new List<MazeCell>();
    List<MazeCell> ends = new List<MazeCell>();
    bool[,] visited;
    static Dictionary<int, (List<MazeCell> all, List<MazeCell> ends)> GraphAreas;

    [SerializeField]
    bool showDebugDisplay = false;

    #region Graph Search

    private void Awake()
    {
        GameManager.MazeGenFinished += CreateGraph;
    }

    private void OnDisable()
    {
        GameManager.MazeGenFinished -= CreateGraph;
    }

    public void CreateGraph()
    {
        GraphAreas = new Dictionary<int, (List<MazeCell> all, List<MazeCell> ends)>();
        visited = new bool[maze.size.x, maze.size.y];
        frontier.Enqueue(GameManager.StartCell);

        index = 0;

        // PASS 1
        //Debug.Log("Pass 1:");

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

        //Debug.Log($"Pass 2: {GraphAreas.Count} partitions");

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

        //Debug.Log(partitionKeys);

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

        //Debug.Log($"Searching {currentCell.gameObject.name}");

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

        //foreach (var key in currentCell.graphAreas.Keys)
        //    coveredIndices.Add(key);

        foreach (var key in currentCell.GraphAreaIndices)
            coveredIndices.Add(key);

        var currentConnectedCount = currentCell.connectedCells.Count;

        foreach (var cell in currentCell.connectedCells)
        {
            //if(currentConnectedCount == 3 && cell.connectedCells.Count == 3)
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
            //Debug.Log($"Searching {cell.gameObject.name} for indices connected except {index}");

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
            Debug.Log($"{gameObject.name} does not have a graph key for {index}");
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

    //////////////////////


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
            Debug.Log($"Smallest area index not found for {gameObject.name}");
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
            Debug.Log($"Largest area index not found for {gameObject.name}");
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

    public List<MazeCell> GetConnections(MazeCell cell, int index, bool includeSelf = false)
    {
        if (!cell.IsGraphConnection)
        {
            var existingIndex = cell.GetGraphAreaIndices()[0];

            if (index != existingIndex)
                Debug.Log($"{gameObject.name} does not belong to {index} returning only available index at {existingIndex}");

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
            Debug.LogError($"{gameObject.name} is not a connection point. Returning ends of area");

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
                Debug.LogError($"Junction {gameObject.name} received an area cell request without an index, returning null");
                return null;
            }

            return new List<MazeCell>(GraphAreas[cell.GetGraphAreaIndices()[0]].all);
        }
        else
        {
            if (!cell.GraphAreaIndices.Contains(index))
            {
                Debug.Log($"{gameObject.name} received area cell request for {index} but does not contain the key, returning null");
                return null;
            }

            return new List<MazeCell>(GraphAreas[index].all);
        }

    }



    #endregion

    void TestAreas()
    {
        foreach(var area in GraphAreas)
        {
            Debug.Log($"GraphFinder index {area.Key}");

            foreach(var cell in area.Value.ends)
                Debug.Log($"ends contains: {cell.gameObject.name}");

            foreach (var cell in area.Value.all)
                Debug.Log($"area contains: {cell.gameObject.name}");
        }

        //for(int i = 0; i < maze.size.x; i++)
        //{
        //    for(int j = 0; j < maze.size.y; j++)
        //    {
        //        if (maze.cells[i, j].state > 1)
        //            continue;

        //        Debug.Log($"{maze.cells[i,j]} connected indic")
        //    }
        //}
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 250, 80, 60), "Test Search"))
            TestAreas();
    }
}
