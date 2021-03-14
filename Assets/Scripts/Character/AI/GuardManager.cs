using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GuardRole
{
    Free,
    Loop,
    Cover,
    Station
}

public class GuardManager : AIManager
{
    [Header("Settings")]
    [SerializeField]
    MinMaxData guardCount;
    [SerializeField, Tooltip("Used to determine area coverage by taking area weights into account")]
    MinMaxData coveragePercent;
    float currentCoverage;

    Dictionary<AI, HashSet<int>> assignedAreas = new Dictionary<AI, HashSet<int>>();
    Dictionary<AI, ChartedPath> assignedLoops = new Dictionary<AI, ChartedPath>();

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

                ai.assignedIndices.Add(index);

                assignedAreas.Add(ai, new HashSet<int> {index});

                Debug.Log($"critical index: {index}");

                foreach(var area in graphFinder.weightedGraphAreas)
                    if(area.Key == index)
                    {
                        currentCoverage += area.Value;
                        break;
                    }

                maxCount--;
            }
        }

        indices = graphFinder.RequestPriorityIndices(IndexPriority.High);

        var areas = graphFinder.GetMatchingIsolatedAreas(indices);
        var loops = graphFinder.GetMatchingLoops(indices);

        string test = "High priority isolated: ";
        Debug.Log(test);

        foreach (var area in areas)
        {
            test = "area: ";
            foreach (var index in indices)
                test += index + "- ";

            Debug.Log(test);
        }

        test = "High priority loop: ";
        Debug.Log(test);

        foreach(var loop in loops)
        {
            loop.DebugPath();
        }


        if (maxCount <= 0)
            return;





        CreateNewAI(maxCount);
    }




    protected override void AssignRoles() { }
}
