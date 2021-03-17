using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;

public class PerformGuardRole : ActionNode
{
    public PerformGuardRole(AI owner)
    {
        this.owner = owner;
        Name = "Perform Guard Role";
    }

    protected override IEnumerator Action()
    {
        owner.ActiveActionNode = this;
        owner.IsActive = true;

        var guard = (Guard)owner;

        switch (guard.role)
        {
            case GuardRole.Free:
                owner.SetBehaviorParams(BehaviorType.Casual, FOVType.Regular, false);

                owner.Move();

                yield return null;

                while (owner.IsMoving)
                    yield return null;

                yield return owner.LookAround();

                break;

            case GuardRole.Loop:
                owner.SetBehaviorParams(BehaviorType.Casual, FOVType.Regular, false);
                var path = owner.GetPath(ChartedPathType.Loop);

                (MazeCell cell, int index) next;    
                next = path.GetNext(owner.CurrentCell, true); 

                while (next.cell != null)
                {
                    if (next.index == -1)
                        yield return owner.GoTo(next.cell);
                    else
                        yield return owner.GoTo(next.cell, next.index);

                    next = path.GetNext(owner.CurrentCell);
                }

                break;

            case GuardRole.Cover:
                owner.SetBehaviorParams(BehaviorType.Casual, FOVType.Regular, false);

                var nextIndex = owner.assignedIndices[Random.Range(0, owner.assignedIndices.Count)];
                var nextCell = GraphFinder.GetRandomCellFromGraphArea(nextIndex);

                while (nextCell != null)
                {
                    yield return owner.GoTo(nextCell);

                    yield return owner.LookAround();

                    nextIndex = owner.assignedIndices[Random.Range(0, owner.assignedIndices.Count)];
                    nextCell = GraphFinder.GetRandomCellFromGraphArea(nextIndex);
                }

                break;

            case GuardRole.Station:
                owner.SetBehaviorParams(BehaviorType.Casual, FOVType.Regular, false);

                nextCell = GraphFinder.GetRandomCellFromGraphArea(owner.assignedIndices[0]);

                while (nextCell != null)
                {
                    yield return owner.GoTo(nextCell);

                    yield return owner.LookAround();

                    nextIndex = owner.assignedIndices[Random.Range(0, owner.assignedIndices.Count)];
                    nextCell = GraphFinder.GetRandomCellFromGraphArea(nextIndex);
                }

                break;
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
