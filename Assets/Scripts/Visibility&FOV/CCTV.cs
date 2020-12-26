using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CCTV : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    CCTVCamera camPrefab;
    [SerializeField][Tooltip("Camera used to test spawn points. Mesh renderer turned off")]
    CCTVCamera testCam;
    public Maze maze;
    private List<CCTVCamera> cameras = new List<CCTVCamera>();

    [Header("Masks")]
    [SerializeField]
    LayerMask targetMask;
    [SerializeField]
    LayerMask obstacleMask;

    [Header("Settings")]
    [SerializeField]
    private int camCount = 0;
    public int CamCount { get; set; }
    [SerializeField]
    float defaultViewRadius = 50f;
    MinMaxData camViewRadius;
    [SerializeField, Range(0f, 1f)]
    float camDisplacementPercent = .5f;
    float camDisplacement;
    [SerializeField]
    float defaultViewAngle = 90f;
    MinMaxData camViewAngle;
    [SerializeField]
    float defaultRotSpeed = 10f;
    MinMaxData camRotSpeed;
    [SerializeField]
    float defaultWaitTime = 2f;
    MinMaxData camWaitTime;
    [SerializeField, Range(0f, 1f)]
    float defaultRotChance = 0.1f;
    MinMaxData camRotChance;
    [SerializeField, Range(0f,1f), Tooltip("Percent to deviate from default value on affected parameters")]
    float paramDeviation = 0.1f;
    [SerializeField, Tooltip("The deviation is biased in the min and max by these multipliers")]
    MinMaxData biasMultipliers;

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

        if(biasMultipliers.min  == 0 || biasMultipliers.max == 0)
        {
            Debug.LogError("Bias multiplier is zero. Assigning default values");

            biasMultipliers.min = 1;
            biasMultipliers.max = 1;
        }

        camViewRadius = GetLimits(defaultViewRadius);
        camViewAngle = GetLimits(defaultViewAngle);
        camRotSpeed = GetLimits(defaultRotSpeed);
        camWaitTime = GetLimits(defaultWaitTime);
        camRotChance = GetLimits(defaultRotChance);
    }

    private MinMaxData GetLimits(float original)
    {
        var min = original - (original * paramDeviation * biasMultipliers.min);
        var max = original + (original * paramDeviation * biasMultipliers.max);
        return new MinMaxData(min, max);
    }

    private float GetViewRadius() => Random.Range(camViewRadius.min, camViewRadius.max);
    private float GetViewAngle() => Random.Range(camViewAngle.min, camViewAngle.max);
    private float GetRotSpeed() => Random.Range(camRotSpeed.min, camRotSpeed.max);
    private float GetWaitTime() => Random.Range(camWaitTime.min, camWaitTime.max);

    public struct CamSpotData
    {
        public IntVector2 coords;
        public Vector3 position;
        public Vector3 direction;
        public float coverage;
        public float maxViewAngle;

        public CamSpotData(IntVector2 coords, Vector3 position, Vector3 direction, float coverage, float maxViewAngle)
        {
            this.coords = coords;
            this.position = position;
            this.direction = direction;
            this.coverage = coverage;
            this.maxViewAngle = maxViewAngle;
        }
    }

    public void PlaceSecurityCameras()
    {
        cameras.Clear();

        testCam.gameObject.SetActive(true);

        var sortedSpots = GetSortedCameraSpots();

        int j = 0;
        for (int i = 0; j < camCount && i < sortedSpots.Count; i++)
        {
            var current = sortedSpots[i].position;
            bool isPlaceable = true;

            bool canSeeStart = Vector2.Distance(current, GameManager.StartCell.transform.position) <= defaultViewRadius
                               && !Physics2D.Linecast(current, GameManager.StartCell.transform.position, obstacleMask);

            if (canSeeStart)
                continue;

            foreach (var placed in cameras)
            {
                float distance = Vector2.Distance(current, placed.transform.position);
                bool inRange = distance <= defaultViewRadius * 2;

                if (!inRange)
                    continue;

                bool lineOfSight = !Physics2D.Linecast(current, placed.transform.position, obstacleMask);
                bool shareView = Vector2.Dot(-sortedSpots[i].direction, placed.Aim.up) > -Mathf.Epsilon;

                //Debug.Log($"Dot product of {-sortedSpots[i].direction} for position ({sortedSpots[i].coords.x},{sortedSpots[i].coords.y}) and {placed.Aim.up} of {placed.gameObject.name}:" +
                //    $" {Vector2.Dot(-sortedSpots[i].direction, placed.Aim.up)}, check result: {shareView}");

                if (lineOfSight || (distance <= defaultViewRadius && shareView))
                    isPlaceable = false;
            }

            if (isPlaceable)
            {
                var cam = Instantiate(camPrefab, current, Quaternion.identity);
                cam.gameObject.name = $"CCTV Cam ({sortedSpots[i].coords.x},{sortedSpots[i].coords.y})";
                cam.transform.parent = transform;
                float zAngle = Mathf.Atan2(-sortedSpots[i].direction.y, -sortedSpots[i].direction.x) * Mathf.Rad2Deg - 90f;
                float viewAngle = GetViewAngle();
                float viewRadius = GetViewRadius();

                //bool isStatic = Random.value > camRotChance.max || sortedSpots[i].maxViewAngle < viewAngle;
                bool isStatic = sortedSpots[i].maxViewAngle < viewAngle;
                Debug.Log($"{cam.gameObject.name} is Static: {isStatic} with {sortedSpots[i].maxViewAngle} less than {viewAngle}");

                if (isStatic)
                {
                    cam.InitCam(viewRadius, viewAngle, zAngle);
                }
                else
                {
                    float rotMin = (sortedSpots[i].maxViewAngle / 2f) - (viewAngle / 2f);
                    float rotMax = (sortedSpots[i].maxViewAngle / 2f) - (viewAngle / 2f);
                    MinMaxData rotLimits = new MinMaxData(rotMin, rotMax);
                    
                    float rotSpeed = GetRotSpeed();
                    float waitTime = GetWaitTime();

                    cam.InitCam(viewRadius, viewAngle, zAngle, rotLimits, rotSpeed, waitTime);
                }

                cameras.Add(cam);

                //Debug.Log($"Cam placed at Cell {sortedSpots[i].coords.x},{sortedSpots[i].coords.y} with direction {-sortedSpots[i].direction} has a coverage of {sortedSpots[i].coverage}");

                j++;
            }
        }

        testCam.gameObject.SetActive(false);
    }

    public List<CamSpotData> GetSortedCameraSpots()
    {
        List<CamSpotData> sorted = new List<CamSpotData>();

        for (int i = 0; i < maze.size.x; i++)
        {
            for (int j = 0; j < maze.size.y; j++)
            {
                var current = maze.cells[i, j];

                if (current.connectedCells.Count + current.placedConnectedCells.Count > 3 || current.specialConnectedCells.Count != 0)
                    continue;

                var possiblePositions = GetPossibleCamPositions(current);

                float topCoverage = 0;
                Vector2 position = Vector2.zero;
                Vector2 direction = Vector2.zero;
                float viewAngle = 0f;

                foreach (var pos in possiblePositions)
                {
                    testCam.transform.position = pos.position;
                    testCam.InitCam(defaultViewRadius, 360f);

                    var temp = testCam.GetCoverageSize();

                    if (temp > topCoverage)
                    {
                        topCoverage = temp;
                        position = pos.position;
                        direction = pos.direction;
                        viewAngle = pos.maxViewAngle;
                    }
                }

                var data = new CamSpotData(new IntVector2(i, j), position, direction, topCoverage, viewAngle);
                sorted.Add(data);
            }
        }

        return sorted.OrderByDescending(x => x.coverage).ToList();
    }

    private List<(Vector3 position, Vector3 direction, float maxViewAngle)> GetPossibleCamPositions(MazeCell cell)
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
        List<float> maxViewAngles = new List<float>();

        var diagonalMask = 1 << 1 | 1 << 3 | 1 << 5 | 1 << 7;

        foreach(var set in CamPosVectors)
        {
            bool testSame = ((cell.allNeighbourBits ^ set.mask) & ~diagonalMask) == 0;

            //Debug.Log($"{cell.gameObject.name} checking bitfield value {System.Convert.ToString(cell.allNeighbourBits, 2)} against mask {System.Convert.ToString(set.mask, 2)}" +
            //          $" with result {System.Convert.ToString((cell.allNeighbourBits ^ set.mask), 2)}. Test passes: {testSame} Adding direction: {set.direction}");

            if (testSame)
            {
                displacementDirs.Add(set.direction);
                maxViewAngles.Add(set.maxViewAngle);
            }
        }

        List<Vector3> positions = new List<Vector3>();

        foreach (var dir in displacementDirs)
        {
            positions.Add(cell.transform.position + dir * camDisplacement);
            //Debug.Log($"{cell.gameObject.name}: Position added with displacement {dir} * {camDisplacement} where cell diagonal is {GameManager.CellDiagonal}");
        }

        //Debug.Log($"{positions.Count} possible placement(s) for {cell.gameObject.name}");

        List<(Vector3 position, Vector3 direction, float maxViewAngle)> results = new List<(Vector3 position, Vector3 direction, float maxViewAngle)>();

        for(int i = 0; i < displacementDirs.Count; i++)
        {
            results.Add((positions[i], displacementDirs[i], maxViewAngles[i]));
        }

        return results;
    }

    static List<(int mask, Vector2 direction, float maxViewAngle)> CamPosVectors = new List<(int mask, Vector2 direction, float maxViewAngle)>
    {
        ( 1 << 0, new Vector2(0, -1), 0f),
        ( 1 << 2, new Vector2(-1, 0), 0f),
        ( 1 << 4, new Vector2(0, 1), 0f),
        ( 1 << 6, new Vector2(1, 0), 0f),
        ( 1 << 0 | 1 << 2, new Vector2(-1, -1), 90f),
        ( 1 << 2 | 1 << 4, new Vector2(-1, 1), 90f),
        ( 1 << 4 | 1 << 6, new Vector2(1, 1), 90f),
        ( 1 << 6 | 1 << 0, new Vector2(1, -1), 90f),
        ( 1 << 0 | 1 << 2 | 1 << 4, new Vector2(-1, 0), 180f),
        ( 1 << 2 | 1 << 4 | 1 << 6, new Vector2(0, 1), 180f),
        ( 1 << 4 | 1 << 6 | 1 << 0, new Vector2(1, 0), 180f),
        ( 1 << 6 | 1 << 0 | 1 << 2, new Vector2(0, -1), 180f),
        ( 1 << 0 | 1 << 4, new Vector2(1, 0), 180f),
        ( 1 << 0 | 1 << 4, new Vector2(-1, 0), 180f),
        ( 1 << 2 | 1 << 6, new Vector2(0, 1), 180f),
        ( 1 << 2 | 1 << 6, new Vector2(0, -1), 180f),
        ( 1 << 0 | 1 << 2 | 1 << 4 | 1 << 6, new Vector2(0,0), 360f)
    };

    private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 250, 80, 60), "Place Cameras"))
            PlaceSecurityCameras();
    }


}
