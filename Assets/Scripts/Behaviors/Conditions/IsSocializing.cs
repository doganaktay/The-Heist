using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;

public class IsSocializing : Condition
{
    private AI owner;

    public IsSocializing(AI owner) : base($"Is Socializing?")
    {
        this.owner = owner;
    }

    protected override void OnReset() { }

    protected override NodeStatus OnRun()
    {
        return owner.IsSocializing ? NodeStatus.Success : NodeStatus.Failure;
    }
}