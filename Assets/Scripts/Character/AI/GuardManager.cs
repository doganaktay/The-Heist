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
        int guardCounter = 0;

        var indices = graphFinder.RequestPriorityIndices(IndexPriority.Critical);

        if(indices.Count > 0)
        {
            foreach(var index in indices)
            {
                var ai = CreateNewAI(GraphFinder.GetRandomCellFromGraphArea(index));
                Guard guard = (Guard)ai;
                guard.role = GuardRole.Station;

                ai.assignedIndices.Add(index);

                assignedAreas.Add(ai, new HashSet<int> {index});

                foreach(var area in graphFinder.weightedGraphAreas)
                    if(area.Key == index)
                    {
                        currentCoverage += area.Value;
                        break;
                    }

                guardCounter++;
            }
        }

        indices = graphFinder.RequestPriorityIndices(IndexPriority.High);

        var areas = graphFinder.GetMatchingIsolatedAreas(indices);
        var loops = graphFinder.GetMatchingLoops(indices);

        var coveredIndices = new List<int>();

        string test = "High priority isolated: ";
        Debug.Log(test);

        foreach (var area in areas)
        {
            //var ai = CreateNewAI(GraphFinder.GetRandomCellFromGraphArea(index));
            //Guard guard = (Guard)ai;
            //guard.role = GuardRole.Cover;

            //ai.assignedIndices.AddRange(area.area);

            //assignedAreas.Add(ai, new HashSet<int> { index });

            //foreach (var area in graphFinder.weightedGraphAreas)
            //    if (area.Key == index)
            //    {
            //        currentCoverage += area.Value;
            //        break;
            //    }

            //maxCount--;

            test = "area: ";
            foreach (var index in area.area)
                test += index + "- ";

            test += "match count: " + area.count;

            Debug.Log(test);
        }

        test = "High priority loop: ";
        Debug.Log(test);

        foreach(var loop in loops)
        {
            loop.loop.DebugPath();

            Debug.Log("match count: " + loop.count);
        }


        if (guardCounter <= 0)
            return;





        CreateNewAI(guardCounter);
    }




    protected override void AssignRoles() { }
}
