using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;

public enum BehaviorType
{
    Disabled = -1,
    Wander,
    Loop,
    Investigate,
    Check,
    Alert,
    Chase
}

public enum FOVType
{
    Disabled = -1,
    Regular,
    Alert,
    Chase
}

public abstract class AI : Character, IBehaviorTree
{
    [SerializeField]
    protected FieldOfView fieldOfView;
    [SerializeField]
    Transform body;
    [SerializeField]
    MinMaxData lookSpeed;
    [SerializeField]
    MinMaxData waitTime;
    [SerializeField]
    float maintainAlertTime = 5f;
    [SerializeField]
    float maintainAlertTimeIncrement = 5f;
    float alertTimer = 0f;
    bool initialized = false;

    [HideInInspector]
    public AIManager manager;
    PathDesigner pathDesigner = PathDesigner.Instance;

    public bool CanSeePlayer { get => fieldOfView.CanSeePlayer() || PlayerIsVeryClose(); }
    public bool IsAlert { get; private set; }
    public float AwarenessDistance { get => fieldOfView.viewRadius; }
    public bool IsActive { get; set; }
    public bool HasSearchTarget { get => fieldOfView.lastKnownPlayerPos != null; }
    private (MazeCell target, bool isOld) searchTarget;
    public (MazeCell target, bool isOld) SearchTarget { get => searchTarget ; set => searchTarget = value; }
    public ChartedPath loopPath;

    public NodeBase BehaviorTree { get ; set; }
    Coroutine behaviourTreeRoutine;
    YieldInstruction btWaitTime = new WaitForSeconds(.1f);

    Coroutine currentAction;
    public Coroutine CurrentAction => currentAction;
    public ActionNode ActiveActionNode;

    #region MonoBehaviour

    protected override void Start()
    {
        base.Start();

        fieldOfView = GetComponentInChildren<FieldOfView>();
        currentSpeed = Random.Range(speed.min, speed.max);

        StartCoroutine(TrackStatus());

        // Behavior Tree
        GenerateBehaviorTree();
        behaviourTreeRoutine = StartCoroutine(RunBehaviorTree());
    }

    protected override void Update()
    {
        base.Update();

        if (CanSeePlayer)
        {
            aimOverrideTarget = GameManager.player.transform;
            AimOverride = true;
            searchTarget = (GameManager.player.CurrentCell, false);
        }
        else
        {
            AimOverride = false;
        }
    }

    void OnDestroy()
    {
        if (behaviourTreeRoutine != null)
            StopCoroutine(behaviourTreeRoutine);
    }

    #endregion MonoBehaviour

    #region BehaviorTree

    protected abstract void GenerateBehaviorTree();

    IEnumerator RunBehaviorTree()
    {
        yield return new WaitForSeconds(1f);

        while (enabled)
        {
            (BehaviorTree as Node).Run();
            yield return btWaitTime;
        }
    }

    #endregion

    #region Field Of View

    void SetFOV(FOVType type)
    {
        if ((int)type < 0)
            return;

        fieldOfView.viewRadius = fovPresets[(int)type].radius;
        fieldOfView.viewAngle = fovPresets[(int)type].angle;
    }

    static List<(float radius, float angle)> fovPresets = new List<(float radius, float angle)>()
    {
        (60f, 60f),
        (70, 80f),
        (80f, 100f)
    };

    #endregion

    #region AI Actions

    public void SetBehavior(IEnumerator behavior, ActionNode node)
    {
        Debug.Log($"Setting behavior to {node.Name}");

        if (currentAction != null)
        {
            StopCoroutine(currentAction);
            IsActive = false;
        }

        currentAction = StartCoroutine(behavior);
    }

    public bool IsActiveNode(ActionNode node) => node == ActiveActionNode;

    public bool CanLoopMap() => PathDesigner.Instance.MapHasCycles;

    public bool GetLoop()
    {
        if (PathDesigner.Instance.MapHasCycles)
        {
            if(loopPath.cells == null)
                loopPath = PathDesigner.Instance.RequestPathLoop();

            return true;
        }

        return false;
    }

    public IEnumerator GoTo(MazeCell cell, bool shouldRun = false, bool lookAroundOnArrival = false)
    {
        IsActive = true;

        ShouldRun = shouldRun;

        Move(cell);

        yield return null;

        while (isMoving)
            yield return null;

        if (lookAroundOnArrival)
            yield return LookAround();

        searchTarget.isOld = true;
        IsActive = false;
    }

    public IEnumerator GoTo(MazeCell cell, int forcedIndex, bool shouldRun = false, bool lookAroundOnArrival = false)
    {
        IsActive = true;

        ShouldRun = shouldRun;

        Move(cell, forcedIndex);

        yield return null;

        while (isMoving)
            yield return null;

        if (lookAroundOnArrival)
            yield return LookAround();

        searchTarget.isOld = true;
        IsActive = false;
    }

    public IEnumerator LookAround()
    {
        var waitTime = Random.Range(this.waitTime.min, this.waitTime.max);
        var randomRot = Quaternion.Euler(new Vector3(0f, 0f, Random.Range(25f, 180f) * Mathf.Sign(Random.Range(-1f, 1f))));
        var currentRot = transform.rotation;
        var targetRot = randomRot * currentRot;
        var lookSpeed = Random.Range(this.lookSpeed.min, this.lookSpeed.max);
        var lookCurrent = 0f;

        Debug.Log($"{gameObject.name} starting {waitTime} second look around");

        while (waitTime > 0)
        {
            lookCurrent += Time.deltaTime;
            var ratio = lookCurrent / lookSpeed * (180f / randomRot.eulerAngles.z);
            var t = Mathf.Min(ratio, 1f);

            transform.rotation = Quaternion.Slerp(currentRot, targetRot, t);

            if (transform.rotation == targetRot)
            {
                currentRot = transform.rotation;
                randomRot = Quaternion.Euler(new Vector3(0f, 0f, Random.Range(25f, 180f) * Mathf.Sign(Random.Range(-1f, 1f))));
                targetRot = randomRot * currentRot;
                lookSpeed = Random.Range(this.lookSpeed.min, this.lookSpeed.max);
                lookCurrent = 0f;

                yield return new WaitForSeconds(Random.Range(1f, 3f));
            }

            waitTime -= Time.deltaTime;
            yield return null;
        }
    }

    //Quaternion GetRandomRotation()
    //{
    //    return Quaternion.Euler(0f, 0f, 0f);
    //}

    #endregion

    #region Utilities and Trackers

    IEnumerator TrackStatus()
    {
        while (true)
        {
            // track player seen
            // track player recognized

            // track alert status

            if (CanSeePlayer)
            {
                IsAlert = true;
                alertTimer = 0f;
            }
            else if (IsAlert)
            {
                alertTimer = 0f;

                while (alertTimer < maintainAlertTime && !CanSeePlayer)
                {
                    alertTimer += Time.deltaTime;
                    yield return null;
                }

                if (!CanSeePlayer)
                {
                    IsAlert = false;
                    maintainAlertTime += maintainAlertTimeIncrement;
                }
            }

            yield return null;
        }
    }

    bool PlayerIsVeryClose() =>
        IsAlert
        && (GameManager.player.transform.position - transform.position).sqrMagnitude < GameManager.CellDiagonal * GameManager.CellDiagonal
        && !Physics2D.Raycast(transform.position, GameManager.player.transform.position - transform.position, GameManager.CellDiagonal, fieldOfView.obstacleMask);
    

    #endregion
}
