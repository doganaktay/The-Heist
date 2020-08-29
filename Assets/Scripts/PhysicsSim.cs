using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PhysicsSim : MonoBehaviour
{
    [HideInInspector]
    public Maze maze;
    [HideInInspector]
    public Trajectory trajectory;
    [HideInInspector]
    public Player playerPrefab;
    [HideInInspector]
    public Player player;

    public int simulationStepCount = 1000;

    Projectile projectileCopy;
    Player playerCopy;
    Rigidbody2D playerCopyRb;
    Rigidbody2D projectileCopyRb;

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

    void FixedUpdate()
    {
        playerCopy.transform.position = player.transform.position;
    }

    void ConstructSimulationScene()
    {
        if (sceneHolder != null)
            Destroy(sceneHolder);

        // create an empty holder object for easy destruction of all simulated objects prior to reconstruction
        sceneHolder = new GameObject();
        sceneHolder.name = "Holder";
        SceneManager.MoveGameObjectToScene(sceneHolder, simulation);

        // add player to simulation
        playerCopy = Instantiate(playerPrefab);
        SceneManager.MoveGameObjectToScene(playerCopy.gameObject, simulation);
        playerCopy.transform.position = player.transform.position;
        playerCopy.transform.rotation = player.transform.rotation;
        playerCopy.transform.localScale = player.transform.localScale;
        playerCopy.transform.parent = sceneHolder.transform;
        playerCopy.name = "Player Copy";

        // disable script
        playerCopy.GetComponent<Player>().enabled = false;

        // cache and set up simulation rigidbody
        playerCopyRb = playerCopy.GetComponent<Rigidbody2D>();
        playerCopyRb.isKinematic = true;
        playerCopyRb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;

        // destroy children unnecessary for simulation
        Destroy(playerCopy.transform.GetChild(2).gameObject);
        Destroy(playerCopy.transform.GetChild(1).gameObject);
        Destroy(playerCopy.transform.GetChild(0).gameObject);

        playerCopy.GetComponent<Renderer>().enabled = false;

        objectPairs.Add(player.gameObject, playerCopy.gameObject);

        // add dummy projectile to simulation
        projectileCopy = Instantiate(player.projectilePrefab);
        SceneManager.MoveGameObjectToScene(projectileCopy.gameObject, simulation);
        projectileCopy.GetComponent<Renderer>().enabled = false;

        projectileCopyRb = projectileCopy.GetComponent<Rigidbody2D>();

        // place dummy outside game boundaries to not interfere with simulation until it is needed
        projectileCopy.transform.position = new Vector3(5000, 5000, 0);
        projectileCopy.transform.parent = sceneHolder.transform;
        projectileCopy.name = "Simulated Projectile";

        projectileCopy.isSimulated = true;
        projectileCopy.trajectory = trajectory;

        // add walls to simulation
        foreach (var wall in maze.wallsInScene)
        {
            var wallCopy = Instantiate(maze.wallPrefab);
            SceneManager.MoveGameObjectToScene(wallCopy.gameObject, simulation);
            wallCopy.transform.position = wall.transform.position;
            wallCopy.transform.rotation = wall.transform.rotation;
            var scale = new Vector3(maze.cellScaleX, maze.cellScaleY, 1f);
            wallCopy.transform.localScale = scale;
            wallCopy.transform.parent = sceneHolder.transform;

            wallCopy.name = wall.name + " Copy";

            Destroy(wallCopy.transform.GetChild(1).gameObject);

            foreach (var r in wallCopy.GetComponentsInChildren<Renderer>())
            {
                r.enabled = false;
            }

            objectPairs.Add(wall.gameObject, wallCopy.gameObject);
        }
    }

    public void RemoveWallFromSimulation(GameObject wall)
    {
        Destroy(objectPairs[wall]);
        objectPairs.Remove(wall);
    }

    public void SimulateProjectile(ProjectileSO so, Vector2 dir, Vector3 pos, float spin = 0)
    {
        projectileCopyRb.WakeUp();
        projectileCopy.Launch(so, playerCopy.transform, dir, pos, spin);

        for (int i=0; i < simulationStepCount; i++)
        {
            simulationPhysics.Simulate(Time.fixedDeltaTime);

            if(projectileCopy.bounceCount < 0)
                break;
        }
    }

    void OnDestroy()
    {
        GameManager.MazeGenFinished -= ConstructSimulationScene;
    }
}
