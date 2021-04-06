using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;

[RequireComponent(typeof(FieldOfView))]
public class CCTVCamera : MonoBehaviour, ISimulateable, IProjectileTarget
{
    FieldOfView fov;
    Transform aim;
    float rotationAngle;
    float rotationSpeed;
    MinMaxData rotationLimits;
    float waitTime;
    Coroutine rotateCamRoutine;
    [SerializeField][Tooltip("Used to iterate the sweep search for possible placement angle")]
    int placementAngleResolution = 1;

    public bool IsStatic { get => fov.IsStatic; set => fov.IsStatic = value; }
    public bool ShowFOV { get => fov.canDraw; set => fov.canDraw = value; }
    public Transform Aim => aim;

    CancellationToken lifetimeToken;

    // interface members
    public GameObject Instance { get => gameObject; }
    public int SyncTransformIndex { get; } = 0;
    public bool IsDynamic { get; set; } = true;
    public bool IsDestructible { get; set; } = true;

    public void TakeHit()
    {
        fov.ClearMesh();
        fov.enabled = false;

        if(rotateCamRoutine != null)
            StopCoroutine(rotateCamRoutine);
    }

    void Awake()
    {
        fov = GetComponent<FieldOfView>();
        aim = fov.aim = transform.GetChild(0);

        lifetimeToken = this.GetCancellationTokenOnDestroy();
    }

    public void SetCamViewDistance(float distance) => fov.viewRadius = distance;
    public void SetCamViewAngle(float angle) => fov.viewAngle = angle;
    public void SetCamRotAngle(float angle) => rotationAngle = angle;
    public void SetCamRotSpeed(float speed) => rotationSpeed = speed;
    public void SetCamWaitTime(float time) => waitTime = time;
    public void SetCamRotLimits(MinMaxData limits) => rotationLimits = limits;

    public void InitCam(float viewDistance, float viewAngle)
    {
        SetCamViewDistance(viewDistance);
        SetCamViewAngle(viewAngle);
    }

    public void InitCam(float viewDistance, float viewAngle, float lookDirAngle, MinMaxData rotLimits)
    {
        Vector3 rotEuler = aim.rotation.eulerAngles;
        rotEuler.z = lookDirAngle;
        aim.rotation = Quaternion.Euler(rotEuler);

        SetCamViewDistance(viewDistance);
        SetCamViewAngle(viewAngle);
        SetCamRotLimits(new MinMaxData(aim.rotation.eulerAngles.z - rotLimits.min, aim.rotation.eulerAngles.z + rotLimits.max));

        //StartCoroutine(SetFinalAngle());
        SetFinalAngle(lifetimeToken).Forget();
    }

    IEnumerator SetFinalAngle()
    {
        yield return null;

        Vector3 rotEuler = aim.rotation.eulerAngles;
        rotEuler.z = GetTopDirectionAngles()[0].angle;
        aim.rotation = Quaternion.Euler(rotEuler);

        //Debug.Log($"{gameObject.name} rot min: {rotationLimits.min} max: {rotationLimits.max} current: {aim.eulerAngles.z}" +
        //    $" Setting angle to {rotEuler.z} with top coverage of {GetTopDirectionAngles()[0].coverage} cells");

        IsStatic = true;
        fov.DrawFieldOfView();
    }

    async UniTaskVoid SetFinalAngle(CancellationToken token)
    {
        await UniTask.NextFrame();

        Vector3 rotEuler = aim.rotation.eulerAngles;
        rotEuler.z = GetTopDirectionAngles()[0].angle;
        aim.rotation = Quaternion.Euler(rotEuler);

        //Debug.Log($"{gameObject.name} rot min: {rotationLimits.min} max: {rotationLimits.max} current: {aim.eulerAngles.z}" +
        //    $" Setting angle to {rotEuler.z} with top coverage of {GetTopDirectionAngles()[0].coverage} cells");

        IsStatic = true;
        fov.DrawFieldOfView();
    }

    public void InitCam(float viewDistance, float viewAngle, float lookDirAngle, MinMaxData rotLimits, float rotSpeed, float waitTime)
    {
        Vector3 rotEuler = aim.rotation.eulerAngles;
        rotEuler.z = lookDirAngle;
        aim.rotation = Quaternion.Euler(rotEuler);

        SetCamViewDistance(viewDistance);
        SetCamViewAngle(viewAngle);
        SetCamRotSpeed(rotSpeed);
        SetCamRotLimits(new MinMaxData(aim.rotation.eulerAngles.z - rotLimits.min, aim.rotation.eulerAngles.z + rotLimits.max));
        SetCamWaitTime(waitTime);

        //rotateCamRoutine = StartCoroutine(RotateCam());

        Rotate(lifetimeToken).Forget();
    }

    IEnumerator RotateCam()
    {
        var limit = Random.value < 0.5f ? rotationLimits.min : rotationLimits.max;
        var delay = new WaitForSeconds(waitTime);

        yield return new WaitForSeconds(Random.Range(0f, waitTime));

        while (true)
        {
            var current = aim.rotation;
            var end = Quaternion.Euler(0, 0, limit);

            var t = 0f;
            while(aim.rotation != Quaternion.Euler(0, 0, limit) && t < 1.01f)
            {
                t += rotationSpeed * Time.deltaTime;
                aim.rotation = Quaternion.Slerp(current, end, t);

                yield return null;
            }

            if (limit == rotationLimits.min)
                limit = rotationLimits.max;
            else
                limit = rotationLimits.min;

            yield return delay;
        }
    }

    async UniTaskVoid Rotate(CancellationToken token)
    {
        var limit = Random.value < 0.5f ? rotationLimits.min : rotationLimits.max;

        await UniTask.Delay((int)(Random.Range(0f, waitTime) * 1000));

        while (!token.IsCancellationRequested)
        {
            var current = aim.rotation;
            var end = Quaternion.Euler(0, 0, limit);

            var t = 0f;
            while (aim.rotation != Quaternion.Euler(0, 0, limit) && t < 1.01f)
            {
                t += rotationSpeed * Time.deltaTime;
                aim.rotation = Quaternion.Slerp(current, end, t);

                await UniTask.NextFrame();
            }

            if (limit == rotationLimits.min)
                limit = rotationLimits.max;
            else
                limit = rotationLimits.min;

            await UniTask.Delay((int)(waitTime * 1000));
        }
    }

    public float GetCoverageSize()
    {
        var meshPoints = fov.GetFOVSnapshot();

        float temp = 0;
        int i = 0;

        for(; i < meshPoints.Count; i++)
        {
            if(i != meshPoints.Count - 1)
            {
                float mulA = meshPoints[i].x * meshPoints[i + 1].y;
                float mulB = meshPoints[i + 1].x * meshPoints[i].y;
                temp = temp + (mulA - mulB);
            }
            else
            {
                float mulA = meshPoints[i].x * meshPoints[0].y;
                float mulB = meshPoints[0].x * meshPoints[i].y;
                temp = temp + (mulA - mulB);
            }
        }

        temp *= 0.5f;

        return Mathf.Abs(temp);
    }

    public Vector3 GetCoverageCenter()
    {
        var meshPoints = fov.GetFOVSnapshot();
        Vector3 total = Vector3.zero;

        for(int i = 0; i < meshPoints.Count; i++)
        {
            total = new Vector3(total.x + meshPoints[i].x, total.y + meshPoints[i].y, total.z + meshPoints[i].z);
        }

        return total / meshPoints.Count;
    }

    public List<(float angle, int coverage)> GetTopDirectionAngles()
    {
        var temp = new List<(float angle, int coverage)>();

        for(int i = 0; i < Mathf.RoundToInt(rotationLimits.max - rotationLimits.min); i += placementAngleResolution)
        {
            var cellsInView = GetCellsInView(rotationLimits.min + i);
            var count = cellsInView.Count;

            temp.Add((rotationLimits.min + i, count));
        }

        return temp.OrderByDescending(x => x.coverage).ToList();
    }

    public List<MazeCell> GetCellsInView()
    {
        List<Collider2D> cells = new List<Collider2D>();
        ContactFilter2D filter = new ContactFilter2D();
        filter.layerMask = 1<<10;
        filter.useLayerMask = true;
        filter.useTriggers = true;
        Physics2D.OverlapCircle(transform.position, fov.viewRadius, filter, results: cells);

        var cellsInView = new List<MazeCell>();

        foreach(var cell in cells)
        {
            var mazeCell = cell.GetComponent<MazeCell>();
            var hit = Physics2D.Linecast(transform.position, cell.transform.position, fov.obstacleMask);
            if (hit.collider == null && Vector2.Distance(transform.position, cell.transform.position) <= fov.viewRadius
                && mazeCell.state < 2)
                cellsInView.Add(mazeCell);
        }
        
        return cellsInView;
    }

    public List<MazeCell> GetCellsInView(float angle)
    {
        List<Collider2D> cells = new List<Collider2D>();
        ContactFilter2D filter = new ContactFilter2D();
        filter.layerMask = 1 << 10;
        filter.useLayerMask = true;
        filter.useTriggers = true;
        Physics2D.OverlapCircle(transform.position, fov.viewRadius, filter, results: cells);

        var cellsInView = new List<MazeCell>();

        foreach (var cell in cells)
        {
            Vector2 cellDir = cell.transform.position - transform.position;
            //float cellAngle = Mathf.Atan2(cellDir.y, cellDir.x) * Mathf.Rad2Deg - 90f;
            //cellAngle *= -1f;
            float cellAngle = (Mathf.Atan2(cellDir.y, cellDir.x) * Mathf.Rad2Deg + 360f - 90f) % 360f;
            bool isInFOV = cellAngle >= angle - fov.viewAngle / 2f && cellAngle <= angle + fov.viewAngle / 2f;

            var mazeCell = cell.GetComponent<MazeCell>();
            var hit = Physics2D.Linecast(transform.position, cell.transform.position, fov.obstacleMask);
            if (hit.collider == null && Vector2.Distance(transform.position, cell.transform.position) <= fov.viewRadius
                && mazeCell.state < 2 && isInFOV)
                cellsInView.Add(mazeCell);
        }

        return cellsInView;
    }

    bool firstTime = true;
    Color randomColor;

    private void OnDrawGizmos()
    {
        var list = GetCellsInView();

        if (firstTime)
        {
            randomColor = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1f);
            Gizmos.color = randomColor;
            firstTime = false;
        }

        int i = 0;
        for(; i < list.Count; )
        {
            Gizmos.color = randomColor;
            Gizmos.DrawWireSphere(list[i].transform.position, GameManager.CellDiagonal / 4f);
            i++;
        }
    }
}
