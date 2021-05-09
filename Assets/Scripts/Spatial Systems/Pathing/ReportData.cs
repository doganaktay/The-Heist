using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct ReportData
{
    public AI owner;
    public List<int> sightingIndices;
    public bool sightingIsJunction;
    public MazeCell observationPoint;
    public MazeCell destination;

    public ReportData(AI owner, MazeCell observationPoint, MazeCell destination = null)
    {
        this.owner = owner;
        this.observationPoint = observationPoint;
        sightingIndices = observationPoint.GetGraphAreaIndices();
        sightingIsJunction = sightingIndices.Count > 1 ? true : false;

        this.destination = destination;
    }
}
