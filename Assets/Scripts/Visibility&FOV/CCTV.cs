using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CCTV : MonoBehaviour
{
    [SerializeField]
    CCTVCamera camPrefab;
    [SerializeField][Tooltip("Camera used to test spawn points. Mesh renderer turned off")]
    CCTVCamera testCam;

    public Maze maze;

    public void CheckCamCoverage()
    {
        List<(float coverage, IntVector2 coords)> ranked = new List<(float, IntVector2)>();

        for (int i = 0; i < maze.size.x; i++)
        {
            for (int j = 0; j < maze.size.y; j++)
            {
                if (maze.cells[i, j].connectedCells.Count + maze.cells[i, j].placedConnectedCells.Count > 3 || maze.cells[i, j].specialConnectedCells.Count != 0)
                    continue;

                testCam.transform.position = maze.cells[i, j].transform.position;

                testCam.InitCam(50f, 360f);

                ranked.Add((testCam.GetCoverageSize(), new IntVector2(i, j)));
            }
        }

        ranked = ranked.OrderByDescending(x => x.coverage).ToList();

        var bestPos = maze.cells[ranked[0].coords.x, ranked[0].coords.y];
        testCam.transform.position = bestPos.transform.position;

        foreach (var pos in ranked)
        {
            Debug.Log($"Cam at Cell {pos.coords.x},{pos.coords.y} has a coverage of {pos.coverage}");
        }
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 250, 80, 60), "CheckCamCoverage"))
            CheckCamCoverage();
    }


}
