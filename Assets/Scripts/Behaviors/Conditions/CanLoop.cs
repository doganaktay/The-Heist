using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;

public class CanLoop : Condition
{
    private AI owner;

    public CanLoop(AI owner) : base($"Can loop map?")
    {
        this.owner = owner;
    }

    protected override void OnReset() { }

    protected override NodeStatus OnRun() => owner.CanLoopMap() ? NodeStatus.Success : NodeStatus.Failure;

}
