using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Curator : MonoBehaviour
{
    [HideInInspector] public Maze maze;
    [HideInInspector] public Pathfinder pathfinder;
    [HideInInspector] public AreaFinder areafinder;
    [HideInInspector] public GraphFinder graphfinder;
    [HideInInspector] public Spotfinder spotfinder;
    public Loot lootPrefab;
    [SerializeField] int maxCritAreaCount = 2;
    [SerializeField] int maxHighAreaCount = 5;
    int currentCritAreaCount;
    int currentHighAreaCount;
    List<Loot> placedLoot = new List<Loot>();

    //private void OnEnable()
    //{
    //    GameManager.MazeGenFinished += AssignRandomPriorities;
    //}

    //private void OnDisable()
    //{
    //    GameManager.MazeGenFinished -= AssignRandomPriorities;
    //}

    public void AssignPriorities()
    {
        currentCritAreaCount = maxCritAreaCount;
        currentHighAreaCount = maxHighAreaCount;

        var topAreas = GraphFinder.GetAreasSorted(SortType.WeightedScore, true);
        for(int i = 0; i < topAreas.Count; i++)
        {
            //if (i == 0)
            //{
            //    var topPosition = topAreas[i].Value.GetTopPlacement();
            //    var loot = Instantiate(lootPrefab, topPosition.transform.position, Quaternion.identity);
            //    loot.gameObject.name = "Loot";
            //    loot.transform.parent = transform;
            //    placedLoot.Add(loot);
            //}

            var area = topAreas[i];

            if (area.Value.isMapEntranceOrExit)
                continue;

            if (currentCritAreaCount > 0)
            {
                graphfinder.RegisterPriorityIndex(area.Key, IndexPriority.Critical);
                currentCritAreaCount--;
            }
            else
                break;
        }

        var topIsolated = GraphFinder.GetIsolatedAreasSorted(SortType.WeightedScore, true);
        for(int i = 0; i < topIsolated.Count; i++)
        {
            var isolated = topIsolated[i];

            bool isAllowed = true;
            foreach(var index in isolated.Key)
                if (GraphFinder.Areas[index].isMapEntranceOrExit)
                {
                    isAllowed = false;
                    break;
                }

            if (!isAllowed)
                break;

            if (isolated.Key.Count > currentHighAreaCount)
                continue;
            else
            {
                foreach(var index in isolated.Key)
                {
                    if(GraphFinder.Areas[index].priority < IndexPriority.High)
                    {
                        graphfinder.RegisterPriorityIndex(index, IndexPriority.High);
                    }

                    if (currentHighAreaCount-- <= 0)
                        break;
                }
            }

            if (currentHighAreaCount <= 0)
                break;
        }
    }
}
