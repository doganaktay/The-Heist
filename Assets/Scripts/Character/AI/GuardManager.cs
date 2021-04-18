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
    float minCoverageThreshold = .20f;

    Dictionary<AI, HashSet<int>> assignedAreas = new Dictionary<AI, HashSet<int>>();
    Dictionary<AI, ChartedPath> assignedLoops = new Dictionary<AI, ChartedPath>();

    #region Initialize

    protected override void OnInitializeAI()
    {
        //AssignRoles();

        int guardCounter = 0;
        if (guardCounter < guardCount.max)
            CreateNewAI((int)guardCount.max - guardCounter);
    }

    protected override void AssignRoles()
    {
        int guardCounter = 0;

        var indices = graphFinder.RequestPriorityIndices(IndexPriority.Critical);

        if (indices.Count > 0)
        {
            foreach (var index in indices)
            {
                var ai = CreateNewAI(GraphFinder.GetRandomCellFromGraphArea(index));
                Guard guard = (Guard)ai;
                guard.role = GuardRole.Station;

                ai.assignedIndices.Add(index);
                assignedAreas.Add(ai, new HashSet<int> { index });

                foreach (var area in graphFinder.weightedGraphAreas)
                    if (area.Key == index)
                    {
                        currentCoverage += area.Value;
                        break;
                    }

                guardCounter++;

                Debug.Log($"{ai.gameObject.name} critical: {index}");
            }
        }

        indices = graphFinder.RequestPriorityIndices(IndexPriority.High);

        var areas = graphFinder.GetMatchingIsolatedAreas(indices);
        var coveredIndices = new HashSet<int>();

        for (int i = areas.Count - 1; i >= 0; i--)
        {
            bool covered = false;
            var current = areas[i].area;

            foreach (var index in coveredIndices)
                if (current.Contains(index))
                {
                    covered = true;
                    break;
                }

            if (covered)
                continue;

            foreach (var index in current)
            {
                coveredIndices.Add(index);
            }

            var ai = CreateNewAI(GraphFinder.GetRandomCellFromGraphArea(new List<int>(current)));
            Guard guard = (Guard)ai;
            guard.role = GuardRole.Cover;

            if (current.Count == 1)
            {
                foreach (var index in current)
                {
                    if (GraphFinder.GetGraphAreaWeight(index) < minCoverageThreshold)
                    {
                        var flood = graphFinder.GetFloodCoverage(index, Random.Range(0.1f, 0.2f));
                        ai.assignedIndices.AddRange(new List<int>(flood));
                        assignedAreas.Add(ai, flood);
                    }
                    else
                    {
                        ai.assignedIndices.AddRange(new List<int>(current));
                        assignedAreas.Add(ai, current);
                    }
                }
            }
            else
            {
                ai.assignedIndices.AddRange(new List<int>(current));
                assignedAreas.Add(ai, current);
            }

            foreach (var weighted in graphFinder.weightedGraphAreas)
                foreach (var index in current)
                    if (weighted.Key == index)
                    {
                        currentCoverage += weighted.Value;
                        break;
                    }

            guardCounter++;

            Debug.Log($"{ai.gameObject.name} cover: {current.Debug()}");

        }

        var loops = graphFinder.GetMatchingLoops(indices);
        coveredIndices.Clear();

        for (int i = loops.Count - 1; i >= 0; i--)
        {
            bool covered = false;
            var current = loops[i].loop;

            foreach (var index in coveredIndices)
                if (current.Contains(index))
                {
                    covered = true;
                    break;
                }

            if (covered)
                continue;

            for (int j = 0; j < current.indices.Length; j++)
                coveredIndices.Add(current.indices[j]);

            var ai = CreateNewAI(GraphFinder.GetRandomCellFromGraphArea(new List<int>(current.indices)));
            Guard guard = (Guard)ai;
            guard.role = GuardRole.Loop;

            assignedLoops.Add(ai, current);
            ai.SetPath(ChartedPathType.Loop, current);

            foreach (var weighted in graphFinder.weightedGraphAreas)
                for (int j = 0; j < current.indices.Length; j++)
                    if (weighted.Key == current.indices[j])
                    {
                        currentCoverage += weighted.Value;
                        break;
                    }

            guardCounter++;

            Debug.Log($"{ai.gameObject.name} loop");
            current.DebugPath();
        }
    }

    #endregion

    #region Social

    

    #endregion
}
