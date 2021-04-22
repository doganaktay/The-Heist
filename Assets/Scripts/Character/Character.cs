using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

public abstract class Character : MonoBehaviour
{
    [Header("Character Parameters")]

    protected MazeCell currentCell, lastCell;
    public MazeCell CurrentCell { get => currentCell; private set => currentCell = value; }
    public MazeCell LastCell { get => lastCell; private set => lastCell = value; }
    readonly static int cellLayerMask = 1 << 10;
    protected Collider2D[] posHits;
    protected Collider2D previousHit;
    [SerializeField] protected bool isOnGrid = true;
    [Range(0f, 1f), SerializeField] protected float pathDriftMultiplier = 0.2f;
    [SerializeField] protected MinMaxData lagPercent;
    [SerializeField] protected float cutCornerPercent = 0.15f;
    [SerializeField] protected MinMaxData speed;
    protected float currentSpeed;
    protected Quaternion derivative;
    [SerializeField] protected MinMaxData turnSpeed;
    protected float currentTurnSpeed;
    public bool ShouldRun { get; set; }
    protected bool isMoving = false;
    protected MazeCell currentTargetCell, nextTargetCell;
    protected List<MazeCell> currentPath;
    public List<MazeCell> CurrentPath => currentPath;

    public int maxTravelDistance, distanceTravelled;

    [HideInInspector] public Transform aim; // used if Character has aim for LookAt
    [HideInInspector] public bool AimOverride { get; set; } = false;
    [HideInInspector] public Transform aimOverrideTarget;

    public Action PositionChange;

    //UniTask Async
    public CancellationToken lifetimeToken;
    protected CancellationTokenSource moveTokenSource = new CancellationTokenSource();

    #region MonoBehaviour

    private void OnEnable()
    {
        lifetimeToken = this.GetCancellationTokenOnDestroy();
        NotificationModule.AddListener(HandleNotification);
    }

    private void OnDisable()
    {
        NotificationModule.RemoveListener(HandleNotification);
    }

    protected virtual void Start()
    {
        posHits = new Collider2D[10];

        currentSpeed = GameManager.rngFree.Range(speed.min, speed.max);
        currentTurnSpeed = GameManager.rngFree.Range(turnSpeed.min, turnSpeed.max);
    }
    
    protected virtual void Update()
    {
        if (isOnGrid && TrackPosition())
            PositionChange?.Invoke();

    }

    #endregion

    bool TrackPosition()
    {
        int hitCount = Physics2D.OverlapCircleNonAlloc(transform.position, 1f, results: posHits, cellLayerMask);

        if (hitCount > 0)
        {
            float dist = Mathf.Infinity;
            int closestIndex = 0;
            for (int i = 0; i < hitCount; i++)
            {
                //var temp = Vector2.Distance(posHits[i].transform.position, transform.position);
                var temp = (posHits[i].transform.position - transform.position).sqrMagnitude;
                if (temp < dist)
                {
                    dist = temp;
                    closestIndex = i;
                }
            }

            if (posHits[closestIndex] == previousHit) { return false; }

            previousHit = posHits[closestIndex];
            lastCell = currentCell;
            currentCell = posHits[closestIndex].GetComponent<MazeCell>();

            return true;
        }

        return false;
    }

    public MazeCell PeekPath() => (!isMoving || currentPath.Count < 2) ? null : currentPath[1];
    public MazeCell PeekPath(float depthPercent)
    {
        if (!isMoving || depthPercent < 0)
            return null;

        var count = currentPath.Count;

        return currentPath[Mathf.Min(Mathf.RoundToInt((count - 1) * depthPercent), count - 1)];
    }

    protected abstract void HandleNotification(MazeCell cell, CellNotificationData data);

    #region Movement

    public bool IsMoving { get => isMoving; }

    public void Move(MazeCell targetCell, PathLayer pathLayer = PathLayer.Base)
    {
        PathRequestManager.RequestPath(new PathRequest(OnPathFound, pathLayer, currentCell, targetCell));
    }

    public void Move(PathLayer pathLayer = PathLayer.Base)
    {
        PathRequestManager.RequestPath(new PathRequest(OnPathFound, pathLayer, currentCell));
    }

    public void Move(MazeCell targetCell, int forcedIndex, PathLayer pathLayer = PathLayer.Base)
    {
        PathRequestManager.RequestPath(new PathRequest(OnPathFound, pathLayer, currentCell, targetCell), forcedIndex);
    }

    public void OnPathFound(List<MazeCell> path)
    {
        if (path[path.Count - 1] == currentCell)
            return;

        //moveTokenSource.Clear();
        //moveTokenSource = new CancellationTokenSource().Token.Merge(lifetimeToken);

        StopGoTo();

        StartGoTo(path).Forget();
    }

    async UniTaskVoid StartGoTo(List<MazeCell> path)
    {
        if (!ShouldRun)
            await GoTo(path, speed.min, moveTokenSource.Token);
        else
            await GoTo(path, speed.max, moveTokenSource.Token);
    }

    public void StopGoTo()
    {
        moveTokenSource.Cancel();
        moveTokenSource.Dispose();
        moveTokenSource = new CancellationTokenSource().Token.Merge(lifetimeToken);

        isMoving = false;
        currentPath?.Clear();
    }

    async UniTask GoTo(List<MazeCell> path, float speed, CancellationToken token)
    {
        currentPath = path;

        isMoving = true;

        int i = 1;

        MazeCell lastCell = currentCell;
        Vector2 drift = GameManager.rngFree.NextInsideUnitCircle().normalized * (GameManager.CellDiagonal * pathDriftMultiplier);
        Vector3 fromPos = transform.position;
        currentTargetCell = path[i];

        nextTargetCell = i < path.Count - 1 ? path[i + 1] : null;
        Vector3 target = currentTargetCell.transform.position + (Vector3)drift;
        Vector3 lookPos;

        Vector2 bias = Vector2.zero;
        if (nextTargetCell != null && currentCell != currentTargetCell)
            bias = MazeDirections.GetDirectionBiasVector(lastCell, currentTargetCell, nextTargetCell)
                   * (GameManager.CellDiagonal * (GameManager.rngFree.Roll() * cutCornerPercent));
                   //* (GameManager.CellDiagonal * GameManager.rngFree.Range(0, cutCornerPercent));

        target += (Vector3)bias;

        while (i < path.Count && !token.IsCancellationRequested)
        {
            if (!AimOverride)
            {
                if (nextTargetCell != null)
                {
                    bool wallCheck = currentCell == currentTargetCell ?
                                     MazeDirections.CheckAhead(currentTargetCell, nextTargetCell)
                                   : MazeDirections.CheckAhead(currentCell, currentTargetCell);

                    if (wallCheck)
                    {
                        var total = (target - fromPos).sqrMagnitude;
                        var current = (transform.position - fromPos).sqrMagnitude;
                        var t = (current / total) - GameManager.rngFree.Range(lagPercent.min, lagPercent.max);

                        lookPos = t < 0 ? (Vector2)target : Vector2.Lerp(target, nextTargetCell.transform.position, t);
                    }
                    else
                        lookPos = target;

                }
                else
                    lookPos = target;

                if (aim != null)
                    aim.Face(lookPos, ref derivative, currentTurnSpeed);
                else
                    transform.Face(lookPos, ref derivative, currentTurnSpeed);
            }

            transform.position = Vector2.MoveTowards(transform.position, target, speed * Time.deltaTime);

            //if ((target - transform.position).sqrMagnitude < 1f)
            if (transform.position == target)
            {
                i++;
                drift = GameManager.rngFree.NextInsideUnitCircle().normalized * (GameManager.CellDiagonal * pathDriftMultiplier);

                fromPos = transform.position;

                // increment distance travelled for AI calculations
                distanceTravelled++;

                if (i < path.Count)
                {
                    lastCell = currentTargetCell;
                    currentTargetCell = path[i];
                    target = currentTargetCell.transform.position + (Vector3)drift;
                }

                nextTargetCell = i < path.Count - 1 ? path[i + 1] : null;

                if (nextTargetCell != null)
                    bias = MazeDirections.GetDirectionBiasVector(lastCell, currentTargetCell, nextTargetCell)
                        * (GameManager.CellDiagonal * GameManager.rngFree.Range(0, cutCornerPercent));
                else
                    bias = Vector2.zero;

                target += (Vector3)bias;
            }

            await UniTask.NextFrame(token);
        }

        isMoving = false;
        currentPath.Clear();
    }

    public async UniTask GoToLocal(CancellationToken token, Vector3 target, bool shouldRun = false)
    {
        isMoving = true;

        while (!token.IsCancellationRequested && transform.position != target)
        {
            Face(target);

            transform.position = Vector2.MoveTowards(transform.position, target, (shouldRun ? speed.max : speed.min) * Time.deltaTime);

            await UniTask.NextFrame();
        }

        isMoving = false;
    }

    public void ResetDistanceTravelled() => distanceTravelled = 0;

    public void Face(Transform target) => transform.Face(target, ref derivative, currentTurnSpeed);
    public void Face(Vector3 target) => transform.Face(target, ref derivative, currentTurnSpeed);
    public void Face(Quaternion target) => transform.Face(target, ref derivative, currentTurnSpeed);

    public void Face(Transform target, float maxDelta) => transform.Face(target, ref derivative, maxDelta);
    public void Face(Vector3 target, float maxDelta) => transform.Face(target, ref derivative, maxDelta);
    public void Face(Quaternion target, float maxDelta) => transform.Face(target, ref derivative, maxDelta);

    #endregion
}
