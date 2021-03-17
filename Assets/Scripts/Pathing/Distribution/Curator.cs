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

    public void AssignRandomPriorities()
    {
        for(int i = 0; i < 5; i++)
        {
            var randomIndex = GraphFinder.FinalGraphIndices[Random.Range(0, GraphFinder.FinalGraphIndices.Count)];
            var priority = (IndexPriority)Random.Range(0, 2);
            graphfinder.RegisterPriorityIndex(randomIndex, priority);

            Debug.Log($"Asssigning {priority.ToString()} to index {randomIndex}");
        }
    }
}
