using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;

public class WillSocialize : Condition
{
    private AI owner;

    public WillSocialize(AI owner) : base($"Will Socialize?")
    {
        this.owner = owner;
    }

    protected override void OnReset() { }

    protected override NodeStatus OnRun()
    {
        if (owner.WillSocialize)
            return NodeStatus.Success;

        owner.CanSocialize = false;
        owner.WaitUntilCanSocialize().Forget();
        return NodeStatus.Failure;
    }
}