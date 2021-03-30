using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;
using Cysharp.Threading.Tasks;
using System.Threading;

public enum BehaviorType
{
    Disabled = -1,
    Casual,
    Investigate,
    Follow,
    Check,
    Pursue,
    Chase
}

public enum FOVType
{
    Disabled,
    Regular,
    Alert,
    Chase
}

public enum ChartedPathType
{
    Loop,
    Pursuit
}

public abstract class AI : Character, IBehaviorTree
{
    [SerializeField]
    protected FieldOfView fieldOfView;
    [SerializeField]
    Transform body;
    [SerializeField]
    MinMaxData waitTime;
    [SerializeField, Tooltip("Time (in seconds) exposure to register player")]
    MinMaxData registerThreshold;
    float currentRegisterThreshold;
    float registerTimer = 0;
    [SerializeField, Tooltip("How much time needs to pass without a sighting to lose track of player after having found him")]
    float lostTargetThreshold = 3f;
    [SerializeField]
    float maintainAlertTime = 5f;
    [SerializeField]
    float maintainAlertTimeIncrement = 5f;
    float alertTimer = 0f;
    float exposureRatio;

    Coroutine FOVRoutine;
    [SerializeField]
    float FOVAdjustTime = 1f;

    [HideInInspector]
    public AIManager manager;
    [HideInInspector]
    public PathDesigner pathDesigner = PathDesigner.Instance;

    public bool RegisterPlayer { get; private set; }
    public bool IsAlert { get; private set; }
    public AI GetVisibleAI(BehaviorType behaviorType) => fieldOfView.GetVisibleAI(behaviorType);
    public void SetMaxExposureTime() => fieldOfView.SetMaxExposureTime();
    public float AwarenessDistance => fieldOfView.viewRadius;
    public LayerMask ViewMask => fieldOfView.obstacleMask;
    public bool IsActive { get; set; }
    public MazeCell PlayerObservationPoint { get; set; }
    public MazeCell PointOfInterest { get; set; }
    public Character followTarget;
    public int SearchAvoidIndex { get; set; } = -1;
    public bool ReadyForPursuit { get; set; } = false;

    Coroutine trackStatusRoutine;

    [Range(0f,1f), Tooltip("Used for movement decisions")]
    public float fitness;
    [Range(0f, 1f), Tooltip("Used for coordination and biasing actions to be prescient")]
    public float foresight;
    [Tooltip("Used in constructing pursuit paths")]
    public int memory;

    // charted paths
    [HideInInspector] public ChartedPath loop;
    [HideInInspector] public ChartedPath pursuit;
    [HideInInspector] public List<int> assignedIndices;

    public NodeBase BehaviorTree { get ; set; }
    Coroutine behaviourTreeRoutine;
    YieldInstruction btWaitTime = new WaitForSeconds(.1f);
    Coroutine currentBehavior;
    public Coroutine CurrentBehavior => currentBehavior;
    public ActionNode ActiveActionNode;
    public BehaviorType CurrentBehaviorType { get; set; }

    //UniTask Async
    protected CancellationTokenSource behaviorTokenSource = new CancellationTokenSource();

    #region MonoBehaviour

    protected override void Start()
    {
        base.Start();

        fieldOfView = GetComponentInChildren<FieldOfView>();
        fieldOfView.AccumulateExposure = true;
        fieldOfView.ExposureLimit = currentRegisterThreshold;

        currentRegisterThreshold = UnityEngine.Random.Range(registerThreshold.min, registerThreshold.max);

        //trackStatusRoutine = StartCoroutine(TrackStatus());
        lifetimeToken = this.GetCancellationTokenOnDestroy();
        Track(lifetimeToken).Forget();

        SetRandomTimeBuffer();
        SetExponentsAndCoefficients();

        GenerateBehaviorTree();
        behaviourTreeRoutine = StartCoroutine(RunBehaviorTree());
    }

    protected override void Update()
    {
        base.Update();
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

    public void SetFOV(FOVType type)
    {
        float radius, angle;

        if (type == FOVType.Disabled)
        {
            fieldOfView.Disable();
            return;
        }
        
        if (!fieldOfView.isActiveAndEnabled)
            fieldOfView.enabled = true;

        radius = fovPresets[(int)type].radius;
        angle = fovPresets[(int)type].angle;

        if (FOVRoutine != null)
            StopCoroutine(FOVRoutine);

        StartCoroutine(AdjustFOV(radius, angle));
    }

    IEnumerator AdjustFOV(float radius, float angle)
    {
        float timer = 0f;
        float ratio = 0f;
        float currentRadius = fieldOfView.viewRadius;
        float currentAngle = fieldOfView.viewAngle;

        while(ratio < 1f)
        {
            fieldOfView.viewRadius = Mathf.Lerp(currentRadius, radius, ratio);
            fieldOfView.viewAngle = Mathf.Lerp(currentAngle, angle, ratio);

            timer += Time.deltaTime;
            ratio = timer / FOVAdjustTime;

            yield return null;
        }
    }

    #endregion

    #region Head Movement

    float randomTimeBuffer;
    int[] trigExponents = new int[3];
    float headMoveCoefficient;
    float multiplier;
    int lastSign;

    protected void AddHeadMovement()
    {
        var timeToUse = Time.time + randomTimeBuffer;
        float amount = 1;

        amount *= Mathf.Sin(headMoveCoefficient * timeToUse);
        var sign = Mathf.Sign(amount);

        if(lastSign != sign)
        {
            multiplier = UnityEngine.Random.Range(0, 2f);
            lastSign = (int)sign;
        }

        var final = amount * multiplier * Mathf.Max(0, (1 - exposureRatio));

        var rot = Quaternion.Euler(0f, 0f, final);
        transform.rotation = rot * transform.rotation;
    }

    float SmoothMin(float a, float b, float k)
    {
        float res = Mathf.Pow(2, -k * a) + Mathf.Pow(2, -k * b);
        return -Mathf.Log(res) / k;
    }

    float RandomValue()
    {
        var value = Mathf.Sin(Vector2.Dot(UnityEngine.Random.insideUnitCircle, new Vector2(12.9898f, 4.1414f))) * 43758.5453;
        var fract = Convert.ToSingle(value - (int)value);
        return fract * 100f;
    }
    
    void SetRandomTimeBuffer() => randomTimeBuffer = RandomValue();
    
    void SetExponentsAndCoefficients()
    {
        for(int i = 0; i < trigExponents.Length; i++)
            trigExponents[i] = UnityEngine.Random.value > 0.5 ? 1 : 3;
            //trigExponents[i] = 1;

        headMoveCoefficient = UnityEngine.Random.Range(4f, 5f);
    }

    static List<(float radius, float angle)> fovPresets = new List<(float radius, float angle)>()
    {
        (1f, 1f),
        (60f, 60f),
        (70, 80f),
        (80f, 100f)
    };

    #endregion

    #region AI Actions

    public IEnumerator Disable(float time = -1)
    {
        StopCoroutine(behaviourTreeRoutine);

        behaviorTokenSource.Cancel();

        if (currentBehavior != null)
        {
            StopCoroutine(currentBehavior);
            IsActive = false;
        }

        if (currentMovement != null)
        {
            StopCoroutine(currentMovement);
            isMoving = false;
        }

        SetBehaviorParams(BehaviorType.Disabled, FOVType.Disabled, false);

        ClearBehaviorTreeData();

        if (time > -1)
        {
            yield return new WaitForSeconds(time);

            Revive();
        }
    }

    private void ClearBehaviorTreeData()
    {
        ClearPath(ChartedPathType.Pursuit);
        PointOfInterest = null;
        PlayerObservationPoint = null;
        ReadyForPursuit = false;
        RegisterPlayer = false;
        ActiveActionNode = null;
    }

    public void Revive()
    {
        SetBehaviorParams(BehaviorType.Investigate, FOVType.Alert, false);
        SetAlertStatus();

        behaviourTreeRoutine = StartCoroutine(RunBehaviorTree());
    }

    public void SetBehavior(IEnumerator behavior)
    {
        behaviorTokenSource.Cancel();

        if (currentBehavior != null)
        {
            StopCoroutine(currentBehavior);
            IsActive = false;
        }

        if (currentMovement != null)
        {
            StopCoroutine(currentMovement);
            isMoving = false;
        }

        currentBehavior = StartCoroutine(behavior);
    }

    public void SetBehaviorParams(BehaviorType behaviorType, FOVType fovType, bool shouldRun)
    {
        CurrentBehaviorType = behaviorType;
        SetFOV(fovType);
        ShouldRun = shouldRun;
    }

    public bool IsActiveNode(ActionNode node) => node == ActiveActionNode;

    public bool CanLoopMap() => PathDesigner.Instance.MapHasCycles;

    public IEnumerator GoTo(MazeCell cell)
    {
        Move(cell);

        yield return null;

        while (isMoving)
            yield return null;
    }

    public IEnumerator GoTo(MazeCell cell, MazeCell abortWhenSeen)
    {
        Move(cell);

        yield return null;

        while (isMoving)
        {
            if (CanSeeCell(abortWhenSeen))
            {
                //Debug.Log($"{gameObject.name} can see path end at {abortWhenSeen.gameObject.name}");

                StopGoTo();
                break;
            }

            yield return null;
        }
    }

    public IEnumerator GoTo(MazeCell cell, int forcedIndex)
    {
        Move(cell, forcedIndex);

        yield return null;

        while (isMoving)
            yield return null;
    }

    public IEnumerator GoTo(MazeCell cell, int forcedIndex, MazeCell abortWhenSeen)
    {
        Move(cell, forcedIndex);

        yield return null;

        while (isMoving)
        {
            if (CanSeeCell(abortWhenSeen))
            {
                //Debug.Log($"{gameObject.name} can see path end at {abortWhenSeen.gameObject.name}");

                StopGoTo();
                break;
            }

            yield return null;
        }
    }

    public IEnumerator LookAround(float time = -1)
    {
        var waitTime = time == -1 ? UnityEngine.Random.Range(this.waitTime.min, this.waitTime.max) : time;
        var targetRot = Quaternion.Euler(0f, 0f, currentCell.GetLookRotationAngle());

        while (waitTime > 0)
        {
            transform.Face(targetRot, ref derivative, currentTurnSpeed);

            if (transform.rotation == targetRot)
            {
                targetRot = Quaternion.Euler(0f, 0f, currentCell.GetLookRotationAngle());

                yield return new WaitForSeconds(UnityEngine.Random.Range(1f, 3f));
            }

            waitTime -= Time.deltaTime;
            yield return null;
        }
    }

    #endregion

    #region Getters and Setters

    public void SetPursuit(MazeCell start)
    {
        ReadyForPursuit = true;
        PlayerObservationPoint = start;
        SearchAvoidIndex = -1;
    }
    public void SetPursuit(MazeCell start, int avoidIndex)
    {
        ReadyForPursuit = true;
        PlayerObservationPoint = start;
        SearchAvoidIndex = avoidIndex;
    }

    public ChartedPath GetPath(ChartedPathType type)
    {
        switch (type)
        {
            case ChartedPathType.Loop:
                return loop;
            case ChartedPathType.Pursuit:
                return pursuit;
        }

        return new ChartedPath(null, new int[1]);
    }

    public void SetPath(ChartedPathType type, ChartedPath path)
    {
        switch (type)
        {
            case ChartedPathType.Loop:
                loop = path;
                break;
            case ChartedPathType.Pursuit:
                pursuit = path;
                break;
        }
    }

    public void ClearPath(ChartedPathType type)
    {
        switch (type)
        {
            case ChartedPathType.Loop:
                loop.Clear();
                break;
            case ChartedPathType.Pursuit:
                pursuit.Clear();
                break;
        }
    }

    #endregion

    #region Utilities and Trackers

    public void SetAlertStatus()
    {
        IsAlert = true;
        alertTimer = 0f;
    }

    IEnumerator TrackStatus()
    {
        yield return new WaitForSeconds(1f);

        while (true)
        {
            if(CurrentBehaviorType != BehaviorType.Disabled)
            {
                exposureRatio = fieldOfView.ContinuousExposureTime / currentRegisterThreshold;

                if (!RegisterPlayer)
                {
                    AimOverride = false;
                    AddHeadMovement();

                    if (exposureRatio >= 1 || (IsAlert && (fieldOfView.CanSeePlayer() || PlayerIsVeryClose())))
                    {
                        SetAlertStatus();
                        RegisterPlayer = true;
                    }
                }
                else
                {
                    if (fieldOfView.CanSeePlayer() || (IsAlert && PlayerIsVeryClose()))
                    {
                        aimOverrideTarget = GameManager.player.transform;
                        AimOverride = true;
                        transform.Face(aimOverrideTarget, ref derivative, currentTurnSpeed);

                        registerTimer = 0;
                        alertTimer = 0f;
                    }
                    else
                    {
                        registerTimer += Time.deltaTime;
                    }

                    if(registerTimer >= lostTargetThreshold)
                    {
                        AimOverride = false;
                        AddHeadMovement();

                        RegisterPlayer = false;
                        registerTimer = 0;
                    }
                }

                if (IsAlert && (int)CurrentBehaviorType < (int)BehaviorType.Follow)
                {
                    alertTimer += Time.deltaTime;

                    if (alertTimer > maintainAlertTime)
                    {
                        IsAlert = false;
                        alertTimer = 0;
                        maintainAlertTime += maintainAlertTimeIncrement;
                    }
                }

                fieldOfView.SetColorBlendFactor(RegisterPlayer || IsAlert ? 1f : fieldOfView.ContinuousExposureTime / currentRegisterThreshold);
            }

            yield return null;
        }
    }

    async UniTask Track(CancellationToken lifetimeToken)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(1));

        while (!lifetimeToken.IsCancellationRequested)
        {
            if (CurrentBehaviorType != BehaviorType.Disabled)
            {
                exposureRatio = fieldOfView.ContinuousExposureTime / currentRegisterThreshold;

                if (!RegisterPlayer)
                {
                    AimOverride = false;
                    AddHeadMovement();

                    if (exposureRatio >= 1 || (IsAlert && (fieldOfView.CanSeePlayer() || PlayerIsVeryClose())))
                    {
                        SetAlertStatus();
                        RegisterPlayer = true;
                    }
                }
                else
                {
                    if (fieldOfView.CanSeePlayer() || (IsAlert && PlayerIsVeryClose()))
                    {
                        aimOverrideTarget = GameManager.player.transform;
                        AimOverride = true;
                        transform.Face(aimOverrideTarget, ref derivative, currentTurnSpeed);

                        registerTimer = 0;
                        alertTimer = 0f;
                    }
                    else
                    {
                        registerTimer += Time.deltaTime;
                    }

                    if (registerTimer >= lostTargetThreshold)
                    {
                        AimOverride = false;
                        AddHeadMovement();

                        RegisterPlayer = false;
                        registerTimer = 0;
                    }
                }

                if (IsAlert && (int)CurrentBehaviorType < (int)BehaviorType.Follow)
                {
                    alertTimer += Time.deltaTime;

                    if (alertTimer > maintainAlertTime)
                    {
                        IsAlert = false;
                        alertTimer = 0;
                        maintainAlertTime += maintainAlertTimeIncrement;
                    }
                }

                fieldOfView.SetColorBlendFactor(RegisterPlayer || IsAlert ? 1f : fieldOfView.ContinuousExposureTime / currentRegisterThreshold);
            }

            await UniTask.NextFrame();
        }
    }

    bool PlayerIsVeryClose() =>
        (CurrentCell == GameManager.player.CurrentCell) ||
        ((GameManager.player.transform.position - transform.position).sqrMagnitude < GameManager.CellDiagonal
        && !Physics2D.Raycast(transform.position, GameManager.player.transform.position - transform.position, GameManager.CellDiagonal, fieldOfView.obstacleMask));

    public bool CanSeeCell(MazeCell cell)
    {
        var dist = cell.transform.position - transform.position;
        var first = dist.sqrMagnitude <= fieldOfView.viewRadius * fieldOfView.viewRadius;
        var dir = Quaternion.AngleAxis(transform.rotation.eulerAngles.z, Vector3.forward) * Vector2.up;
        var second = !Physics2D.Raycast(transform.position, dir, dist.magnitude, fieldOfView.obstacleMask);

        return first && second;
    }

    public Vector3 CalculateAverageHeading(List<MazeCell> path, int start, int lookAheadLimit = -1)
    {
        Vector3 heading = Vector3.zero;

        for (int i = start; i < path.Count && (lookAheadLimit == -1 ? true : i < start + lookAheadLimit); i++)
            heading += path[i].transform.position;

        heading /= path.Count - start;

        return heading;
    }

    #endregion
}
