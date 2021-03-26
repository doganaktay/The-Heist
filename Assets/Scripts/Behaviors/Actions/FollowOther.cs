using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;

public class FollowOther : ActionNode
{
    AI target;

    public FollowOther(AI owner)
    {
        this.owner = owner;
        Name = "Follow Other";
    }

    protected override IEnumerator Action()
    {
        owner.ActiveActionNode = this;
        owner.IsActive = true;

        owner.SetBehaviorParams(BehaviorType.Follow, FOVType.Alert, true);
        owner.SetMaxExposureTime();

        var currentTargetCell = target.CurrentCell;
        owner.Move(currentTargetCell);

        yield return null;

        while (owner.IsMoving)
        {
            if (currentTargetCell != target.CurrentCell)
            {
                currentTargetCell = target.CurrentCell;
                owner.Move(currentTargetCell);
            }

            yield return null;
        }

        owner.IsActive = false;
        owner.ActiveActionNode = null;
    }

    protected override bool ShouldAssignAction()
    {
        if (IsCurrentAction())
        {


            return false;
        }

        return true;
    }
}
