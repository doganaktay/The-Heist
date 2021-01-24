using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;

public enum BehaviorType
{
    Disabled = -1,
    Wander,
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
    float alertTimer = 0f;
    bool initialized = false;

    [HideInInspector]
    public AIManager manager;

    public bool CanSeePlayer { get => fieldOfView.CanSeePlayer() || (GameManager.player.transform.position - transform.position).sqrMagnitude < GameManager.CellDiagonal * GameManager.CellDiagonal; }
    public bool IsAlert { get; private set; }
    public float AwarenessDistance { get => fieldOfView.viewRadius; }
    public bool IsActive { get; private set; }
    public bool HasSearchTarget { get => fieldOfView.lastKnownPlayerPos != null; }
    public MazeCell SearchTarget { get => GameManager.player.CurrentCell; }

    public NodeBase BehaviorTree { get ; set; }
    Coroutine behaviourTreeRoutine;
    YieldInstruction btWaitTime = new WaitForSeconds(.1f);
    BehaviorData lastBehaviorData;
    BehaviorData currentBehaviorData;

    Coroutine currentAction;

    #region MonoBehaviour

    protected override void Start()
    {
        base.Start();

        fieldOfView = GetComponentInChildren<FieldOfView>();
        currentSpeed = Random.Range(speed.min, speed.max);

        StartCoroutine(TrackAlertStatus());

        // Behavior Tree
        GenerateBehaviorTree();
        behaviourTreeRoutine = StartCoroutine(RunBehaviorTree());
    }

    protected override void Update()
    {
        base.Update();

        if (CheckForBehaviorChange() || !initialized)
        {
            TakeBehaviorAction();
            initialized = true;
        }

        if (CanSeePlayer)
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
    }

    #endregion MonoBehaviour

    #region BehaviorTree

    protected abstract void GenerateBehaviorTree();

    IEnumerator RunBehaviorTree()
    {
        while (enabled)
        {
            (BehaviorTree as Node).Run();
            yield return btWaitTime;
        }
    }

    bool CheckForBehaviorChange()
    {
        if (lastBehaviorData.type != currentBehaviorData.type || (currentBehaviorData.isRepeating && !IsActive))
        {
            lastBehaviorData = currentBehaviorData;
            return true;
        }
        else
            return false;
    }

    void TakeBehaviorAction()
    {
        if (currentAction != null)
            StopCoroutine(currentAction);

        switch (currentBehaviorData.type)
        {
            case BehaviorType.Wander:
                currentAction = StartCoroutine(Wander());
                break;

            case BehaviorType.Check:
                if (SearchTarget != null)
                    currentAction = StartCoroutine(GoTo(SearchTarget, true));
                break;

            case BehaviorType.Chase:
                currentAction = StartCoroutine(Chase());
                break;
        }

        SetFOV(currentBehaviorData.fovType);
    }

    public void SetBehaviorData(BehaviorData data)
    {
        currentBehaviorData = data;
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

    IEnumerator Wander()
    {
        IsActive = true;

        Move();

        yield return null;

        while (isMoving)
            yield return null;

        yield return LookAround();

        IsActive = false;
    }

    IEnumerator LookAround()
    {
        var waitTime = Random.Range(this.waitTime.min, this.waitTime.max);
        var randomRot = Quaternion.Euler(new Vector3(0f, 0f, Random.Range(25f, 180f) * Mathf.Sign(Random.Range(-1f, 1f))));
        var currentRot = transform.rotation;
        var targetRot = randomRot * currentRot;
        var lookSpeed = Random.Range(this.lookSpeed.min, this.lookSpeed.max);
        var lookCurrent = 0f;

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
            }

            waitTime -= Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator Chase()
    {
        IsActive = true;

        ShouldRun = true;
        var currentTargetCell = GameManager.player.CurrentCell;
        Move(currentTargetCell);

        yield return null;

        while (isMoving)
        {
            if (currentTargetCell != GameManager.player.CurrentCell)
            {
                currentTargetCell = GameManager.player.CurrentCell;
                Move(currentTargetCell);
            }

            yield return null;
        }

        IsActive = false;
    }

    IEnumerator GoTo(MazeCell cell, bool shouldRun = false)
    {
        IsActive = true;

        ShouldRun = shouldRun;

        Move(cell);

        yield return null;

        while (isMoving)
            yield return null;

        IsActive = false;
    }

    #endregion

    #region Trackers

    IEnumerator TrackAlertStatus()
    {
        while (true)
        {
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

                if(!CanSeePlayer)
                    IsAlert = false;
            }

            yield return null;
        }
    }

    #endregion
}
