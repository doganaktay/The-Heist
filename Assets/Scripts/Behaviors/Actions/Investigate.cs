using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;

public class Investigate : Node
{
    private AI owner;

    public Investigate(AI owner)
    {
        this.owner = owner;
        Name = "Investigate";
    }

    protected override void OnReset() { }

    protected override NodeStatus OnRun()
    {
        if (EvaluationCount == 0)
        {
            //owner.SetBehaviorData(new BehaviorData(BehaviorType.Investigate, FOVType.Alert));
            return NodeStatus.Running;
        }

        return NodeStatus.Success;
    }

    //IEnumerator InvestigateRoutine(MazeCell center)
    //{
    //    owner.IsActive = true;

    //    var possiblePositions = Propagation.instance.Propagate(center, 100000, 10);
    //    var pathToPos = PathRequestManager.RequestPathImmediate(owner.currentCell, searchTarget.target);
    //    var pathToPlayer = PathRequestManager.RequestPathImmediate(searchTarget.target, GameManager.player.CurrentCell);

    //    var final = new List<MazeCell>();

    //    foreach(var pos in possiblePositions)
    //    {
    //        if (!pathToPos.Contains(pos) && pathToPlayer.Contains(pos))
    //            final.Add(pos);
    //    }

    //    Debug.Log($"{gameObject.name} will investigate {final.Count} positions.");

    //    while (IsAlert)
    //    {
    //        if (final.Count == 0)
    //        {
    //            Debug.Log("No position to investigate");
    //            break;
    //        }

    //        var dest = final[Random.Range(0, final.Count)];

    //        Debug.Log($"Investigating {dest.gameObject.name}");

    //        yield return GoTo(dest, false, true);
    //    }

    //    yield return null;

    //    IsActive = false;
    //}
}
