using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;

public class Investigate : ActionNode
{
    public Investigate(AI owner)
    {
        this.owner = owner;
        Name = "Investigate";
    }

    protected override IEnumerator Action()
    {
        owner.ActiveActionNode = this;
        owner.IsActive = true;

        owner.SetBehaviorParams(BehaviorType.Investigate, FOVType.Alert, false);

        var possiblePositions = Propagation.instance.Propagate(owner.CurrentCell, 100000, 10);

        while (owner.IsAlert)
        {
            var dest = possiblePositions[Random.Range(0, possiblePositions.Count)];
            owner.ShouldRun = false;

            yield return owner.GoTo(dest, true);
        }

        yield return null;

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
