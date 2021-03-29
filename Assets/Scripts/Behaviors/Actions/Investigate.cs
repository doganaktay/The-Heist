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

        if(Random.value > 0.5f)
            yield return owner.LookAround();

        var possiblePositions = Propagation.instance.Propagate(owner.CurrentCell, 100000, 10);

        while (owner.IsAlert)
        {
            var dest = possiblePositions[Random.Range(0, possiblePositions.Count)];
            owner.ShouldRun = Random.value > 0.5f ? true : false;

            yield return owner.GoTo(dest.cell);

            if (Random.value > 0.2f)
                yield return owner.LookAround(Random.Range(0, 4f));
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
