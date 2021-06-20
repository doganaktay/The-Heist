using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IsolatedAreaData
{
    public float weight;
    public float placementScore;
    public float weightedScore;
    public HashSet<MazeCell> entryPoints;

    public IsolatedAreaData(float weight, HashSet<MazeCell> entryPoints)
    {
        this.weight = weight;
        this.entryPoints = entryPoints;
    }

    public void CalculatePlacementScore(HashSet<int> indices)
    {
        float score = 0;
        int count = 0;

        // this ignores the overlapping cells
        // between indices when counting
        foreach(var index in indices)
        {
            var area = GraphFinder.Areas[index];
            score += area.placementScore;
            count += area.all.Count;
        }

        placementScore = score / count;
    }

    public void CalculateWeightedScore(HashSet<int> indices)
    {
        float score = 0;

        foreach (var index in indices)
            score += GraphFinder.Areas[index].weightedScore;

        weightedScore = score / indices.Count;
    }
}