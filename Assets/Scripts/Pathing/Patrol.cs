using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Patrol : MonoBehaviour
{
    public Pathfinder pathfinder;
    bool patrolling;
    [SerializeField]
    float maxSpeed = 10f;
    float speed;
    [SerializeField]
    float bufferDistance = 0.01f;
    [SerializeField]
    float maxWaitTime = 3f;

    List<MazeCell> patrolPath = new List<MazeCell>();
    public MazeCell currentCell;
    MazeCell nextCell;

    Coroutine patrol;

    void Start()
    {
        speed = Random.Range(1f, maxSpeed);
        patrolPath = pathfinder.GetRandomAStarPath(currentCell);
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
        while(i < path.Count)
        {
            nextCell = path[i];
            transform.position = Vector2.MoveTowards(transform.position, nextCell.transform.position, speed * Time.deltaTime);

            if(((Vector2)nextCell.transform.position - (Vector2)transform.position).magnitude < bufferDistance)
            {
                currentCell = nextCell;
                i++;
            }

            yield return null;
        }

        yield return new WaitForSeconds(Random.Range(0f, maxWaitTime));

        patrolling = false;
    }
}
