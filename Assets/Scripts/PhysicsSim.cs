using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PhysicsSim : MonoBehaviour
{
    public Maze maze;
    public GameObject player;

    GameObject sceneHolder;
    PhysicsScene2D simulationPhysics;
    Scene simulation;

    Dictionary<GameObject, GameObject> objectPairs = new Dictionary<GameObject, GameObject>();

    void Awake()
    {
        GameManager.MazeGenFinished += ConstructSimulationScene;
    }

    void Start()
    {
        //create simulation scene
        CreateSceneParameters csp = new CreateSceneParameters(LocalPhysicsMode.Physics2D);
        simulation = SceneManager.CreateScene("Physics Sim", csp);
        simulationPhysics = simulation.GetPhysicsScene2D();
    }

    void ConstructSimulationScene()
    {
        if (sceneHolder != null)
            Destroy(sceneHolder);

        sceneHolder = new GameObject();
        sceneHolder.name = "Holder";
        SceneManager.MoveGameObjectToScene(sceneHolder, simulation);

        var playerCopy = Instantiate(player);
        SceneManager.MoveGameObjectToScene(playerCopy, simulation);
        playerCopy.transform.position = player.transform.position;
        playerCopy.transform.rotation = player.transform.rotation;
        playerCopy.transform.localScale = player.transform.localScale;
        playerCopy.transform.parent = sceneHolder.transform;

        // destroy children unnecessary for simulation
        Destroy(playerCopy.transform.GetChild(2).gameObject);
        Destroy(playerCopy.transform.GetChild(1).gameObject);
        Destroy(playerCopy.transform.GetChild(0).gameObject);

        playerCopy.GetComponent<Renderer>().enabled = false;

        objectPairs[player] = playerCopy;

        foreach(var wall in maze.wallsInScene)
        {
            var wallCopy = Instantiate(wall);
            SceneManager.MoveGameObjectToScene(wallCopy, simulation);
            wallCopy.transform.position = wall.transform.position;
            wallCopy.transform.rotation = wall.transform.rotation;
            var scale = new Vector3(maze.cellScaleX, maze.cellScaleY, 1f);
            wallCopy.transform.localScale = scale;
            wallCopy.transform.parent = sceneHolder.transform;

            Destroy(wallCopy.transform.GetChild(1).gameObject);

            foreach (var r in wallCopy.GetComponentsInChildren<Renderer>())
            {
                r.enabled = false;
            }

            objectPairs[wall] = wallCopy;
        }
    }

    public void RemoveWallFromSimulation(GameObject wall)
    {
        Destroy(objectPairs[wall]);
    }

    void OnDestroy()
    {
        GameManager.MazeGenFinished -= ConstructSimulationScene;
    }
}
