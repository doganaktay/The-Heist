using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;

public class IsPosting : Condition
{
    private AI owner;

    public IsPosting(AI owner) : base($"Is posting?")
    {
        this.owner = owner;
    }

    protected override void OnReset() { }

    protected override NodeStatus OnRun()
    {
        return owner.IsPosting ? NodeStatus.Success : NodeStatus.Failure;
    }
}
