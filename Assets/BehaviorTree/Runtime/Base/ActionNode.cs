using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;

public abstract class ActionNode : Node
{
    protected AI owner;

    protected override void OnReset() { }

    protected override NodeStatus OnRun()
    {
        if (EvaluationCount == 0 && ShouldAssignAction())
        {
            owner.SetBehavior(Action(), this);
            return NodeStatus.Running;
        }

        return NodeStatus.Success;
    }

    protected abstract bool ShouldAssignAction();
    protected abstract IEnumerator Action();

    protected bool IsCurrentAction() => owner.IsActiveNode(this) && owner.CurrentAction != null;
}
