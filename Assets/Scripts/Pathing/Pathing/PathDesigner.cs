using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathDesigner : MonoBehaviour
{
    [HideInInspector]
    public static PathDesigner Instance;
    [SerializeField]
    GraphFinder graph;

    private void Awake()
    {
        if(Instance == null)
            Instance = this;
        else
        {
            Debug.LogError("Trying to create a second instance of path designer. Destroying game object");
            Destroy(gameObject);
        }
    }
}
