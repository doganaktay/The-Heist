using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;

public class HasPursuitPath : Condition
{
    private AI owner;
    private MazeCell searchStart;

    public HasPursuitPath(AI owner) : base($"Has Pursuit?")
    {
        this.owner = owner;
    }

    protected override void OnReset() { }

    protected override NodeStatus OnRun()
    {
        if (owner.ReadyForPursuit)
        {
            owner.ReadyForPursuit = false;

            searchStart = owner.PlayerObservationPoint;
            owner.pursuit = PathDesigner.Instance.GetPursuitPath(owner, owner.CurrentCell, searchStart);

            return NodeStatus.Success;
        }
        else if (owner.pursuit.cells != null && owner.pursuit.cells.Length > 0)
        {
            return NodeStatus.Success;
        }

        return NodeStatus.Failure;
    }
}