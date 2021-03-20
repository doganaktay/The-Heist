using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;

public class RegisterPlayer : Condition
{
    private AI owner;

    public RegisterPlayer(AI owner) : base($"Register player?")
    {
        this.owner = owner;
    }

    protected override void OnReset() { }

    protected override NodeStatus OnRun()
    {
        return owner.RegisterPlayer ? NodeStatus.Success : NodeStatus.Failure;
    }

}
