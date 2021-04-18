using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;

public class ShouldPost : Condition
{
    private AI owner;

    public ShouldPost(AI owner) : base($"Should post?")
    {
        this.owner = owner;
    }

    protected override void OnReset() { }

    protected override NodeStatus OnRun()
    {
        if (owner.ShouldPost)
        {
            owner.ResetDistanceTravelled();
            owner.IsPosting = true;

            return NodeStatus.Success;
        }

        return NodeStatus.Failure;
    }
}
