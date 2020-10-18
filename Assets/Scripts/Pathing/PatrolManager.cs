using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatrolManager : MonoBehaviour
{
    public Pathfinder pathfinder;
    public AreaFinder areafinder;
    public Patrol patrolPrefab;
    public List<Patrol> patrols = new List<Patrol>();

    public int patrolCount = 3;

    void Start()
    {
        GameManager.MazeGenFinished += ResetPatrols;
    }

    public void CreateNewPatrol()
    {
        for (int i = 0; i < patrolCount; i++)
        {
            var randomCell = areafinder.WalkableArea[Random.Range(0, areafinder.WalkableArea.Count)];
            var patrol = Instantiate(patrolPrefab, randomCell.transform.position, Quaternion.identity);

            patrol.name = "Patrol " + (patrols.Count + i);
            patrol.pathfinder = pathfinder;
            patrol.currentCell = randomCell;
            patrol.transform.parent = transform;

            patrols.Add(patrol);
        }
    }

    void ResetPatrols()
    {
        foreach (var patrol in patrols)
        {
            //patrol.StopAndDestroy();
            if(patrol != null)
                Destroy(patrol.gameObject);
        }
    }

    void OnDestroy()
    {
        GameManager.MazeGenFinished -= ResetPatrols;
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 80, 60), "New Patrol"))
            CreateNewPatrol();
    }

}
