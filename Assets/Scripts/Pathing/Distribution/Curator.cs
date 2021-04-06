using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Curator : MonoBehaviour
{
    public Maze maze;
    public Pathfinder pathfinder;
    public AreaFinder areafinder;
    public GraphFinder graphfinder;
    public Spotfinder spotfinder;

    //private void OnEnable()
    //{
    //    GameManager.MazeGenFinished += AssignRandomPriorities;
    //}

    //private void OnDisable()
    //{
    //    GameManager.MazeGenFinished -= AssignRandomPriorities;
    //}

    int maxAttempt = 100;
    public void AssignRandomPriorities()
    {
        var assigned = new List<int>();

        for(int i = 0; i < 10; i++)
        {
            int attempt = 0;
            var randomIndex = GraphFinder.FinalGraphIndices[Random.Range(0, GraphFinder.FinalGraphIndices.Count)];

            while(assigned.Contains(randomIndex) && attempt < maxAttempt)
            {
                randomIndex = GraphFinder.FinalGraphIndices[Random.Range(0, GraphFinder.FinalGraphIndices.Count)];
                attempt++;
            }

            if (attempt >= maxAttempt)
                continue;

            var priority = Random.value < 0.1f ? IndexPriority.Critical : IndexPriority.High;
            graphfinder.RegisterPriorityIndex(randomIndex, priority);

            assigned.Add(randomIndex);

            //Debug.Log($"Asssigning {priority.ToString()} to index {randomIndex}");
        }
    }
}
