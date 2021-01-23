using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;

public enum BehaviorType
{
    Disabled = -1,
    Wander,
    Alert,
    Chase
}

public enum FOVType
{
    Regular,
    Alert,
    Chasing
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

    public bool CanSeePlayer { get => fieldOfView.CanSeePlayer(); }
    public bool IsAlert { get; private set; }
    public float AwarenessDistance { get => fieldOfView.viewRadius; }
    public bool IsActive { get; private set; }

    public NodeBase BehaviorTree { get ; set; }
    Coroutine behaviourTreeRoutine;
    YieldInstruction btWaitTime = new WaitForSeconds(.1f);
    BehaviorType currentBehaviorType;
    public BehaviorType newBehaviorType;

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
        if (currentBehaviorType != newBehaviorType)
        {
            currentBehaviorType = newBehaviorType;
            return true;
        }
        else
            return false;
    }

    void TakeBehaviorAction()
    {
        if (currentAction != null)
            StopCoroutine(currentAction);

        switch (currentBehaviorType)
        {
            case BehaviorType.Wander:
                currentAction = StartCoroutine(Wander());
                SetFOV(FOVType.Regular);
                AimOverride = false;
                break;

            case BehaviorType.Chase:
                currentAction = StartCoroutine(Chase());
                SetFOV(FOVType.Chasing);
                aimOverrideTarget = GameManager.player.transform;
                AimOverride = true;
                break;
        }
    }

    public void SetBehaviorType(BehaviorType type)
    {
        newBehaviorType = type;
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
        Debug.Log($"{gameObject.name} caught player!");
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
