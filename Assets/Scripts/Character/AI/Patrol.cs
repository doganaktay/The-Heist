using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;

public class Patrol : AI
{
    protected override void GenerateBehaviorTree()
    {
        BehaviorTree = new Selector($"{gameObject.name} AI Tree",
                            new Sequence("Chase Player",
                                new CanSeePlayer(this),
                                new Chase(this)),
                            new Sequence("Handle Alert",
                                new IsAlert(this),
                                new HasSearchTarget(this),
                                new Check(this)),
                            new Selector($"{gameObject.name} Wander",
                                new Wander(this)));
    }
}
