using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISimulateable
{
    GameObject Instance { get; }
    int SyncTransformIndex { get; }
    bool IsDynamic { get; set; }
    bool IsDestructible { get; set; }
}
