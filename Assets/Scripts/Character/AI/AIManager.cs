using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AIManager : MonoBehaviour
{
    public AI aiPrefab;
    [SerializeField]
    protected string aiTypeName;

    [HideInInspector]
    public AreaFinder areafinder;
    [HideInInspector]
    public GraphFinder graphFinder;
    [HideInInspector]
    public List<AI> activeAIs = new List<AI>();
    public int ActiveAICount => activeAIs.Count;

    void Start()
    {
        GameManager.MazeGenFinished += InitializeAI;
    }

    public void CreateNewAI(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var randomCell = areafinder.WalkableArea[Random.Range(0, areafinder.WalkableArea.Count)];
            var ai = Instantiate(aiPrefab, new Vector3(randomCell.transform.position.x, randomCell.transform.position.y, -1f), Quaternion.identity);

            ai.name = aiTypeName + " " + activeAIs.Count;
            ai.transform.parent = transform;
            activeAIs.Add(ai);
        }
    }

    public void CreateNewAI(MazeCell cell, int count = 1)
    {
        for (int i = 0; i < count; i++)
        {
            var ai = Instantiate(aiPrefab, new Vector3(cell.transform.position.x, cell.transform.position.y, -1f), Quaternion.identity);

            ai.name = aiTypeName + " " + activeAIs.Count;
            ai.transform.parent = transform;
            activeAIs.Add(ai);
        }
    }

    public AI CreateNewAI(MazeCell cell)
    {
        var ai = Instantiate(aiPrefab, new Vector3(cell.transform.position.x, cell.transform.position.y, -1f), Quaternion.identity);

        ai.name = aiTypeName + " " + activeAIs.Count;
        ai.transform.parent = transform;
        activeAIs.Add(ai);

        return ai;
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

        activeAIs.Clear();
    }

    public void InitializeAI()
    {
        ResetAI();
        OnInitializeAI();
    }

    void OnDestroy()
    {
        GameManager.MazeGenFinished -= InitializeAI;
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 80, 60), $"New {aiTypeName}"))
            CreateNewAI(1);
    }

    protected abstract void AssignRoles();
    protected abstract void OnInitializeAI();
}
