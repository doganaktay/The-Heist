using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;

public class Wander : ActionNode
{
    public Wander(AI owner)
    {
        this.owner = owner;
        Name = "Wander";
    }

    protected override IEnumerator Action()
    {
        owner.ActiveActionNode = this;
        owner.IsActive = true;

        owner.SetBehaviorParams(BehaviorType.Casual, FOVType.Regular, false);

        owner.Move();

        yield return null;

        while (owner.IsMoving)
            yield return null;

        yield return owner.LookAround();

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
