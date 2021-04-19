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
    Chase,
    Social
}

public enum ChartedPathType
{
    Loop,
    Pursuit
}

public abstract class AI : Character, IBehaviorTree
{
    [Space]
    [Header("AI")]
    // references
    [SerializeField] protected FieldOfView fieldOfView;
    [SerializeField] Transform body;
    [HideInInspector] public AIManager manager;
    [HideInInspector] public PathDesigner pathDesigner = PathDesigner.Instance;

    [Space]
    [Header("Main Utility Parameters")]
    // AI behavior params
    [Range(0f, 1f), Tooltip("Used for movement decisions")]
    public float fitness;
    [Range(0f, 1f), Tooltip("Used for coordination and biasing actions to be prescient")]
    public float foresight;
    [Range(0f, 1f), Tooltip("Used for action decisions (lazy - 0 to proactive - 1)")]
    public float initiative;
    [Tooltip("Used in constructing pursuit paths")]
    public int memory;

    [Space]
    [Header("Base Parameters")]
    // base params
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
    [SerializeField]
    float FOVAdjustTime = 1f;
    [SerializeField]
    MinMaxData defaultPostTime;
    public bool ShouldPost { get; private set; } = false;
    public bool IsPosting { get; set; } = false;

    // behavior params and props
    public bool RegisterPlayer { get; private set; }
    public bool IsAlert { get; private set; }
    public void SetMaxExposureTime() => fieldOfView.SetMaxExposureTime();
    public float AwarenessDistance => fieldOfView.viewRadius;
    public LayerMask ViewMask => fieldOfView.obstacleMask;
    public bool IsActive { get; set; }
    public MazeCell PlayerObservationPoint { get; set; }
    public MazeCell PointOfInterest { get; set; }
    [HideInInspector] public Character followTarget;
    public int SearchAvoidIndex { get; set; } = -1;
    public bool ReadyForPursuit { get; set; } = false;

    [Space]
    [Header("Social Parameters")]
    [HideInInspector] public List<AI> socialTargets;
    public bool CanSocialize { get; set; } = true;
    public bool IsSocializing { get; set; } = false;
    [SerializeField, Tooltip("Time (in seconds) needed to pass before AI can socialize again")]
    MinMaxData socialRepeatTime;
    [SerializeField, Tooltip("Time (in seconds) AI will spend socializing")]
    MinMaxData socialSpendTime;
    public bool WillSocialize => CanSocialize && UnityEngine.Random.value > initiative;
    public float CurrentSocialTime { get; set; }

    // head movement
    float randomTimeBuffer;
    int[] trigExponents = new int[3];
    float headMoveCoefficient;
    float multiplier;
    int lastSign;

    // charted paths and assigned indices
    [HideInInspector] public ChartedPath loop;
    [HideInInspector] public ChartedPath pursuit;
    [HideInInspector] public List<int> assignedIndices;

    // behavior tree
    public NodeBase BehaviorTree { get; set; }
    float btWaitTime = 0.1f;
    UniTask currentBehavior;
    public UniTask CurrentBehavior => currentBehavior;
    public ActionNode ActiveActionNode;
    public BehaviorType CurrentBehaviorType { get; set; }

    //UniTask Async
    protected CancellationTokenSource behaviorTreeTokenSource = new CancellationTokenSource();
    protected CancellationTokenSource behaviorTokenSource = new CancellationTokenSource();
    protected CancellationTokenSource fovTokenSource = new CancellationTokenSource();

    #region MonoBehaviour

    protected override void Start()
    {
        base.Start();

        SetUtilityParams();

        fieldOfView = GetComponentInChildren<FieldOfView>();
        fieldOfView.AccumulateExposure = true;
        currentRegisterThreshold = UnityEngine.Random.Range(registerThreshold.min, registerThreshold.max);
        fieldOfView.ExposureLimit = currentRegisterThreshold;

        maxTravelDistance = Mathf.RoundToInt(GameManager.CellCount * fitness);
        distanceTravelled = UnityEngine.Random.Range(0, maxTravelDistance);

        Track(lifetimeToken).Forget();
        HeadMovement(lifetimeToken).Forget();

        SetRandomTimeBuffer();
        SetHeadMoveParams();

        GenerateBehaviorTree();
        RunBehaviorTree(behaviorTreeTokenSource.Token.Merge(lifetimeToken).Token).Forget();
    }

    //string lastNode = "";
    protected override void Update()
    {
        base.Update();

        //if (ActiveActionNode != null && ActiveActionNode.Name != lastNode)
        //{
        //    Debug.Log($"{gameObject.name} new active node: {ActiveActionNode.Name}");
        //    lastNode = ActiveActionNode.Name;
        //}
    }

    #endregion MonoBehaviour

    #region BehaviorTree

    protected abstract void GenerateBehaviorTree();

    async UniTask RunBehaviorTree(CancellationToken token)
    {
        await UniTask.Delay(1000, false, PlayerLoopTiming.Update, token);

        while (!token.IsCancellationRequested && enabled)
        {
            (BehaviorTree as Node).Run();
            await UniTask.Delay((int)(btWaitTime * 1000), false, PlayerLoopTiming.Update, token);
        }
    }

    void StopBehaviorTree()
    {
        behaviorTreeTokenSource.Cancel();
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

        fovTokenSource = fovTokenSource.Renew();

        AdjustFOV(radius, angle, fovTokenSource.Token).Forget();
    }

    async UniTask AdjustFOV(float radius, float angle, CancellationToken token)
    {
        float timer = 0f;
        float ratio = 0f;
        float currentRadius = fieldOfView.viewRadius;
        float currentAngle = fieldOfView.viewAngle;

        while(ratio < 1f && !token.IsCancellationRequested)
        {
            fieldOfView.viewRadius = Mathf.Lerp(currentRadius, radius, ratio);
            fieldOfView.viewAngle = Mathf.Lerp(currentAngle, angle, ratio);

            timer += Time.deltaTime;
            ratio = timer / FOVAdjustTime;

            await UniTask.NextFrame(token);
        }

        if(!token.IsCancellationRequested)
            fieldOfView.SetShaderRadius(fieldOfView.viewRadius);
    }

    static List<(float radius, float angle)> fovPresets = new List<(float radius, float angle)>()
    {
        (1, 1),
        (60, 60),
        (70, 80),
        (80, 100),
        (30, 40)
    };

    #endregion

    #region Head Movement

    protected async UniTask HeadMovement(CancellationToken token)
    {
        while(!token.IsCancellationRequested && !AimOverride)
        {
            var timeToUse = Time.time + randomTimeBuffer;
            float amount = 1;

            amount *= Mathf.Sin(headMoveCoefficient * timeToUse);
            var sign = Mathf.Sign(amount);

            if (lastSign != sign)
            {
                multiplier = UnityEngine.Random.Range(0, 2f);
                lastSign = (int)sign;
            }

            var final = amount * multiplier * Mathf.Max(0, 1 - exposureRatio);

            var rot = Quaternion.Euler(0f, 0f, final);
            transform.rotation = rot * transform.rotation;

            await UniTask.NextFrame();
        }
    }

    float RandomValue()
    {
        var value = Mathf.Sin(Vector2.Dot(UnityEngine.Random.insideUnitCircle, new Vector2(12.9898f, 4.1414f))) * 43758.5453;
        var fract = Convert.ToSingle(value - (int)value);
        return fract * 100f;
    }
    
    void SetRandomTimeBuffer() => randomTimeBuffer = RandomValue();
    
    void SetHeadMoveParams()
    {
        for(int i = 0; i < trigExponents.Length; i++)
            trigExponents[i] = UnityEngine.Random.value > 0.5 ? 1 : 3;

        headMoveCoefficient = UnityEngine.Random.Range(4f, 5f);
    }

    #endregion

    #region AI Actions

    public async UniTask Disable(CancellationToken token, float time = -1)
    {
        StopBehaviorTree();

        behaviorTokenSource.Cancel();
        StopGoTo();

        SetBehaviorParams(BehaviorType.Disabled, FOVType.Disabled, false);

        ClearBehaviorTreeData();

        if (time > -1)
        {
            await UniTask.Delay((int)(time * 1000), false, PlayerLoopTiming.Update, token);

            Revive();
        }
    }

    public void Disable(float time = -1)
    {
        Disable(lifetimeToken, time).Forget();
    }

    public void Revive()
    {
        SetBehaviorParams(BehaviorType.Investigate, FOVType.Alert, false);
        SetAlertStatus();

        behaviorTreeTokenSource = new CancellationTokenSource();
        RunBehaviorTree(behaviorTreeTokenSource.Token.Merge(lifetimeToken).Token).Forget();
    }

    public void SetBehavior(ActionNode nextActiveNode, Func<CancellationToken, UniTask> behavior)
    {
        behaviorTokenSource = behaviorTokenSource.Renew().Token.Merge(lifetimeToken);

        ActiveActionNode = nextActiveNode;
        IsActive = false;
        isMoving = false;

        StopGoTo();

        behavior(behaviorTokenSource.Token).Forget();
    }

    public void SetBehaviorParams(BehaviorType behaviorType, FOVType fovType, bool shouldRun)
    {
        CurrentBehaviorType = behaviorType;
        SetFOV(fovType);
        ShouldRun = shouldRun;
    }

    public bool IsActiveNode(ActionNode node) => node == ActiveActionNode;

    public async UniTask GoTo(CancellationToken token, MazeCell cell)
    {
        Move(cell);

        await UniTask.NextFrame(token);

        while (isMoving && !token.IsCancellationRequested)
            await UniTask.NextFrame(token);
    }

    public async UniTask GoTo(CancellationToken token, MazeCell cell, MazeCell abortWhenSeen)
    {
        Move(cell);

        await UniTask.NextFrame(token);

        while (isMoving && !token.IsCancellationRequested)
        {
            if (CanSeeCell(abortWhenSeen))
            {
                StopGoTo();
                break;
            }

            await UniTask.NextFrame(token);
        }
    }

    public async UniTask GoTo(CancellationToken token, MazeCell cell, Transform target, float stopDistance)
    {
        Move(cell);

        await UniTask.NextFrame(token);

        while (isMoving && !token.IsCancellationRequested)
        {
            if ((target.position - transform.position).sqrMagnitude < stopDistance * stopDistance)
            {
                StopGoTo();
                break;
            }

            await UniTask.NextFrame(token);
        }
    }

    public async UniTask GoTo(CancellationToken token, MazeCell cell, Transform target, Func<bool> stopCondition)
    {
        Move(cell);

        await UniTask.NextFrame(token);

        while (isMoving && !token.IsCancellationRequested)
        {
            if (stopCondition())
            {
                StopGoTo();
                break;
            }

            await UniTask.NextFrame(token);
        }
    }

    public async UniTask GoTo(CancellationToken token, MazeCell cell, int forcedIndex)
    {
        Move(cell, forcedIndex);

        await UniTask.NextFrame(token);

        while (isMoving && !token.IsCancellationRequested)
            await UniTask.NextFrame(token);
    }

    public async UniTask GoTo(CancellationToken token, MazeCell cell, int forcedIndex, MazeCell abortWhenSeen)
    {
        Move(cell, forcedIndex);

        await UniTask.NextFrame(token);

        while (isMoving && !token.IsCancellationRequested)
        {
            if (CanSeeCell(abortWhenSeen))
            {
                StopGoTo();
                break;
            }

            await UniTask.NextFrame(token);
        }
    }

    public async UniTask LookAround(CancellationToken token, float time = -1)
    {
        var waitTime = time == -1 ? UnityEngine.Random.Range(this.waitTime.min, this.waitTime.max) : time;
        var targetRot = Quaternion.Euler(0f, 0f, currentCell.GetLookRotationAngle());

        while (waitTime > 0 && !token.IsCancellationRequested)
        {
            transform.Face(targetRot, ref derivative, currentTurnSpeed);

            if (transform.rotation == targetRot)
            {
                targetRot = Quaternion.Euler(0f, 0f, currentCell.GetLookRotationAngle());

                await UniTask.Delay((int)(UnityEngine.Random.Range(1f, 3f) * 1000), false, PlayerLoopTiming.Update, token);
            }

            waitTime -= Time.deltaTime;
            await UniTask.NextFrame(token);
        }
    }

    public void CopyBehaviorState(AI other)
    {
        // this is a brutish way of copying
        // need to be mindful of a growing behavior tree
        // as more state copies may be needed

        RegisterPlayer = other.RegisterPlayer;
        ReadyForPursuit = other.ReadyForPursuit;
        PlayerObservationPoint = other.PlayerObservationPoint;
        PointOfInterest = other.PointOfInterest;
        IsAlert = other.IsAlert;
    }

    #endregion

    #region Getters and Setters    

    void SetUtilityParams()
    {
        fitness = fitness.GetScaledRange().GetRandomInRange();
        foresight = foresight.GetScaledRange().GetRandomInRange();
        initiative = initiative.GetScaledRange().GetRandomInRange();
    }

    //public float GetPostTime() => defaultPostTime.GetRandomInRange() * (AreaFinder.WalkableCellCount / (1 + GetCoverageSize()));
    public float GetPostTime() => defaultPostTime.GetRandomInRange();

    public int GetCoverageSize()
    {
        var size = loop.GetSize();

        if (size > 0)
            return size;

        size = GraphFinder.GetAreaCellCount(assignedIndices);

        return size;
    }

    public float GetSocialTimer() => UnityEngine.Random.Range(socialSpendTime.min, socialSpendTime.max);
    public float GetSocialResetTimer() => UnityEngine.Random.Range(socialRepeatTime.min, socialRepeatTime.max);
    public void SetSocialTargets(List<AI> targets)
    {
        if (targets.Contains(this))
            targets.Remove(this);

        socialTargets = targets;
    }

    public List<T> GetVisible<T>(BehaviorType behaviorType, bool exactMatch = false) where T : AI => fieldOfView.GetVisible<T>(behaviorType, exactMatch);

    public void SetPursuit(MazeCell start, int avoidIndex = -1)
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

    public async UniTaskVoid WaitUntilCanSocialize()
    {
        while (!lifetimeToken.IsCancellationRequested)
        {
            var timer = socialRepeatTime.GetRandomInRange();

            await UniTask.Delay((int)(timer * 1000), false, PlayerLoopTiming.Update, lifetimeToken);

            socialTargets.Clear();
            CanSocialize = true;
        }
    }

    public void SetAlertStatus()
    {
        IsAlert = true;
        alertTimer = 0f;
    }

    async UniTask Track(CancellationToken lifetimeToken)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(1), false, PlayerLoopTiming.Update, lifetimeToken);

        while (!lifetimeToken.IsCancellationRequested)
        {
            if (CurrentBehaviorType != BehaviorType.Disabled)
            {
                exposureRatio = fieldOfView.ContinuousExposureTime / currentRegisterThreshold;

                if (!RegisterPlayer)
                {
                    AimOverride = false;

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
                        registerTimer = 0;
                        alertTimer = 0f;
                    }
                    else
                    {
                        registerTimer += Time.deltaTime;
                    }

                    AimOverride = true;
                    aimOverrideTarget = GameManager.player.transform;
                    transform.Face(aimOverrideTarget, ref derivative, currentTurnSpeed);

                    if (registerTimer >= lostTargetThreshold)
                    {
                        AimOverride = false;
                        RegisterPlayer = false;
                        registerTimer = 0;
                    }
                }

                if (IsAlert && CurrentBehaviorType < BehaviorType.Follow)
                {
                    alertTimer += Time.deltaTime;

                    if (alertTimer > maintainAlertTime)
                    {
                        IsAlert = false;
                        alertTimer = 0;
                        maintainAlertTime += maintainAlertTimeIncrement;
                    }
                }


                ShouldPost = distanceTravelled >= maxTravelDistance ? true : false;

                fieldOfView.SetShaderPosition(transform.position);
                fieldOfView.SetShaderBlend(RegisterPlayer || IsAlert ? 1f : exposureRatio);
            }

            await UniTask.NextFrame(lifetimeToken);
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

    public Vector3 CalculateAverageLookPos<T>(List<T> others) where T : MonoBehaviour
    {
        var final = Vector3.zero;

        foreach (var other in others)
            final += other.transform.position;

        return final / others.Count;
    }

    public Vector3 CalculateAverageHeading(List<MazeCell> path, int start, int lookAheadLimit = -1)
    {
        Vector3 heading = Vector3.zero;

        for (int i = start; i < path.Count && (lookAheadLimit == -1 ? true : i < start + lookAheadLimit); i++)
            heading += path[i].transform.position;

        heading /= path.Count - start;

        return heading;
    }

    public MazeCell GetCenterCell<T>(List<T> others) where T : AI
    {
        Vector3 center = transform.position;
        foreach (var other in others)
            center += other.transform.position;

        center /= others.Count + 1;

        Collider2D[] results = new Collider2D[1];

        if(Physics2D.OverlapCircleNonAlloc(center, 2f, results, 1<<10) > 0)
        {
            if (results[0].TryGetComponent(out MazeCell cell))
                return cell;
        }

        return null;
    }

    #endregion
}
