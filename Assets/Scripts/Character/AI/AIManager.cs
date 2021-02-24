using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AIManager : MonoBehaviour
{
    public AI aiPrefab;
    [SerializeField]
    protected string aiTypeName;
    public int aiCount = 3;

    [HideInInspector]
    public AreaFinder areafinder;
    [HideInInspector]
    public List<AI> activeAIs = new List<AI>();

    void Start()
    {
        GameManager.MazeGenFinished += ResetAI;
    }

    public void CreateNewAI()
    {
        for (int i = 0; i < aiCount; i++)
        {
            var randomCell = areafinder.WalkableArea[Random.Range(0, areafinder.WalkableArea.Count)];
            var ai = Instantiate(aiPrefab, new Vector3(randomCell.transform.position.x, randomCell.transform.position.y, -1f), Quaternion.identity);

            ai.name = aiTypeName + " " + (activeAIs.Count + i);
            ai.transform.parent = transform;

            ai.manager = this;

            activeAIs.Add(ai);
        }
    }

    public void CreateNewAI(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var randomCell = areafinder.WalkableArea[Random.Range(0, areafinder.WalkableArea.Count)];
            var ai = Instantiate(aiPrefab, new Vector3(randomCell.transform.position.x, randomCell.transform.position.y, -1f), Quaternion.identity);

            ai.name = aiTypeName + " " + (activeAIs.Count + i);
            ai.transform.parent = transform;
            activeAIs.Add(ai);
        }
    }

    public bool ProximityCheck(AI aiToCheck)
    {
        foreach (var active in activeAIs)
        {
            if (active == aiToCheck)
                continue;

            if ((active.transform.position - aiToCheck.transform.position).sqrMagnitude < aiToCheck.AwarenessDistance * aiToCheck.AwarenessDistance)
                return true;
        }

        return false;
    }

    void ResetAI()
    {
        foreach (var ai in activeAIs)
        {
            if (ai != null)
                Destroy(ai.gameObject);
        }
    }

    void OnDestroy()
    {
        GameManager.MazeGenFinished -= ResetAI;
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 80, 60), $"New {aiTypeName}"))
            CreateNewAI();
    }

    protected abstract void AssignRoles();
}
