using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spotfinder : MonoBehaviour
{
    public Maze maze;
    public PhysicsSim simulation;
    public Pathfinder pathfinder;
    public AreaFinder areafinder;

    [SerializeField] GameObject placeHolder;
    [SerializeField] int placeCount = 1;
    List<MazeCell> availableSpots = new List<MazeCell>();

    float spotHeight = 3f;

    void FindAvailableSpots()
    {
        for (int i = 0; i < maze.size.x; i++)
        {
            for (int j = 0; j < maze.size.y; j++)
            {
                if ((i == pathfinder.startPos.x && j == pathfinder.startPos.y) || (i == pathfinder.endPos.x && j == pathfinder.endPos.y))
                    continue;

                if (maze.cells[i, j].connectedCells.Count <= 1)
                {
                    maze.cells[i, j].isPlaceable = true;
                    if (!availableSpots.Contains(maze.cells[i, j]))
                        availableSpots.Add(maze.cells[i, j]);
                    continue;
                }

                int connectedCount = 0;

                foreach (var cell in maze.cells[i, j].connectedCells)
                {
                    if(cell.pos.y > maze.cells[i, j].pos.y)
                    {
                        if(i + 1 < maze.size.x
                           && cell.connectedCells.Contains(maze.cells[cell.pos.x + 1, cell.pos.y])
                           && maze.cells[cell.pos.x + 1, cell.pos.y].connectedCells.Contains(maze.cells[i + 1, j])
                           && maze.cells[i + 1, j].connectedCells.Contains(maze.cells[i,j]))
                        {
                            connectedCount++;
                        }
                    }
                    else if (cell.pos.x > maze.cells[i, j].pos.x)
                    {
                        if (j - 1 >= 0
                            && cell.connectedCells.Contains(maze.cells[cell.pos.x, cell.pos.y - 1])
                            && maze.cells[cell.pos.x, cell.pos.y - 1].connectedCells.Contains(maze.cells[i, j - 1])
                            && maze.cells[i, j - 1].connectedCells.Contains(maze.cells[i, j]))
                        {
                            connectedCount++;
                        }
                    }
                    else if (cell.pos.y < maze.cells[i, j].pos.y)
                    {
                        if (i - 1 >= 0
                            && cell.connectedCells.Contains(maze.cells[cell.pos.x - 1, cell.pos.y])
                            && maze.cells[cell.pos.x - 1, cell.pos.y].connectedCells.Contains(maze.cells[i - 1, j])
                            && maze.cells[i - 1, j].connectedCells.Contains(maze.cells[i, j]))
                        {
                            connectedCount++;
                        }
                    }
                    else if (cell.pos.x < maze.cells[i, j].pos.x)
                    {
                        if (j + 1 < maze.size.y
                            && cell.connectedCells.Contains(maze.cells[cell.pos.x, cell.pos.y + 1])
                            && maze.cells[cell.pos.x, cell.pos.y + 1].connectedCells.Contains(maze.cells[i, j + 1])
                            && maze.cells[i, j + 1].connectedCells.Contains(maze.cells[i, j]))
                        {
                            connectedCount++;
                        }
                    }
                }

                if (connectedCount >= maze.cells[i, j].connectedCells.Count - 1)
                {
                    maze.cells[i, j].isPlaceable = true;
                    if (!availableSpots.Contains(maze.cells[i, j]))
                        availableSpots.Add(maze.cells[i, j]);
                }
                else
                {
                    maze.cells[i, j].isPlaceable = false;
                    if (availableSpots.Contains(maze.cells[i, j]))
                        availableSpots.Remove(maze.cells[i, j]);
                }
            }
        }

        for (int i = 0; i < maze.size.x; i++)
        {
            for (int j = 0; j < maze.size.y; j++)
            {
                maze.cells[i, j].placeableNeighbourCount = 0;

                foreach (var cell in maze.cells[i, j].connectedCells)
                {
                    if (cell.isPlaceable)
                        maze.cells[i, j].placeableNeighbourCount++;
                }
            }
        }
    }

    void PlaceRandom()
    {
        var count = placeCount;

        while(availableSpots.Count > 0 && count > 0)
        {
            int random = Random.Range(0, availableSpots.Count);

            if(pathfinder.TryNeighbourPaths(availableSpots[random]))
            {
                availableSpots[random].state = 2;

                var go = Instantiate(placeHolder);
                Vector3 scale = new Vector3(maze.cellScaleX, maze.cellScaleY, spotHeight);
                go.transform.localScale = scale;
                go.transform.position = new Vector3(availableSpots[random].transform.position.x, availableSpots[random].transform.position.y,
                                                    availableSpots[random].transform.position.z - spotHeight / 2);

                availableSpots[random].isWalkable = false;

                HashSet<MazeCell> temp = new HashSet<MazeCell>();

                foreach (var cell in availableSpots[random].connectedCells)
                {
                    if (!cell.isWalkable)
                    {
                        cell.connectedCells.Remove(availableSpots[random]);
                        cell.placedConnectedCells.Add(availableSpots[random]);
                        temp.Add(cell);                        
                    }
                }

                foreach(var t in temp)
                {
                    availableSpots[random].connectedCells.Remove(t);

                    if(!t.isWalkable)
                        availableSpots[random].placedConnectedCells.Add(t);
                }

                count--;
            }

            availableSpots[random].isPlaceable = false;
            availableSpots.Remove(availableSpots[random]);

            areafinder.NewDetermineAreas();
        }
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 250, 80, 60), "Find Spots"))
            FindAvailableSpots();
        if (GUI.Button(new Rect(10, 310, 80, 60), "Gen Random"))
            PlaceRandom();
    }

    private void OnDrawGizmos()
    {
        if (maze != null && maze.cells.Length != 0)
        {
            foreach (var cell in availableSpots)
            {
                if (cell.isPlaceable)
                    Gizmos.DrawCube(cell.transform.position, new Vector3(5,5,5));
            }
        }
    }
}
