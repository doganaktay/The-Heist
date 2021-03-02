using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;

public class Chase : ActionNode
{
    public Chase(AI owner)
    {
        this.owner = owner;
        Name = "Chase";
    }

    protected override IEnumerator Action()
    {
        owner.ActiveActionNode = this;
        owner.IsActive = true;

        owner.SetFOV(FOVType.Chase);
        owner.ShouldRun = true;
        var currentTargetCell = GameManager.player.CurrentCell;
        owner.SetPursuit(currentTargetCell);
        owner.Move(currentTargetCell);

        yield return null;

        while (owner.IsMoving)
        {
            if (currentTargetCell != GameManager.player.CurrentCell)
            {
                currentTargetCell = GameManager.player.CurrentCell;
                owner.Move(currentTargetCell);
                owner.SetPursuit(currentTargetCell);
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
