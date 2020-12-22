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

    private List<CCTVCamera> cameras = new List<CCTVCamera>();

    [SerializeField]
    LayerMask targetMask;
    [SerializeField]
    LayerMask obstacleMask;
    [SerializeField]
    float testRadius = 50f;
    [SerializeField, Range(0f, 1f)]
    float camDisplacementPercent = .5f;
    float camDisplacement;

    public Maze maze;
    [SerializeField]
    private int camCount = 0;
    public int CamCount { get; set; }

    private void OnEnable()
    {
        GameManager.MazeGenFinished += CalculateSystemParams;
    }

    private void OnDisable()
    {
        GameManager.MazeGenFinished -= CalculateSystemParams;
    }

    private void CalculateSystemParams()
    {
        camDisplacement = GameManager.CellDiagonal / 2f * camDisplacementPercent;
    }

    public struct CamSpotData
    {
        public IntVector2 coords;
        public Vector3 position;
        public Vector3 direction;
        public float coverage;

        public CamSpotData(IntVector2 coords, Vector3 position, Vector3 direction, float coverage)
        {
            this.coords = coords;
            this.position = position;
            this.direction = direction;
            this.coverage = coverage;
        }
    }

    public List<CamSpotData> GetSortedCameraSpots()
    {
        List<CamSpotData> ranked = new List<CamSpotData>();

        for (int i = 0; i < maze.size.x; i++)
        {
            for (int j = 0; j < maze.size.y; j++)
            {
                if (maze.cells[i, j].connectedCells.Count + maze.cells[i, j].placedConnectedCells.Count > 3 || maze.cells[i, j].specialConnectedCells.Count != 0)
                    continue;

                var possiblePairs = GetPossibleCamPositions(maze.cells[i, j]);

                float topCoverage = 0;
                Vector2 position = Vector2.zero;
                Vector2 direction = Vector2.zero;

                foreach(var pair in possiblePairs)
                {
                    testCam.transform.position = pair.position;
                    testCam.InitCam(testRadius, 360f);

                    var temp = testCam.GetCoverageSize();

                    if (temp > topCoverage)
                    {
                        topCoverage = temp;
                        position = pair.position;
                        direction = pair.direction;
                    }
                }

                var data = new CamSpotData(new IntVector2(i, j), position, direction, topCoverage);
                ranked.Add(data);
            }
        }

        return ranked.OrderByDescending(x => x.coverage).ToList();
    }

    public void PlaceSecurityCameras()
    {
        var sortedSpots = GetSortedCameraSpots();

        int j = 0;
        while (j < camCount && sortedSpots.Count > 0)
        {
            var current = sortedSpots[j].position;
            bool isPlaceable = true;

            foreach(var placed in cameras)
            {
                if (!Physics2D.Linecast(current, placed.transform.position, obstacleMask)
                    && Vector2.Distance(current, placed.transform.position) <= 100f)
                    isPlaceable = false;
            }

            if (isPlaceable)
            {
                var pos = current;
                var cam = Instantiate(camPrefab, pos, Quaternion.identity);
                cam.transform.parent = transform;
                cam.InitCam(50f, 360f);
                cameras.Add(cam);

                cam.Aim.up = -sortedSpots[j].direction;
                cam.gameObject.name = $"CCTV Cam ({sortedSpots[j].coords.x},{sortedSpots[j].coords.y})";

                Debug.Log($"Cam placed at Cell {sortedSpots[j].coords.x},{sortedSpots[j].coords.y} with direction {-sortedSpots[j].direction} has a coverage of {sortedSpots[j].coverage}");

                j++;
            }
            else
            {
                Debug.Log($"Cell {sortedSpots[j].coords.x},{sortedSpots[j].coords.y} was rejected as candidate");
            }

            sortedSpots.Remove(sortedSpots[j]);
        }

        testCam.gameObject.SetActive(false);
    }

    private List<(Vector3 position, Vector3 direction)> GetPossibleCamPositions(MazeCell cell)
    {
        // 0001 - 1011 => 0,-1
        // 0010 - 0111 => -1, 0
        // 0100 - 1110 => 0, 1
        // 1000 - 1101 => 1, 0
        // ----
        // 0011 => -1. -1
        // 0110 => -1, 1
        // 1100 => 1, 1
        // 1001 => 1, -1
        // ----
        // 0101 => -1, 0 | 1, 0
        // 1010 => 0, 1 | 0, -1

        List<Vector3> displacementDirs = new List<Vector3>();

        foreach(var set in CamPosVectors)
        {
            bool testSame = (cell.allNeighbourBits ^ set.mask) == 0;

            Debug.Log($"{cell.gameObject.name} checking bitfield value {System.Convert.ToString(cell.allNeighbourBits, 2)} against mask {System.Convert.ToString(set.mask, 2)}" +
                      $" with result {System.Convert.ToString((cell.allNeighbourBits ^ set.mask),2)}. Test passes: {testSame} Adding direction: {set.direction}");

            if (testSame)
                displacementDirs.Add(set.direction);
        }

        List<Vector3> positions = new List<Vector3>();

        foreach(var dir in displacementDirs)
        {
            positions.Add(cell.transform.position + dir * camDisplacement);
            Debug.Log($"Position added with displacement {dir} * {camDisplacement} where cell diagonal is {GameManager.CellDiagonal}");
        }

        Debug.Log($"{positions.Count} possible placement(s) for {cell.gameObject.name}");

        List<(Vector3 position, Vector3 direction)> results = new List<(Vector3 position, Vector3 direction)>();

        for(int i = 0; i < displacementDirs.Count; i++)
        {
            results.Add((positions[i], displacementDirs[i]));
        }

        return results;
    }

    static List<(int mask, Vector2 direction)> CamPosVectors = new List<(int mask, Vector2 direction)>
    {
        ( 1 << 0, new Vector2(0, -1)),
        ( 1 << 2, new Vector2(-1, 0)),
        ( 1 << 4, new Vector2(0, 1)),
        ( 1 << 6, new Vector2(1, 0)),
        ( 1 << 0 | 1 << 2, new Vector2(-1, -1)),
        ( 1 << 2 | 1 << 4, new Vector2(-1, 1)),
        ( 1 << 4 | 1 << 6, new Vector2(1, 1)),
        ( 1 << 6 | 1 << 0, new Vector2(1, -1)),
        ( 1 << 0 | 1 << 2 | 1 << 4, new Vector2(-1, 0)),
        ( 1 << 2 | 1 << 4 | 1 << 6, new Vector2(0, 1)),
        ( 1 << 4 | 1 << 6 | 1 << 0, new Vector2(1, 0)),
        ( 1 << 6 | 1 << 0 | 1 << 2, new Vector2(0, -1)),
        ( 1 << 0 | 1 << 4, new Vector2(1, 0)),
        ( 1 << 0 | 1 << 4, new Vector2(-1, 0)),
        ( 1 << 2 | 1 << 6, new Vector2(0, 1)),
        ( 1 << 2 | 1 << 6, new Vector2(0, -1)),
    };

    private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 250, 80, 60), "CheckCamCoverage"))
            PlaceSecurityCameras();
    }


}
