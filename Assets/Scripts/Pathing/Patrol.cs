using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Patrol : MonoBehaviour
{
    public Pathfinder pathfinder;
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
    public MazeCell currentCell;
    MazeCell nextCell;

    Coroutine patrol;

    void Start()
    {
        speed = Random.Range(minSpeed, maxSpeed);
        patrolPath = pathfinder.GetRandomAStarPath(currentCell);
        transform.LookAt2D(patrolPath[1].transform);
        patrol = StartCoroutine(DoPatrol(patrolPath));
    }

    void Update()
    {
        if (!patrolling)
        {
            patrolPath = pathfinder.GetRandomAStarPath(currentCell);
            patrol = StartCoroutine(DoPatrol(patrolPath));
        }
    }

    IEnumerator DoPatrol(List<MazeCell> path)
    {
        patrolling = true;

        // starting at 1 because index 0 is the cell it is already on
        int i = 1;

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
            var t = ratio < 1f ? ratio : 1f;

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
