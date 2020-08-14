using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
	public Pathfinder pathfinder;
	public AStar aStar;
    public AreaFinder areafinder;
    public PatrolManager patrolManager;
	public Player playerPrefab;
	public Player player;
	public PathFollow aiPrefab;
	public PathFollow ai;
	public Maze mazePrefab;
	private Maze mazeInstance;

	public static event Action MazeGenFinished;

	private void Start()
	{
		BeginGame();
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Alpha1))
		{
			RestartGame();
		}
	}

	private void BeginGame()
	{
		// generate maze
		mazeInstance = Instantiate(mazePrefab);
		mazeInstance.Generate();

		// pass references to pathfinder
		pathfinder.maze = mazeInstance;
		pathfinder.areafinder = areafinder;
		pathfinder.initialized = false;

		// pass references to AStar
		aStar.maze = mazeInstance;
		aStar.pathfinder = pathfinder;
		aStar.areafinder = areafinder;

		// pass references to areafinder
        areafinder.maze = mazeInstance;

		// pass references to patrol manager
		patrolManager.pathfinder = pathfinder;
		patrolManager.areafinder = areafinder;

		// instantiate player & AI
		player = Instantiate(playerPrefab, new Vector3(transform.position.x, transform.position.y, -2f), Quaternion.identity);
		ai = Instantiate(aiPrefab, new Vector3(mazeInstance.cells[0, 0].transform.position.x,
							  mazeInstance.cells[0, 0].transform.position.y, -1f), Quaternion.identity);

		// pass another reference to pathfinder
		pathfinder.player = player;

		// pass references to AI
		ai.maze = mazeInstance;
		ai.pathfinder = pathfinder;
		ai.startPos = new IntVector2(0, 0);
		ai.endPos = new IntVector2(mazeInstance.size.x - 1, mazeInstance.size.y - 1);

		// call event
		MazeGenFinished();
	}

	private void RestartGame()
	{		
		Destroy(mazeInstance.gameObject);
		Destroy(player.gameObject);
        ai.StopAndDestroy();

        BeginGame();
	}
}