using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class AreaFinder : MonoBehaviour
{
    MazeCell[,] grid;
    public Maze maze;
    public PhysicsSim simulation;

    Dictionary<int, List<MazeCell>> lowCellAreas = new Dictionary<int, List<MazeCell>>();
    Dictionary<int, List<MazeCell>> highCellAreas = new Dictionary<int, List<MazeCell>>();

    Dictionary<int, List<MazeCell>> lowCellConnected = new Dictionary<int, List<MazeCell>>();
    Dictionary<int, List<MazeCell>> highCellConnected = new Dictionary<int, List<MazeCell>>();

    Dictionary<int, List<MazeCell>> placedAreas = new Dictionary<int, List<MazeCell>>();

    public List<MazeCell> GetLowCellArea (int areaIndex) { return lowCellAreas[areaIndex]; }
    public List<MazeCell> GetHighCellArea(int areaIndex) { return highCellAreas[areaIndex]; }

    public List<MazeCell> GetPatrolAreaByIndex(int areaIndex){ return lowCellAreas[areaIndex]; }
    public List<MazeCell> GetLowConnectionPoints(int areaIndex){ return lowCellConnected[areaIndex]; }
    public List<MazeCell> GetRandomArea(){ return lowCellAreas.ElementAt(UnityEngine.Random.Range(0, lowCellAreas.Count)).Value; }

    #if UNITY_EDITOR
    public bool displayCellID = false;
    #endif

    // being called by pathfinder when the maze changes
    public void FindAreas()
    {
        ResetGrid();
        NewDetermineAreas();
    }

    public void MakeRooms()
    {
        foreach (var room in lowCellAreas)
        {
            foreach (var cell in room.Value)
            {
                for (int i = 0; i < MazeDirections.vectors.Length; i++)
                {
                    var xpos = cell.pos.x + MazeDirections.vectors[i].x;
                    var ypos = cell.pos.y + MazeDirections.vectors[i].y;

                    if (xpos < 0 || ypos < 0 || xpos > maze.size.x - 1 || ypos > maze.size.y - 1)
                        continue;

                    var neighbour = maze.cells[xpos, ypos];

                    if (!cell.connectedCells.Contains(neighbour) && cell.state == neighbour.state
                        && cell.areaIndex == neighbour.areaIndex)
                    {
                        var wall = (MazeCellWall)cell.GetEdge((MazeDirection)i);
                        MazeDirections.RemoveWall(wall);
                        maze.wallsInScene.Remove(wall.GetComponentInParent<MazeCellWall>());
                        simulation.RemoveWallFromSimulation(wall.gameObject);

                        cell.connectedCells.Add(neighbour);
                    }
                }
            }
        }

        foreach (var room in highCellAreas)
        {
            foreach (var cell in room.Value)
            {
                for (int i = 0; i < MazeDirections.vectors.Length; i++)
                {
                    var xpos = cell.pos.x + MazeDirections.vectors[i].x;
                    var ypos = cell.pos.y + MazeDirections.vectors[i].y;

                    if (xpos < 0 || ypos < 0 || xpos > maze.size.x - 1 || ypos > maze.size.y - 1)
                        continue;

                    var neighbour = maze.cells[xpos, ypos];

                    if (!cell.connectedCells.Contains(neighbour) && cell.state == neighbour.state
                        && cell.areaIndex == neighbour.areaIndex && Mathf.Abs(neighbour.distanceFromStart[0] - cell.distanceFromStart[0]) < 4)
                    {
                        var wall = (MazeCellWall)cell.GetEdge((MazeDirection)i);
                        MazeDirections.RemoveWall(wall);
                        maze.wallsInScene.Remove(wall.GetComponentInParent<MazeCellWall>());
                        simulation.RemoveWallFromSimulation(wall.gameObject);

                        cell.connectedCells.Add(neighbour);
                    }
                }
            }
        }
    }

    public List<MazeCell> GetRandomAreaWeighted()
    {
        var totalCount = 0;
        int[] weights = new int[lowCellAreas.Count];

        for(int i = 0; i < lowCellAreas.Count; i++)
        {
            if(i == 0)
                weights[i] = lowCellAreas.ElementAt(i).Value.Count;
            else
                weights[i] = lowCellAreas.ElementAt(i).Value.Count + weights[i-1];

            totalCount = weights[i];
        }

        var rand = UnityEngine.Random.Range(0, totalCount);
        var destinationIndex = -1;

        for(int i = 0; i < lowCellAreas.Count; i++)
        {
            if (weights[i] >= rand && destinationIndex == -1 ? true : i - 1 < 0 ? true : rand > weights[i-1] ? true : false)
                destinationIndex = i;
        }

        return lowCellAreas.ElementAt(destinationIndex).Value;
    }

    // resets local copy of grid to current maze
    public void ResetGrid()
    {
        if(grid == null)
            grid = new MazeCell[maze.size.x, maze.size.y];

        for (int i = 0; i < maze.size.x; i++)
        {
            for (int j = 0; j < maze.size.y; j++)
            {
                grid[i, j] = maze.cells[i, j];
            }
        }
    }

    public void DetermineSinglePaths()
    {
        foreach(var lowCellArea in lowCellAreas)
        {
            bool isCorridor = true;
            foreach(var cell in lowCellArea.Value)
            {
                if (cell.connectedCells.Count > 2)
                {
                    isCorridor = false;
                    break;
                }
            }

            if(isCorridor)
                Debug.Log("Corridor at index: " + lowCellArea.Key);
        }
    }

    // GRID SEARCH IMPLEMENTING CCL

    public void NewDetermineAreas()
    {

        lowCellAreas.Clear();
        highCellAreas.Clear();
        lowCellConnected.Clear();
        highCellConnected.Clear();

        placedAreas.Clear();

        int[,] labels = new int[maze.size.x, maze.size.y];
        List<HashSet<int>> linkedLow = new List<HashSet<int>>();
        int nextLabelLow = 0;
        List<HashSet<int>> linkedHigh = new List<HashSet<int>>();
        int nextLabelHigh = 0;
        List<HashSet<int>> linkedPlaced = new List<HashSet<int>>();
        int nextLabelPlaced = 0;

        for (int j = 0; j < maze.size.x; j++)
        {
            for (int i = 0; i < maze.size.y; i++)
            {
                grid[j, i].searched = false;

                int k = i - 1;
                int h = j - 1;

                if (grid[j, i].state == 0)
                {
                    HashSet<int> neighbors = new HashSet<int>();

                    if (k >= 0 && grid[j, k].state == 0 && grid[j,k].connectedCells.Contains(grid[j,i]))
                        neighbors.Add(labels[j, k]);
                    if (h >= 0 && grid[h, i].state == 0 && grid[h, i].connectedCells.Contains(grid[j, i]))
                        neighbors.Add(labels[h, i]);

                    if (neighbors.Count == 0)
                    {
                        linkedLow.Add(new HashSet<int> { nextLabelLow });
                        labels[j, i] = nextLabelLow;
                        nextLabelLow++;
                    }
                    else
                    {
                        HashSet<int> neighborLabels = new HashSet<int>();
                        foreach (int n in neighbors)
                        {
                            foreach (int a in linkedLow[n])
                            { neighborLabels.Add(a); }
                        }

                        labels[j, i] = neighborLabels.Min();
                        foreach (int label in neighborLabels)
                        {
                            linkedLow[label].UnionWith(neighborLabels);
                        }
                    }

                }
                else if (grid[j, i].state == 1)
                {
                    HashSet<int> neighbors = new HashSet<int>();

                    if (k >= 0 && grid[j, k].state == 1 && grid[j, k].connectedCells.Contains(grid[j, i]))
                        neighbors.Add(labels[j, k]);
                    if (h >= 0 && grid[h, i].state == 1 && grid[h, i].connectedCells.Contains(grid[j, i]))
                        neighbors.Add(labels[h, i]);

                    if (neighbors.Count == 0)
                    {
                        linkedHigh.Add(new HashSet<int> { nextLabelHigh });
                        labels[j, i] = nextLabelHigh;
                        nextLabelHigh++;
                    }
                    else
                    {
                        HashSet<int> neighborLabels = new HashSet<int>();
                        foreach (int n in neighbors)
                        {
                            foreach (int a in linkedHigh[n])
                            { neighborLabels.Add(a); }
                        }

                        labels[j, i] = neighborLabels.Min();
                        foreach (int label in neighborLabels)
                        {
                            linkedHigh[label].UnionWith(neighborLabels);
                        }
                    }

                }
                else if (grid[j, i].state == 2) // uses placedConnectedCells set to search
                {
                    HashSet<int> neighbors = new HashSet<int>();

                    if (k >= 0 && grid[j, k].state == 2 && grid[j, k].placedConnectedCells.Contains(grid[j, i]))
                        neighbors.Add(labels[j, k]);
                    if (h >= 0 && grid[h, i].state == 2 && grid[h, i].placedConnectedCells.Contains(grid[j, i]))
                        neighbors.Add(labels[h, i]);

                    if (neighbors.Count == 0)
                    {
                        linkedPlaced.Add(new HashSet<int> { nextLabelPlaced });
                        labels[j, i] = nextLabelPlaced;
                        nextLabelPlaced++;
                    }
                    else
                    {
                        HashSet<int> neighborLabels = new HashSet<int>();
                        foreach (int n in neighbors)
                        {
                            foreach (int a in linkedPlaced[n])
                            { neighborLabels.Add(a); }
                        }

                        labels[j, i] = neighborLabels.Min();
                        foreach (int label in neighborLabels)
                        {
                            linkedPlaced[label].UnionWith(neighborLabels);
                        }
                    }

                }
            }
        }

        for (int j = 0; j < maze.size.x; j++)
        {
            for (int i = 0; i < maze.size.y; i++)
            {
                if (grid[j, i].state == 0)
                {
                    labels[j, i] = linkedLow[labels[j, i]].Min();
                    grid[j, i].areaIndex = labels[j, i];

                    #if UNITY_EDITOR
                    if (displayCellID)
                        grid[j, i].cellText.text = labels[j, i].ToString();
                    #endif

                    if (!lowCellAreas.ContainsKey(labels[j, i]))
                    {
                        lowCellAreas.Add(labels[j, i], new List<MazeCell>());
                        lowCellAreas[labels[j, i]].Add(grid[j, i]);
                    }
                    else
                        lowCellAreas[labels[j, i]].Add(grid[j, i]);
                }
                else if(grid[j, i].state == 1)
                {
                    labels[j, i] = linkedHigh[labels[j, i]].Min();
                    grid[j, i].areaIndex = labels[j, i];

                    #if UNITY_EDITOR
                    if (displayCellID)
                        grid[j, i].cellText.text = labels[j, i].ToString();
                    #endif

                    if (!highCellAreas.ContainsKey(labels[j, i]))
                    {
                        highCellAreas.Add(labels[j, i], new List<MazeCell>());
                        highCellAreas[labels[j, i]].Add(grid[j, i]);
                    }
                    else
                        highCellAreas[labels[j, i]].Add(grid[j, i]);
                }
                else if (grid[j, i].state == 2)
                {
                    labels[j, i] = linkedPlaced[labels[j, i]].Min();
                    grid[j, i].areaIndex = labels[j, i];

                    #if UNITY_EDITOR
                    if (displayCellID)
                        grid[j, i].cellText.text = labels[j, i].ToString();
                    #endif

                    if (!placedAreas.ContainsKey(labels[j, i]))
                    {
                        placedAreas.Add(labels[j, i], new List<MazeCell>());
                        placedAreas[labels[j, i]].Add(grid[j, i]);
                    }
                    else
                        placedAreas[labels[j, i]].Add(grid[j, i]);
                }
            }
        }

        SetPaths();
        DetermineConnections();
    }

    void DetermineConnections()
    {
        foreach (var lowCellArea in lowCellAreas)
        {
            lowCellConnected.Add(lowCellArea.Key, new List<MazeCell>());
        }

        foreach (var highCellArea in highCellAreas)
        {
            highCellConnected.Add(highCellArea.Key, new List<MazeCell>());
            SearchNeighbours(highCellArea.Value[0]);
        }
    }

    void SetPaths()
    {
        for (int j = 0; j < maze.size.x; j++)
        {
            for (int i = 0; i < maze.size.y; i++)
            {
                if (grid[j, i].state == 1)
                {
                    var d = grid[j, i].distanceFromStart[0];

                    #if UNITY_EDITOR
                    if (displayCellID)
                    {
                        grid[j, i].cellText.color = Color.white;
                        grid[j, i].cellText.text = d.ToString();
                    }
                    #endif
                }   
            }
        }   
    }

    // search is only performed on cells with state < 2
    // state > 1 means placement
    void SearchNeighbours(MazeCell point)
    {
        int k = point.row - 1;
        int h = point.col - 1;
        int l = point.row + 1;
        int m = point.col + 1;

        grid[point.row, point.col].searched = true;

        if (m < maze.size.y && !grid[point.row, m].searched && grid[point.row, point.col].connectedCells.Contains(grid[point.row, m]))
        {
            if (grid[point.row, point.col].state == grid[point.row, m].state)
                SearchNeighbours(grid[point.row, m]);
            else if (grid[point.row, m].state < 2)
            {
                highCellConnected[grid[point.row, point.col].areaIndex].Add(grid[point.row, m]);
                lowCellConnected[grid[point.row, m].areaIndex].Add(grid[point.row, point.col]);

                #if UNITY_EDITOR
                if (displayCellID)
                {
                    grid[point.row, m].cellText.color = Color.magenta;
                    grid[point.row, point.col].cellText.color = Color.yellow;
                }
                #endif
            }
        }

        if (l < maze.size.x && !grid[l, point.col].searched && grid[point.row, point.col].connectedCells.Contains(grid[l, point.col]))
        {
            if (grid[point.row, point.col].state == grid[l, point.col].state)
                SearchNeighbours(grid[l, point.col]);
            else if (grid[l, point.col].state < 2)
            {
                highCellConnected[grid[point.row, point.col].areaIndex].Add(grid[l, point.col]);
                lowCellConnected[grid[l, point.col].areaIndex].Add(grid[point.row, point.col]);

                #if UNITY_EDITOR
                if (displayCellID)
                {
                    grid[l, point.col].cellText.color = Color.magenta;
                    grid[point.row, point.col].cellText.color = Color.yellow;
                }
                #endif
            }
        }

        if (h >= 0 && !grid[point.row, h].searched && grid[point.row, point.col].connectedCells.Contains(grid[point.row, h]))
        {
            if (grid[point.row, point.col].state == grid[point.row, h].state)
                SearchNeighbours(grid[point.row, h]);
            else if (grid[point.row, h].state < 2)
            {
                highCellConnected[grid[point.row, point.col].areaIndex].Add(grid[point.row, h]);
                lowCellConnected[grid[point.row, h].areaIndex].Add(grid[point.row, point.col]);

                #if UNITY_EDITOR
                if (displayCellID)
                {
                    grid[point.row, h].cellText.color = Color.magenta;
                    grid[point.row, point.col].cellText.color = Color.yellow;
                }
                #endif
            }
        }
        if (k >= 0 && !grid[k, point.col].searched && grid[point.row, point.col].connectedCells.Contains(grid[k, point.col]))
        {

            if (grid[point.row, point.col].state == grid[k, point.col].state)
                SearchNeighbours(grid[k, point.col]);
            else if (grid[k, point.col].state < 2)
            {
                highCellConnected[grid[point.row, point.col].areaIndex].Add(grid[k, point.col]);
                lowCellConnected[grid[k, point.col].areaIndex].Add(grid[point.row, point.col]);

                #if UNITY_EDITOR
                if (displayCellID)
                {
                    grid[k, point.col].cellText.color = Color.magenta;
                    grid[point.row, point.col].cellText.color = Color.yellow;
                }
                #endif
            }
        }
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 130, 80, 60), "Make Rooms"))
            MakeRooms();
        if (GUI.Button(new Rect(10, 190, 80, 60), "Find Corridors"))
            DetermineSinglePaths();
    }
}
