using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

public abstract class ActionNode : Node
{
    protected AI owner;
    protected Func<CancellationToken, UniTask> action;

    protected override void OnReset() { }

    protected override NodeStatus OnRun()
    {
        if (EvaluationCount == 0 && ShouldAssignAction())
        {
            action = Action;

            owner.SetBehavior(this, action);
            return NodeStatus.Running;
        }

        return NodeStatus.Success;
    }

    protected abstract bool ShouldAssignAction();
    protected abstract UniTask Action(CancellationToken token);

    protected bool IsCurrentAction() => owner.IsActiveNode(this) && owner.IsActive;
}
