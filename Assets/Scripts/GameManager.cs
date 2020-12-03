using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Archi.Touch;

public class GameManager : MonoBehaviour
{
	[Header("Setup")]
	[SerializeField] int gridSizeX;
	[SerializeField] int gridSizeY;
	[SerializeField] float cellSizeX;
	[SerializeField] float cellSizeY;

	[Header("References")]
	public DControl touchControl;
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
	public Maze mazePrefab;
	private Maze mazeInstance;
	public GameObject layout, spawnedObjects;

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
		mazeInstance.size.x = gridSizeX;
		mazeInstance.size.y = gridSizeY;
		mazeInstance.cellScaleX = cellSizeX;
		mazeInstance.cellScaleY = cellSizeY;
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

		// pass references to dispersion calculator
		Dispersion.maze = mazeInstance;

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
		player.spawnedObjectHolder = spawnedObjects;

		// pass another reference to pathfinder
		pathfinder.player = player;

		// pass references to PhysicsSimulation
		physicsSim.maze = mazeInstance;
		physicsSim.playerPrefab = playerPrefab;
		physicsSim.player = player;
		physicsSim.trajectory = trajectory;

		// pass data to touchUI
		touchUI.gameManager = this;
		var mazeHeight = cellSizeY * gridSizeY / (cellSizeX * gridSizeX / 16f * 9f);
		touchUI.topMenuHeight = (1 - mazeHeight) * Screen.height;
		touchUI.mainMenuHeight = mazeHeight * Screen.height;

		// pass references to touchControl
		touchControl.player = player;
		touchControl.touchUI = touchUI;

		// set up areas, placeable slots and simulation
		ConstructScene();

		// directional light reset
		lights.StartRotation();

		// call event
		MazeGenFinished();
    }

	private void ConstructScene()
    {
		pathfinder.NewPath();
		areafinder.FindAreas();
		physicsSim.ConstructSimulationScene();
		areafinder.MakeRooms();
		spotfinder.DeterminePlacement();
		areafinder.FindAreas();
		areafinder.DropWalls(true);
		//physicsSim.AddLayout(); // enable to include placement in physics sim
		pathfinder.NewPath();
		areafinder.FindAreas();
	}

	public void RestartGame()
	{		
		Destroy(mazeInstance.gameObject);
		Destroy(player.gameObject);

		ClearObjectHolder(layout);
		ClearObjectHolder(spawnedObjects);

        BeginGame();
	}

	void ClearObjectHolder(GameObject holder)
    {
		for (int i=0; i < holder.transform.childCount; i++)
        {
			var t = holder.transform.GetChild(i);
			Destroy(t.gameObject);
        }
    }
}