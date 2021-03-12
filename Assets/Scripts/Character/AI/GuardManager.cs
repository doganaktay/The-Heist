using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GuardRole
{
    Free,
    Patrol,
    VisitAreas,
    Station
}

public class GuardManager : AIManager
{
    [SerializeField]
    MinMaxData guardCount;
    int currentGuardCount;
    [SerializeField, Header("Used to distribute areas")]
    float coveragePerArea;

    protected override void OnInitializeAI()
    {
        currentGuardCount = Mathf.RoundToInt(Random.Range(guardCount.min, guardCount.max));

        var indices = graphFinder.RequestPriorityIndices(IndexPriority.Critical);

        if(indices.Count > 0)
        {
            foreach(var index in indices)
            {
                var ai = CreateNewAI(graphFinder.GetRandomCellFromGraphArea(index));
                Guard guard = (Guard)ai;
                guard.role = GuardRole.Station;

                currentGuardCount--;
                if (currentGuardCount <= 0)
                    break;
            }
        }

        indices = graphFinder.RequestPriorityIndices(IndexPriority.High);

        if(indices.Count > 0)
        {
            
        }



        CreateNewAI(currentGuardCount);
    }


    protected override void AssignRoles() { }
}
