﻿using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Archi.Touch;
using Archi.IO;

public class GameManager : MonoBehaviour
{
	//[Header("Grid Setup")]

	[SerializeField] int gridSizeX;
	[SerializeField] int gridSizeY;
	[SerializeField] float cellSizeX;
	[SerializeField] float cellSizeY;
	public static float CellDiagonal;
	public static int CellCount;

	//[Header("Parameter scaling")]

	[SerializeField] float parameterDeviation;
	public static float ParameterDeviation { get; private set; }
	[SerializeField] MinMaxData biasMultipliers;
	public static MinMaxData BiasMultipliers { get; private set; }

	// RNG
	//[Header("RNG")]

	public bool reuseSeed = true;
	public bool seedFromSave = false;
	List<uint> savedSeeds;

	public uint seed = 1;
	public static RNG rngFree;
	public static RNG rngSeeded;

	//[Header("References")]

	public DControl touchControl;
	public Trajectory trajectory;
	public PhysicsSim physicsSim;
	public Lights lights;
	public Pathfinder pathfinder;
	public AreaFinder areafinder;
	public Spotfinder spotfinder;
	public GraphFinder graphFinder;
	public Propagation propagationModule;
	public GuardManager guardManager;
	public Curator curator;
	public CCTV cctv;
	public TextOverlay textOverlay;
	public Player playerPrefab;
	public static Player player;
	public Maze mazePrefab;
	private Maze mazeInstance;
	public GameObject layout, spawnedObjects, cctvHolder;

	private static MazeCell startCell, endCell;
	public static MazeCell StartCell => startCell;
	public static MazeCell EndCell => endCell;

	public static Action MazeGenFinished;

    private void Awake()
    {
		InitRNG();
    }

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


	private void BeginGame()
	{
		// generate maze
		mazeInstance = Instantiate(mazePrefab);
		mazeInstance.size.x = gridSizeX;
		mazeInstance.size.y = gridSizeY;
		mazeInstance.cellScaleX = cellSizeX;
		mazeInstance.cellScaleY = cellSizeY;
		mazeInstance.Generate();

		// retrieve four corner cells
		var possibleCorners = new List<IntVector2>();
		possibleCorners.Add(new IntVector2(0, 0));
		possibleCorners.Add(new IntVector2(0, mazeInstance.size.y - 1));
		possibleCorners.Add(new IntVector2(mazeInstance.size.x - 1, 0));
		possibleCorners.Add(new IntVector2(mazeInstance.size.x - 1, mazeInstance.size.y - 1));
		possibleCorners.Shuffle(true);
		var selectedStart = possibleCorners[0];
		var selectedEnd = possibleCorners[1];

		startCell = mazeInstance.cells[selectedStart.x, selectedStart.y];
		endCell = mazeInstance.cells[selectedEnd.x, selectedEnd.y];

		// set cell diagonal distance
		CellDiagonal = Mathf.Sqrt(cellSizeX * cellSizeX + cellSizeY * cellSizeY);

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

		// set static getters for parameter deviation and bias multipliers
		ParameterDeviation = parameterDeviation;
		BiasMultipliers = biasMultipliers;

		// set cell count static reference
		CellCount = gridSizeX * gridSizeY;

        // pass references to pathfinder
        pathfinder.maze = mazeInstance;
		pathfinder.areafinder = areafinder;
		pathfinder.initialized = false;
		pathfinder.startPos = selectedStart;
		pathfinder.endPos = selectedEnd;
		pathfinder.aStar = new AStar(mazeInstance);
		pathfinder.guardManager = guardManager;

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

		// pass references to corridor finder
		graphFinder.maze = mazeInstance;
		graphFinder.spotfinder = spotfinder;

		// pass references to curator
		curator.maze = mazeInstance;
		curator.pathfinder = pathfinder;
		curator.areafinder = areafinder;
		curator.graphfinder = graphFinder;
		curator.spotfinder = spotfinder;

		// pass references to propagation calculator
		Propagation.maze = mazeInstance;

		// initialize and pass references to notification module
		NotificationModule.Create(mazeInstance);

		// pass references to patrol manager		
		guardManager.areafinder = areafinder;
		guardManager.graphFinder = graphFinder;

		// pass references to CCTV
		cctv.maze = mazeInstance;
		cctv.simulation = physicsSim;

		// initialize and pass references to text overlay
		textOverlay.maze = mazeInstance;
		textOverlay.InitializeDisplay();

		// instantiate and pass references to player
		player = Instantiate(playerPrefab, new Vector3(mazeInstance.cells[selectedStart.x, selectedStart.y].transform.position.x,
										   mazeInstance.cells[selectedStart.x, selectedStart.y].transform.position.y, -3.5f), Quaternion.identity);
		player.maze = mazeInstance;
		player.pathfinder = pathfinder;
		player.simulation = physicsSim;
		player.trajectory = trajectory;
		player.spawnedObjectHolder = spawnedObjects;

		// pass another reference to pathfinder
		pathfinder.player = player;

		// pass references to PhysicsSimulation
		physicsSim.maze = mazeInstance;
		physicsSim.playerPrefab = playerPrefab;
		physicsSim.player = player;
		physicsSim.trajectory = trajectory;

		// pass data to TouchUI.instance
		TouchUI.instance.gameManager = this;
		var mazeHeight = cellSizeY * gridSizeY / (cellSizeX * gridSizeX / 16f * 9f);
		TouchUI.instance.topMenuHeight = (1 - mazeHeight) * Screen.height;
		TouchUI.instance.mainMenuHeight = mazeHeight * Screen.height;

		// pass references to touchControl
		touchControl.player = player;

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
		propagationModule.BuildConnectivityGrid();
		areafinder.SetPassableWalls();
		graphFinder.Initialize();

		// for testing
		curator.AssignRandomPriorities();

		//guardManager.InitializeAI();
    }

	public void RestartGame()
	{		
		Destroy(mazeInstance.gameObject);
		Destroy(player.gameObject);

		layout.ClearChildren();
		spawnedObjects.ClearChildren();
		cctvHolder.ClearChildren(1);

		InitRNG();
		BeginGame();
	}

	#region RNG

	void InitRNG()
    {
        rngFree = new RNG((uint)DateTime.Now.Ticks);

		if(rngSeeded == null)
        {
            if (reuseSeed)
            {
				rngSeeded = new RNG(seed);
			}
			else if (!seedFromSave)
            {
				GenerateSeed();
				rngSeeded = new RNG(seed);
            }
            else
            {
				LoadSeeds();
				rngSeeded = new RNG(savedSeeds[rngFree.Next(savedSeeds.Count)]);
			}
        }
		else if (reuseSeed)
        {
			rngSeeded.ResetSeed(seed);
        }
		else if (seedFromSave)
        {
			LoadSeeds();
			rngSeeded = new RNG(savedSeeds[rngFree.Next(savedSeeds.Count)]);
        }
        else
        {
			GenerateSeed();
			rngSeeded = new RNG(seed);
        }
    }

	#endregion

	#region Utility

	public static MinMaxData GetScaledRange(float param, bool canBeNegative = false)
    {
		var min = param - (param * ParameterDeviation * BiasMultipliers.min);
		var max = param + (param * ParameterDeviation * BiasMultipliers.max);

		return canBeNegative ? new MinMaxData(min, max) : new MinMaxData(Mathf.Max(0,min), max);
	}

	public void GenerateSeed() => seed = new RNG((uint)DateTime.Now.Ticks).NextUint();

	#endregion

	#region Editor Only

#if UNITY_EDITOR

	private const string SEED_SAVE_PATH = "seeds.txt";

	public void SaveSeed(uint seed)
    {
		SaveSystem.SaveDirect(SEED_SAVE_PATH, seed.ToString(), true);
    }

	public void LoadSeeds()
    {
		var list = SaveSystem.LoadDirect(SEED_SAVE_PATH);
		var results = new List<uint>();

		foreach(var s in list)
        {
			if(uint.TryParse(s, out uint result))
				results.Add(result);
        }

		savedSeeds = results;
    }

#endif

	#endregion
}