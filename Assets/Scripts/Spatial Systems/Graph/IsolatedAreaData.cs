using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IsolatedAreaData
{
    public float weight;
    public float placementScore;
    public HashSet<MazeCell> entryPoints;

    public IsolatedAreaData(float weight, HashSet<MazeCell> entryPoints)
    {
        this.weight = weight;
        this.entryPoints = entryPoints;
    }

    public float GetWeightedScore() => weight * placementScore;

    public void CalculateIsolatedScore(HashSet<int> indices)
    {
        float score = 0;

        foreach (var index in indices)
            score += GraphFinder.Areas[index].placementScore;

        placementScore = score / indices.Count;
    }
}