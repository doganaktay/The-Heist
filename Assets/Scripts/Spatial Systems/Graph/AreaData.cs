using System.Collections.Generic;
using UnityEngine;

public class AreaData
{
    public List<MazeCell> all;
    public List<MazeCell> ends;
    public List<MazeCell> placement;
    public float weight;
    public float placementScore;
    public float weightedScore;
    public List<KeyValuePair<float, MazeCell>> sortedVantagePoints;
    public List<Loot> loot;
    public List<CCTVCamera> cameras;
    public bool isMapEntranceOrExit = false;
    public bool hasDeadEnd = false;
    public bool isIsolated = false;
    public IndexPriority priority = IndexPriority.None;

    // possible future additions
    // security system type
    // door with key

    // utility


    public AreaData(List<MazeCell> all, List<MazeCell> ends)
    {
        this.all = all;
        this.ends = ends;

        foreach (var end in ends)
            if (end.IsDeadEnd)
            {
                hasDeadEnd = true;
                break;
            }

        placement = new List<MazeCell>();
        float score = 0;

        foreach (var cell in all)
            foreach (var placed in cell.placedConnectedCells)
                if (!placement.Contains(placed))
                {
                    placement.Add(placed);
                    score += placed.PlacementScore;
                }

        if (placement.Count > 0)
            placementScore = score / placement.Count;
    }

    public AreaData(List<MazeCell> all, List<MazeCell> ends, float weight)
    {
        this.all = all;
        this.ends = ends;
        this.weight = weight;

        foreach (var end in ends)
            if (end.IsDeadEnd)
            {
                hasDeadEnd = true;
                break;
            }

        placement = new List<MazeCell>();
        float score = 0;

        foreach (var cell in all)
            foreach (var placed in cell.placedConnectedCells)
                if (!placement.Contains(placed))
                {
                    placement.Add(placed);
                    score += placed.PlacementScore;
                }

        if (placement.Count > 0)
            placementScore = score / placement.Count;
    }

    // weighted score is tampered with junction count
    // so that large areas with many junctions
    // pay a penalty
    public void CalculateWeightedScore() => weightedScore = placementScore * (weight / (ends.Count == 1 ? 1 : (hasDeadEnd ? ends.Count - 1 : ends.Count)));

    public void SetWeight(float weight) => this.weight = weight;
    public void SetLoot(List<Loot> loot) => this.loot = loot;
    public void SetVantagePoints(List<KeyValuePair<float, MazeCell>> vantagePoints) => sortedVantagePoints = vantagePoints;
    public void SetIsEntranceOrExit(bool isEntranceOrExit) => isMapEntranceOrExit = isEntranceOrExit;
    public void SetCameras(List<CCTVCamera> cameras) => this.cameras = cameras;
    public void SetIsIsolated(bool isIsolated) => this.isIsolated = isIsolated;
    public void SetPriority(IndexPriority priority) => this.priority = priority;

    public void AddCamera(CCTVCamera camera)
    {
        if (cameras == null)
            cameras = new List<CCTVCamera>();

        if (!cameras.Contains(camera))
            cameras.Add(camera);
    }

    public void FindAndSetPlacement()
    {
        placement = new List<MazeCell>();

        foreach (var cell in all)
            foreach (var placed in cell.placedConnectedCells)
                if (!placement.Contains(placed))
                    placement.Add(placed);

        if (placement.Count == 0)
            return;

        SetPlacementScore();
    }

    void SetPlacementScore()
    {
        float score = 0;

        foreach (var cell in placement)
            score += cell.PlacementScore;

        placementScore = score / placement.Count;
    }

    public MazeCell GetVantagePoint(float normalizedParameter)
    {
        var range = GameManager.GetScaledRange(normalizedParameter);
        var final = Mathf.Min(1, GameManager.rngFree.Range(range.min, range.max));
        var index = Mathf.RoundToInt((sortedVantagePoints.Count - 1) * final);

        return sortedVantagePoints[index].Value;
    }

    public MazeCell GetTopPlacement()
    {
        float max = float.MinValue;
        MazeCell found = null;

        foreach(var cell in placement)
        {
            var score = cell.GetPlacementScore();
            if(score > max)
            {
                max = score;
                found = cell;
            }
        }

        return found;
    }
}