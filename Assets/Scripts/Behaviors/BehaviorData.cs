using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct BehaviorData
{
    public BehaviorType type;
    public FOVType fovType;
    public bool isRepeating;

    public BehaviorData(BehaviorType type, FOVType fovType = FOVType.Regular, bool isRepeating = false)
    {
        this.type = type;
        this.fovType = fovType;
        this.isRepeating = isRepeating;
    }
}
