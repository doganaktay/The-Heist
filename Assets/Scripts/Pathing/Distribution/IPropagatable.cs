using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPropagatable
{
    MazeCell Center { get; set; }
    List<MazeCell> AffectedCells { get; set; }
    float PropagationStrength { get; set; }
    float PropagationMinThreshold { get; set; }
    Color NotificationColor { get; set; }
    List<MazeCell> AcquirePropagationArea(MazeCell center, float strength, float minimum);
    
}
