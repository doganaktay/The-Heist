using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatrolManager : MonoBehaviour
{
    public Pathfinder pathfinder;
    public PathPatrol patrolPrefab;
    public PathPatrol[] patrols;
    public List<MazeCell> activePath = new List<MazeCell>();

    public int patrolCount = 3;
    public int maxPatrolCount = 20;

    void Start()
    {
        pathfinder = FindObjectOfType<Pathfinder>();
        patrols = new PathPatrol[maxPatrolCount];

    }

    public void CreateNewPatrol()
    {
        List<MazeCell> currentPath = pathfinder.GetCurrentPath();

        if (currentPath == null) return;

        for (int i = 0; i < patrolCount; i++)
        {
            if (patrolCount > currentPath.Count) return;

            MazeCell randomCell = currentPath[Random.Range(0, currentPath.Count)];

            currentPath.Remove(randomCell);
            patrols[i] = Instantiate(patrolPrefab, randomCell.transform.position, Quaternion.identity);
        }
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 80, 60), "New Patrol"))
            CreateNewPatrol();
    }

}
