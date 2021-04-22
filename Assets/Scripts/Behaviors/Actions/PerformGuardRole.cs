using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;
using System.Threading;
using Cysharp.Threading.Tasks;

public class PerformGuardRole : ActionNode
{
    public PerformGuardRole(AI owner)
    {
        this.owner = owner;
        Name = "Perform Guard Role";
    }

    protected async override UniTask Action(CancellationToken token)
    {
        owner.IsActive = true;

        var guard = (Guard)owner;

        switch (guard.role)
        {
            case GuardRole.Free:
                owner.SetBehaviorParams(BehaviorType.Casual, FOVType.Regular, false);

                owner.Move();

                await UniTask.NextFrame(token);

                while (owner.IsMoving && !token.IsCancellationRequested)
                    await UniTask.NextFrame(token);

                await owner.LookAround(token);

                break;

            case GuardRole.Loop:
                owner.SetBehaviorParams(BehaviorType.Casual, FOVType.Regular, false);
                var path = owner.GetPath(ChartedPathType.Loop);

                (MazeCell cell, int index) next;    
                next = path.GetNext(owner.CurrentCell, true); 

                while (next.cell != null && !token.IsCancellationRequested)
                {
                    if (next.index == -1)
                        await owner.GoTo(token, next.cell);
                    else
                        await owner.GoTo(token, next.cell, next.index);

                    next = path.GetNext(owner.CurrentCell);

                    if (GameManager.rngFree.Roll(0.5f))
                        await owner.LookAround(token);
                }

                break;

            case GuardRole.Cover:
                owner.SetBehaviorParams(BehaviorType.Casual, FOVType.Regular, false);

                var nextIndex = owner.assignedIndices[GameManager.rngFree.Range(0, owner.assignedIndices.Count)];
                var nextCell = GraphFinder.GetRandomCellFromGraphArea(nextIndex);

                while (nextCell != null && !token.IsCancellationRequested)
                {
                    await owner.GoTo(token, nextCell);

                    await owner.LookAround(token);

                    nextIndex = owner.assignedIndices[GameManager.rngFree.Range(0, owner.assignedIndices.Count)];
                    nextCell = GraphFinder.GetRandomCellFromGraphArea(nextIndex);
                }

                break;

            case GuardRole.Station:
                owner.SetBehaviorParams(BehaviorType.Casual, FOVType.Regular, false);

                nextCell = GraphFinder.GetRandomCellFromGraphArea(owner.assignedIndices[0]);

                while (nextCell != null && !token.IsCancellationRequested)
                {
                    await owner.GoTo(token, nextCell);

                    await owner.LookAround(token);

                    nextIndex = owner.assignedIndices[GameManager.rngFree.Range(0, owner.assignedIndices.Count)];
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
