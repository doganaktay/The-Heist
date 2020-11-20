using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spotfinder : MonoBehaviour
{
    public Maze maze;
    public PhysicsSim simulation;
    public Pathfinder pathfinder;
    public AreaFinder areafinder;
    public GameObject layout;

    [SerializeField] int placeCount = 1;
    List<MazeCell> availableSpots = new List<MazeCell>();
    List<MazeCell> placedSpots = new List<MazeCell>();

    public List<Tile> activeTileSet = new List<Tile>();

    float spotHeight = 3f; // used to scale height (in z axis) for placed tiles

    public void DeterminePlacement()
    {
        FindAvailableSpots();
        PlaceRandom();
        DetermineTilePlacement();
    }

    void FindAvailableSpots()
    {
        availableSpots.Clear();

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

        placedSpots.Clear();

        while(availableSpots.Count > 0 && count > 0)
        {
            int random = Random.Range(0, availableSpots.Count);

            if(pathfinder.TryNeighbourPaths(availableSpots[random]))
            {
                availableSpots[random].state = 2;

                placedSpots.Add(availableSpots[random]);

                HashSet<MazeCell> temp = new HashSet<MazeCell>();

                foreach (var cell in availableSpots[random].connectedCells)
                {
                    if (cell.state > 1)
                    {
                        cell.connectedCells.Remove(availableSpots[random]);
                        cell.placedConnectedCells.Add(availableSpots[random]);
                        temp.Add(cell);                        
                    }
                }

                foreach(var t in temp)
                {
                    availableSpots[random].connectedCells.Remove(t);
                    availableSpots[random].placedConnectedCells.Add(t);
                }

                count--;
            }

            availableSpots[random].isPlaceable = false;
            availableSpots.Remove(availableSpots[random]);
        }
    }

    private void DetermineTilePlacement()
    {
        foreach(var spot in placedSpots)
        {
            DetermineNeighbourBits(spot);
            Tile tileToPlace = SetTile(spot);

            //Debug.Log("Cell:" + spot.gameObject.name + " cardinal bits: " + spot.cardinalBits + " diagonal bits: " + spot.diagonalBits);

            if (tileToPlace != null)
            {
                var go = Instantiate(tileToPlace.tile);
                go.transform.position = new Vector3(spot.transform.position.x, spot.transform.position.y,
                                                    spot.transform.position.z - spotHeight / 2);
                go.transform.rotation = Quaternion.Euler(go.transform.rotation.x, go.transform.rotation.y, go.transform.rotation.z + tileToPlace.selectedRule.rotation);
                Vector3 scale = tileToPlace.selectedRule.rotation % 180f == 0 ?
                                new Vector3(maze.cellScaleX, maze.cellScaleY, 1f) : new Vector3(maze.cellScaleY, maze.cellScaleX, 1f);
                go.transform.localScale = scale;
                go.transform.parent = layout.transform;

                maze.placementInScene.Add(go); // adding placement to maze to then be copied over to physics sim
            }
        }
    }

    private Tile SetTile(MazeCell spot)
    {
        List<Tile> candidates = new List<Tile>();

        foreach(var potentialTile in activeTileSet)
        {
            foreach(var rule in potentialTile.rules)
            {
                if (rule.cardinal == spot.cardinalBits)
                {
                    candidates.Add(potentialTile);
                    potentialTile.selectedRule = rule;
                    break;
                }
            }
        }

        if(candidates.Count == 1)
        {
            return candidates[0];
        }
        else
        {
            foreach(var candidate in candidates)
            {
                int test = candidate.selectedRule.diagonal & spot.diagonalBits;

                if (test != 0 && candidate.selectedRule.diagonal != 0)
                    return candidate;
            }

            foreach(var candidate in candidates)
            {
                if (candidate.selectedRule.diagonal == 0)
                    return candidate;
            }
        }

        Debug.Log("No candidate found for: " + spot.gameObject.name + " with candidate count of: " + candidates.Count);
        return null;
    }

    private void DetermineNeighbourBits(MazeCell cell)
    {
        cell.cardinalBits = 0;
        cell.diagonalBits = 0;

        for(int i = 0; i < MazeDirections.vectors.Length; i++)
        {
            var xpos = cell.pos.x + MazeDirections.vectors[i].x;
            var ypos = cell.pos.y + MazeDirections.vectors[i].y;

            if (xpos < 0 || ypos < 0 || xpos > maze.size.x - 1 || ypos > maze.size.y - 1)
                continue;

            var neighbour = maze.cells[xpos, ypos];

            if(i == 0 && (cell.connectedCells.Contains(neighbour) || cell.placedConnectedCells.Contains(neighbour)))
            {
                if (cell.connectedCells.Contains(neighbour))
                    cell.cardinalBits |= 1 << 0;

                var xdiagonal = cell.pos.x + MazeDirections.diagonalVectors[i].x;
                var ydiagonal = cell.pos.y + MazeDirections.diagonalVectors[i].y;

                if (xdiagonal < 0 || ydiagonal < 0 || xdiagonal > maze.size.x - 1 || ydiagonal > maze.size.y - 1)
                    continue;

                var diagonal = maze.cells[xdiagonal, ydiagonal];

                if (diagonal.state > 1)
                    continue;

                var xnext = cell.pos.x + MazeDirections.vectors[(i + 1) % MazeDirections.vectors.Length].x;
                var ynext = cell.pos.y + MazeDirections.vectors[(i + 1) % MazeDirections.vectors.Length].y;
                var next = maze.cells[xnext, ynext];

                if (neighbour.connectedCells.Contains(diagonal) && diagonal.connectedCells.Contains(next)
                    && (next.connectedCells.Contains(cell) || next.placedConnectedCells.Contains(cell)))
                    cell.diagonalBits |= 1 << 0;
            }
            else if (i == 1 && (cell.connectedCells.Contains(neighbour) || cell.placedConnectedCells.Contains(neighbour)))
            {
                if(cell.connectedCells.Contains(neighbour))
                    cell.cardinalBits |= 1 << 1;

                var xdiagonal = cell.pos.x + MazeDirections.diagonalVectors[i].x;
                var ydiagonal = cell.pos.y + MazeDirections.diagonalVectors[i].y;

                if (xdiagonal < 0 || ydiagonal < 0 || xdiagonal > maze.size.x - 1 || ydiagonal > maze.size.y - 1)
                    continue;

                var diagonal = maze.cells[xdiagonal, ydiagonal];

                if (diagonal.state > 1)
                    continue;

                var xnext = cell.pos.x + MazeDirections.vectors[(i + 1) % MazeDirections.vectors.Length].x;
                var ynext = cell.pos.y + MazeDirections.vectors[(i + 1) % MazeDirections.vectors.Length].y;
                var next = maze.cells[xnext, ynext];

                if (neighbour.connectedCells.Contains(diagonal) && diagonal.connectedCells.Contains(next)
                    && (next.connectedCells.Contains(cell) || next.placedConnectedCells.Contains(cell)))
                    cell.diagonalBits |= 1 << 1;
            }
            else if (i == 2 && (cell.connectedCells.Contains(neighbour) || cell.placedConnectedCells.Contains(neighbour)))
            {
                if (cell.connectedCells.Contains(neighbour))
                    cell.cardinalBits |= 1 << 2;

                var xdiagonal = cell.pos.x + MazeDirections.diagonalVectors[i].x;
                var ydiagonal = cell.pos.y + MazeDirections.diagonalVectors[i].y;

                if (xdiagonal < 0 || ydiagonal < 0 || xdiagonal > maze.size.x - 1 || ydiagonal > maze.size.y - 1)
                    continue;

                var diagonal = maze.cells[xdiagonal, ydiagonal];

                if (diagonal.state > 1)
                    continue;

                var xnext = cell.pos.x + MazeDirections.vectors[(i + 1) % MazeDirections.vectors.Length].x;
                var ynext = cell.pos.y + MazeDirections.vectors[(i + 1) % MazeDirections.vectors.Length].y;
                var next = maze.cells[xnext, ynext];

                if (neighbour.connectedCells.Contains(diagonal) && diagonal.connectedCells.Contains(next)
                    && (next.connectedCells.Contains(cell) || next.placedConnectedCells.Contains(cell)))
                    cell.diagonalBits |= 1 << 2;
            }
            else if (i == 3 && (cell.connectedCells.Contains(neighbour) || cell.placedConnectedCells.Contains(neighbour)))
            {
                if (cell.connectedCells.Contains(neighbour))
                    cell.cardinalBits |= 1 << 3;

                var xdiagonal = cell.pos.x + MazeDirections.diagonalVectors[i].x;
                var ydiagonal = cell.pos.y + MazeDirections.diagonalVectors[i].y;

                if (xdiagonal < 0 || ydiagonal < 0 || xdiagonal > maze.size.x - 1 || ydiagonal > maze.size.y - 1)
                    continue;

                var diagonal = maze.cells[xdiagonal, ydiagonal];

                if (diagonal.state > 1)
                    continue;

                var xnext = cell.pos.x + MazeDirections.vectors[(i + 1) % MazeDirections.vectors.Length].x;
                var ynext = cell.pos.y + MazeDirections.vectors[(i + 1) % MazeDirections.vectors.Length].y;
                var next = maze.cells[xnext, ynext];

                if (neighbour.connectedCells.Contains(diagonal) && diagonal.connectedCells.Contains(next)
                    && (next.connectedCells.Contains(cell) || next.placedConnectedCells.Contains(cell)))
                    cell.diagonalBits |= 1 << 3;
            }
        }
    }

    //private void OnGUI()
    //{
    //    if (GUI.Button(new Rect(10, 250, 80, 60), "Determine Tiles"))
    //        DetermineTilePlacement();
    //}

    private void OnDrawGizmos()
    {
        if (maze != null && maze.cells.Length != 0)
        {
            //foreach (var cell in availableSpots)
            //{
            //    if (cell.isPlaceable)
            //        Gizmos.DrawCube(cell.transform.position, new Vector3(5,5,5));
            //}

            foreach (var cell in placedSpots)
            {
               Gizmos.DrawCube(cell.transform.position, new Vector3(5, 5, 5));
            }
        }
    }
}
