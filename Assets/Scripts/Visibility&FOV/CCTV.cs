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
    [HideInInspector]
    public PhysicsSim simulation;

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

    private void OnEnable()
    {
        GameManager.MazeGenFinished += SetupCCTV;
    }

    private void OnDisable()
    {
        GameManager.MazeGenFinished -= SetupCCTV;
    }

    private void SetupCCTV()
    {
        camDisplacement = GameManager.CellDiagonal / 2f * camDisplacementPercent;

        camViewRadius = GetLimits(defaultViewRadius);
        camViewAngle = GetLimits(defaultViewAngle);
        camRotSpeed = GetLimits(defaultRotSpeed);
        camWaitTime = GetLimits(defaultWaitTime);
        camRotChance = GetLimits(defaultRotChance);

        StartCoroutine(PlaceCams());
    }

    IEnumerator PlaceCams()
    {
        yield return null;
        PlaceSecurityCameras();
    }

    private MinMaxData GetLimits(float original)
    {
        var min = original - (original * GameManager.ParameterDeviation * GameManager.BiasMultipliers.min);
        var max = original + (original * GameManager.ParameterDeviation * GameManager.BiasMultipliers.max);
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
        public int coveredCellCount;

        public CamSpotData(IntVector2 coords, Vector3 position, Vector3 direction, float coverage, float maxViewAngle, int coveredCellCount)
        {
            this.coords = coords;
            this.position = position;
            this.direction = direction;
            this.coverage = coverage;
            this.maxViewAngle = maxViewAngle;
            this.coveredCellCount = coveredCellCount;
        }
    }

    public void PlaceSecurityCameras()
    {
        cameras.Clear();

        testCam.gameObject.SetActive(true);

        var sortedSpots = GetSortedCameraSpots();

        for (int i = 0, j = 0; j < camCount && i < sortedSpots.Count; i++)
        {
            var current = sortedSpots[i].position;
            bool isPlaceable = true;

            bool canSeeStart = Vector2.Distance(current, GameManager.StartCell.transform.position) <= camViewRadius.max
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

                if (lineOfSight || (distance <= defaultViewRadius && shareView))
                {
                    isPlaceable = false;
                    break;
                }
            }

            if (isPlaceable)
            {
                var cam = Instantiate(camPrefab, current, Quaternion.identity);
                cam.gameObject.name = $"CCTV Cam ({sortedSpots[i].coords.x},{sortedSpots[i].coords.y})";
                cam.transform.parent = transform;
                float zAngle = Mathf.Atan2(-sortedSpots[i].direction.y, -sortedSpots[i].direction.x) * Mathf.Rad2Deg - 90f;
                float viewAngle = GetViewAngle();
                float viewRadius = GetViewRadius();

                bool isStatic = Random.value > camRotChance.max || sortedSpots[i].maxViewAngle < viewAngle;
                float rotMin = (sortedSpots[i].maxViewAngle / 2f) - (viewAngle / 2f);
                float rotMax = (sortedSpots[i].maxViewAngle / 2f) - (viewAngle / 2f);
                MinMaxData rotLimits = new MinMaxData(rotMin, rotMax);

                if (isStatic)
                {
                    cam.InitCam(viewRadius, viewAngle, zAngle, rotLimits);
                }
                else
                {

                    float rotSpeed = GetRotSpeed();
                    float waitTime = GetWaitTime();

                    cam.InitCam(viewRadius, viewAngle, zAngle, rotLimits, rotSpeed, waitTime);
                }

                cameras.Add(cam);

                j++;
            }
        }

        foreach(var cam in cameras)
        {
            simulation.AddToSim(cam as ISimulateable);
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

                if (current.connectedCells.Count + current.placedConnectedCells.Count > 3 || current.specialConnectedCells.Count > 0)
                    continue;

                var possiblePositions = GetPossibleCamPositions(current);

                float topCoverage = 0;
                Vector2 position = Vector2.zero;
                Vector2 direction = Vector2.zero;
                float viewAngle = 0f;
                int coveredCellCount = 0;

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
                        coveredCellCount = testCam.GetCellsInView().Count;
                    }
                }

                var data = new CamSpotData(new IntVector2(i, j), position, direction, topCoverage, viewAngle, coveredCellCount);
                sorted.Add(data);
            }
        }

        return sorted.OrderByDescending(x => x.coveredCellCount).ToList();
    }

    private List<(Vector3 position, Vector3 direction, float maxViewAngle)> GetPossibleCamPositions(MazeCell cell)
    {
        List<Vector3> displacementDirs = new List<Vector3>();
        List<float> maxViewAngles = new List<float>();

        var diagonalMask = 1 << 1 | 1 << 3 | 1 << 5 | 1 << 7;

        foreach(var set in CamPosVectors)
        {
            bool testSame = ((cell.allNeighbourBits ^ set.mask) & ~diagonalMask) == 0;

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
        }

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
}
