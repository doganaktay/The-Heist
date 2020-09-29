using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
	public Trajectory trajectory;
	public PhysicsSim physicsSim;
	public Lights lights;
	public Pathfinder pathfinder;
	public AStar aStar;
    public AreaFinder areafinder;
	public Spotfinder spotfinder;
    public PatrolManager patrolManager;
	public TextOverlay textOverlay;
	public Player playerPrefab;
	public Player player;
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

		// pass references to pathfinder
		pathfinder.maze = mazeInstance;
		pathfinder.areafinder = areafinder;
		pathfinder.initialized = false;
		pathfinder.startPos = new IntVector2(0, 0);
		pathfinder.endPos = new IntVector2(mazeInstance.size.x - 1, mazeInstance.size.y - 1);

		// pass references to AStar
		aStar.maze = mazeInstance;
		aStar.pathfinder = pathfinder;
		aStar.areafinder = areafinder;

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
		player.simulation = physicsSim;
		player.trajectory = trajectory;

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
		areafinder.DropWalls(0.1f);
		physicsSim.AddLayout();
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