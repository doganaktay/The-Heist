using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
	public TouchUI touchUI;
	public Trajectory trajectory;
	public PhysicsSim physicsSim;
	public Lights lights;
	public Pathfinder pathfinder;
	public AreaFinder areafinder;
	public Spotfinder spotfinder;
	public PatrolManager patrolManager;
	public TextOverlay textOverlay;
	public Player playerPrefab;
	public static Player player;
	public PathFollow aiPrefab;
	public PathFollow ai;
	public Maze mazePrefab;
	private Maze mazeInstance;
	public GameObject layout;

	private void Start()
	{
		BeginGame();
	}

    private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Alpha0))
		{
			RestartGame();
		}
	}

	public static Action MazeGenFinished;

	private void BeginGame()
	{
		// generate maze
		mazeInstance = Instantiate(mazePrefab);
		mazeInstance.Generate();

		if(mazeInstance.cellScaleY > mazeInstance.cellScaleX)
        {
			var newPos = mazeInstance.transform.position;
			newPos.x -= (mazeInstance.cellScaleY - mazeInstance.cellScaleX) * mazeInstance.cells.GetLength(0) / 2f;
			mazeInstance.transform.position = newPos;
        }
		else if (mazeInstance.cellScaleX > mazeInstance.cellScaleY)
        {
			var newPos = mazeInstance.transform.position;
			newPos.y -= (mazeInstance.cellScaleX - mazeInstance.cellScaleY) * mazeInstance.cells.GetLength(1) / 2f;
			mazeInstance.transform.position = newPos;
        }

        // pass references to pathfinder
        pathfinder.maze = mazeInstance;
		pathfinder.areafinder = areafinder;
		pathfinder.initialized = false;
		pathfinder.startPos = new IntVector2(0, 0);
		pathfinder.endPos = new IntVector2(mazeInstance.size.x - 1, mazeInstance.size.y - 1);
		pathfinder.aStar = new AStar(mazeInstance);

		// pass references to areafinder
        areafinder.maze = mazeInstance;
		areafinder.simulation = physicsSim;
		areafinder.pathfinder = pathfinder;

		// pass references to spotfinder
		spotfinder.maze = mazeInstance;
		spotfinder.simulation = physicsSim;
		spotfinder.pathfinder = pathfinder;
		spotfinder.areafinder = areafinder;
		spotfinder.layout = layout;

		// pass references to patrol manager
		patrolManager.pathfinder = pathfinder;
		patrolManager.areafinder = areafinder;

		// initialize and pass references to text overlay
		textOverlay.maze = mazeInstance;
		textOverlay.InitializeDisplay();

		// instantiate and pass references to player
		player = Instantiate(playerPrefab, new Vector3(mazeInstance.cells[0, 0].transform.position.x,
										   mazeInstance.cells[0, 0].transform.position.y, -3.5f), Quaternion.identity);
		player.maze = mazeInstance;
		player.pathfinder = pathfinder;
		player.simulation = physicsSim;
		player.trajectory = trajectory;
		player.touchUI = touchUI;

		// pass another reference to pathfinder
		pathfinder.player = player;

		// pass references to AI
		//ai.maze = mazeInstance;
		//ai.pathfinder = pathfinder;
		//ai.startPos = new IntVector2(0, 0);
		//ai.endPos = new IntVector2(mazeInstance.size.x - 1, mazeInstance.size.y - 1);

		// pass references to PhysicsSimulation
		physicsSim.maze = mazeInstance;
		physicsSim.playerPrefab = playerPrefab;
		physicsSim.player = player;
		physicsSim.trajectory = trajectory;

		// set up areas, placeable slots and simulation
		ConstructLayout();

		// directional light reset
		lights.StartRotation();

		// call event
		MazeGenFinished();
    }

	private void ConstructLayout()
    {
		pathfinder.NewPath();
		areafinder.FindAreas();
		physicsSim.ConstructSimulationScene();
		areafinder.MakeRooms();
		spotfinder.DeterminePlacement();
		areafinder.FindAreas();
		areafinder.DropWalls();
		//physicsSim.AddLayout(); // enable to include placement in physics sim
		pathfinder.NewPath();
		areafinder.FindAreas();
	}

	private void RestartGame()
	{		
		Destroy(mazeInstance.gameObject);
		Destroy(player.gameObject);

		ClearLayout();

		//ai.StopAndDestroy();

		// clearing material cache on projectiles
		// not strictly necessary, but if random friction values start getting used
		// uncomment if there's a danger of the dictionary growing too large
		//Projectile.ClearMaterialCache();

        BeginGame();
	}

	void ClearLayout()
    {
		for (int i=0; i < layout.transform.childCount; i++)
        {
			var t = layout.transform.GetChild(i);

			if (t == layout.transform)
				continue;

			Destroy(t.gameObject);
        }
    }
}