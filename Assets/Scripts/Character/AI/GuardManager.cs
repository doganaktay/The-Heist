using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GuardRole
{
    Free,
    Loop,
    Coveraage,
    Station
}

public class GuardManager : AIManager
{
    [SerializeField]
    MinMaxData guardCount;
    [SerializeField, Range(0, 1f), Header("Used to determine area coverage by taking area weights into account")]
    float coveragePercent;

    protected override void OnInitializeAI()
    {
        int maxCount = (int)guardCount.max;

        var indices = graphFinder.RequestPriorityIndices(IndexPriority.Critical);

        if(indices.Count > 0)
        {
            foreach(var index in indices)
            {
                var ai = CreateNewAI(graphFinder.GetRandomCellFromGraphArea(index));
                Guard guard = (Guard)ai;
                guard.role = GuardRole.Station;

                maxCount--;
            }
        }

        indices = graphFinder.RequestPriorityIndices(IndexPriority.High);

        if(indices.Count > 0)
        {
            
        }

        if (maxCount <= 0)
            return;





        CreateNewAI(maxCount);
    }




    protected override void AssignRoles() { }
}
