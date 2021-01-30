using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;

public class HasSearchTarget : Condition
{
    private AI owner;

    public HasSearchTarget(AI owner) : base($"Has search target?")
    {
        this.owner = owner;
    }

    protected override void OnReset() { }

    protected override NodeStatus OnRun() => owner.HasSearchTarget && !owner.SearchTarget.isOld ? NodeStatus.Success : NodeStatus.Failure;

}