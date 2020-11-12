using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Archi.Touch;

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
    [HideInInspector]
    public int currentStepCount = 0;

    Projectile projectileCopy;
    Player playerCopy;
    Rigidbody2D playerCopyRb;
    Transform playerCopyTransform;
    Rigidbody2D projectileCopyRb;

    GameObject sceneHolder;
    PhysicsScene2D simulationPhysics;
    Scene simulation;

    Dictionary<GameObject, GameObject> objectPairs = new Dictionary<GameObject, GameObject>();

    void Start()
    {
        //create simulation scene
        CreateSceneParameters csp = new CreateSceneParameters(LocalPhysicsMode.Physics2D);
        simulation = SceneManager.CreateScene("Physics Sim", csp);
        simulationPhysics = simulation.GetPhysicsScene2D();
    }

    void Update()
    {
        playerCopyTransform.position = player.transform.position;
    }

    public void ConstructSimulationScene()
    {
        if (sceneHolder != null)
            Destroy(sceneHolder);

        // create an empty holder object for easy destruction of all simulated objects prior to reconstruction
        sceneHolder = new GameObject();
        sceneHolder.name = "Holder";
        SceneManager.MoveGameObjectToScene(sceneHolder, simulation);

        playerCopy = Instantiate(playerPrefab);

        // disable scripts
        Destroy(playerCopy.GetComponent<DControl>());
        Destroy(playerCopy.GetComponent<Player>());

        // add player to simulation
        SceneManager.MoveGameObjectToScene(playerCopy.gameObject, simulation);
        playerCopyTransform = playerCopy.transform;
        playerCopyTransform.position = player.transform.position;
        playerCopyTransform.rotation = player.transform.rotation;
        playerCopyTransform.localScale = player.transform.localScale;
        playerCopyTransform.parent = sceneHolder.transform;
        playerCopy.name = "Player Copy";

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
        projectileCopy.simulation = this;

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

    public void AddLayout()
    {
        // add placement to simulation
        foreach (var placement in maze.placementInScene)
        {
            var holderCopy = Instantiate(maze.placeHolderPrefab);
            SceneManager.MoveGameObjectToScene(holderCopy.gameObject, simulation);
            holderCopy.transform.position = placement.transform.position;
            holderCopy.transform.rotation = placement.transform.rotation;
            var scale = new Vector3(maze.cellScaleX, maze.cellScaleY, 1f);
            holderCopy.transform.localScale = scale;
            holderCopy.transform.parent = sceneHolder.transform;

            holderCopy.name = placement.name + " Copy";

            //Destroy(holderCopy.transform.GetChild(1).gameObject);

            foreach (var r in holderCopy.GetComponentsInChildren<Renderer>())
            {
                r.enabled = false;
            }

            objectPairs.Add(placement.gameObject, holderCopy.gameObject);
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
        projectileCopy.Launch(so, dir, pos, spin);

        for (; currentStepCount < simulationStepCount; currentStepCount++)
        {
            simulationPhysics.Simulate(Time.fixedDeltaTime);

            if (projectileCopy.bounceCount <= 0)
                break;
        }

        currentStepCount = 0;
    }
}
