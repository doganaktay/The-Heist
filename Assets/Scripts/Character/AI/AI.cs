using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;

public enum BehaviorType
{
    Disabled = -1,
    Casual,
    Investigate,
    Check,
    Pursue,
    Chase
}

public enum FOVType
{
    Disabled = -1,
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
    MinMaxData lookSpeed;
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

    Coroutine FOVRoutine;
    [SerializeField]
    float FOVAdjustTime = 1f;

    [HideInInspector]
    public AIManager manager;
    [HideInInspector]
    public PathDesigner pathDesigner = PathDesigner.Instance;

    public bool RegisterPlayer { get; private set; }
    public bool IsAlert { get; private set; }
    public float AwarenessDistance => fieldOfView.viewRadius;
    public LayerMask ViewMask => fieldOfView.obstacleMask;
    public bool IsActive { get; set; }
    public MazeCell PlayerObservationPoint { get; set; }
    public MazeCell PointOfInterest { get; set; }
    public int SearchAvoidIndex { get; set; } = -1;
    public bool ReadyForPursuit { get; set; } = false;
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
    [HideInInspector] public List<int> assignedIndices;

    public NodeBase BehaviorTree { get ; set; }
    Coroutine behaviourTreeRoutine;
    YieldInstruction btWaitTime = new WaitForSeconds(.1f);
    Coroutine currentAction;
    public Coroutine CurrentAction => currentAction;
    public ActionNode ActiveActionNode;
    public BehaviorType CurrentBehavior { get; set; }

    #region MonoBehaviour
    
    protected override void Start()
    {
        base.Start();

        fieldOfView = GetComponentInChildren<FieldOfView>();
        fieldOfView.AccumulateExposure = true;
        fieldOfView.ExposureLimit = currentRegisterThreshold;
        currentSpeed = Random.Range(speed.min, speed.max);
        currentRegisterThreshold = Random.Range(registerThreshold.min, registerThreshold.max);

        // Track status
        trackStatusRoutine = StartCoroutine(TrackStatus());

        // Behavior Tree
        GenerateBehaviorTree();
        behaviourTreeRoutine = StartCoroutine(RunBehaviorTree());
    }

    protected override void Update()
    {
        base.Update();

        if (RegisterPlayer && (fieldOfView.CanSeePlayer() || (IsAlert && PlayerIsVeryClose())))
        {
            aimOverrideTarget = GameManager.player.transform;
            AimOverride = true;
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

        if (trackStatusRoutine != null)
            StopCoroutine(trackStatusRoutine);
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

    static List<(float radius, float angle)> fovPresets = new List<(float radius, float angle)>()
    {
        (60f, 60f),
        (70, 80f),
        (80f, 100f)
    };

    #endregion

    #region AI Actions

    public IEnumerator Disable(float time = -1)
    {
        SetBehaviorParams(BehaviorType.Disabled, FOVType.Disabled, false);
        StopCoroutine(behaviourTreeRoutine);

        if(currentAction != null)
        {
            StopCoroutine(currentAction);
            IsActive = false;
        }

        if(time > -1)
        {
            yield return new WaitForSeconds(time);

            Enable();
        }
    }

    public void Enable()
    {
        behaviourTreeRoutine = StartCoroutine(RunBehaviorTree());
        SetBehaviorParams(BehaviorType.Investigate, FOVType.Alert, false);
    }

    public void SetBehavior(IEnumerator behavior, ActionNode node)
    {
        if(node != null)
            Debug.Log($"Setting behavior to {node.Name}");

        if (currentAction != null)
        {
            StopCoroutine(currentAction);
            IsActive = false;
        }

        if (currentMovement != null)
        {
            StopCoroutine(currentMovement);
            isMoving = false;
        }

        currentAction = StartCoroutine(behavior);
    }

    public void SetBehaviorParams(BehaviorType behaviorType, FOVType fovType, bool shouldRun)
    {
        CurrentBehavior = behaviorType;
        SetFOV(fovType);
        ShouldRun = shouldRun;
    }

    public bool IsActiveNode(ActionNode node) => node == ActiveActionNode;

    public bool CanLoopMap() => PathDesigner.Instance.MapHasCycles;

    public bool GetLoop()
    {
        if (PathDesigner.Instance.MapHasCycles)
        {
            if(loop.cells.Length == 0)
                loop = PathDesigner.Instance.RequestPathLoop();

            return true;
        }

        return false;
    }

    public IEnumerator GoTo(MazeCell cell, bool lookAroundOnArrival = false)
    {
        //IsActive = true;

        Move(cell);

        yield return null;

        while (isMoving)
            yield return null;

        if (lookAroundOnArrival)
            yield return LookAround();

        //IsActive = false;
    }

    public IEnumerator GoTo(MazeCell cell, int forcedIndex, bool lookAroundOnArrival = false)
    {
        //IsActive = true;

        Move(cell, forcedIndex);

        yield return null;

        while (isMoving)
            yield return null;

        if (lookAroundOnArrival)
            yield return LookAround();

        //IsActive = false;
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

        Debug.Log($"{gameObject.name} finished look around");
    }

    public IEnumerable Disabled(float time = -1)
    {
        if (time > -1)
        {
            yield return new WaitForSeconds(time);
            Enable();
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
            if (!RegisterPlayer)
            {
                if(fieldOfView.ContinuousExposureTime > currentRegisterThreshold
                   || (IsAlert && (fieldOfView.CanSeePlayer() || PlayerIsVeryClose())))
                {
                    SetAlertStatus();
                    RegisterPlayer = true;

                    UnityEngine.Debug.Log($"{gameObject.name} found player. Exposure: {fieldOfView.ContinuousExposureTime}/{currentRegisterThreshold}, {fieldOfView.ContinuousExposureTime > currentRegisterThreshold}, Is alert: {IsAlert} Can see player: {fieldOfView.CanSeePlayer()} Is too close: {PlayerIsVeryClose()}");
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
                    registerTimer += Time.deltaTime;

                if(registerTimer >= lostTargetThreshold)
                {
                    RegisterPlayer = false;
                    registerTimer = 0;

                    UnityEngine.Debug.Log($"{gameObject.name} lost player");
                }
            }

            

            if (IsAlert && (int)CurrentBehavior < (int)BehaviorType.Check)
            {
                alertTimer += Time.deltaTime;

                if (alertTimer > maintainAlertTime)
                {
                    IsAlert = false;
                    alertTimer = 0;
                    maintainAlertTime += maintainAlertTimeIncrement;

                    Debug.Log($"{gameObject.name} is no longer alert");
                }
            }

            fieldOfView.SetColorBlendFactor(RegisterPlayer || IsAlert ? 1f : fieldOfView.ContinuousExposureTime / currentRegisterThreshold);

            yield return null;
        }
    }

    bool PlayerIsVeryClose() =>
        (CurrentCell == GameManager.player.CurrentCell) ||
        ((GameManager.player.transform.position - transform.position).sqrMagnitude < GameManager.CellDiagonal
        && !Physics2D.Raycast(transform.position, GameManager.player.transform.position - transform.position, GameManager.CellDiagonal, fieldOfView.obstacleMask));
    

    #endregion
}
