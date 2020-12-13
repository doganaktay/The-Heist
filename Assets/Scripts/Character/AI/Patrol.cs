using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Patrol : Character
{
    public Pathfinder pathfinder;
    private FieldOfView fieldOfView;
    bool patrolling;
    [SerializeField]
    Transform body;
    [SerializeField]
    float minSpeed = 2f;
    [SerializeField]
    float maxSpeed = 10f;
    float speed;
    [SerializeField]
    float turnSpeed = 1f;
    [SerializeField]
    float minLookSpeed = 2f;
    [SerializeField]
    float maxLookSpeed = 10f;
    [SerializeField]
    float bufferDistance = 0.01f;
    [SerializeField]
    float minWaitTime = 1f;
    [SerializeField]
    float maxWaitTime = 3f;

    List<MazeCell> patrolPath = new List<MazeCell>();
    MazeCell nextCell;

    Coroutine patrol;

    protected override void Start()
    {
        base.Start();

        fieldOfView = GetComponentInChildren<FieldOfView>();
        speed = Random.Range(minSpeed, maxSpeed);
    }

    protected override void Update()
    {
        base.Update();

        if (!patrolling)
        {
            PathRequestManager.RequestPath(new PathRequest(OnPathFound, PathLayer.Base, currentCell));
        }
    }

    public void OnPathFound(List<MazeCell> path)
    {
        if(patrol != null)
            StopCoroutine(patrol);

        transform.LookAt2D(path[1].transform);
        patrol = StartCoroutine(DoPatrol(path));
    }

    IEnumerator DoPatrol(List<MazeCell> path)
    {
        patrolling = true;

        // starting at 1 because index 0 is the cell it is already on
        int i = path[0] == currentCell ? 1 : 0;

        while (i < path.Count)
        {
            nextCell = path[i];
            transform.LookAt2D(nextCell.transform, turnSpeed);
            transform.position = Vector2.MoveTowards(transform.position, nextCell.transform.position, speed * Time.deltaTime);

            if(((Vector2)nextCell.transform.position - (Vector2)transform.position).magnitude < bufferDistance)
            {
                currentCell = nextCell;
                i++;
            }

            yield return null;
        }

        var waitTime = Random.Range(minWaitTime, maxWaitTime);
        var randomRot = Quaternion.Euler(new Vector3(0f, 0f, Random.Range(25f, 180f) * Mathf.Sign(Random.Range(-1f, 1f))));
        var currentRot = transform.rotation;
        var targetRot = randomRot * currentRot;
        var lookSpeed = Random.Range(minLookSpeed, maxLookSpeed);
        var lookCurrent = 0f;

        while(waitTime > 0)
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
                lookSpeed = Random.Range(minLookSpeed, maxLookSpeed);
                lookCurrent = 0f;
            }

            waitTime -= Time.deltaTime;
            yield return null;
        }

        patrolling = false;
    }
}
