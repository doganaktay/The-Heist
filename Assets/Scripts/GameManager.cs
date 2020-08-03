using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
	public Pathfinder pathfinder;
    public AreaFinder areafinder;
    public PatrolManager patrolManager;
	public Player playerPrefab;
	public Player player;
	public PathFollow aiPrefab;
	public PathFollow ai;
	public Maze mazePrefab;
	private Maze mazeInstance;

	public event Action Restart;
	public event Action MazeGenFinished;

	private void Start()
	{
		BeginGame();
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Alpha1))
		{
			RestartGame();

			if(Restart != null)
				Restart();
		}
	}

	private void BeginGame()
	{
		mazeInstance = Instantiate(mazePrefab);
		mazeInstance.Generate();
		pathfinder.maze = mazeInstance;
		pathfinder.areafinder = areafinder;
        areafinder.maze = mazeInstance;

		player = Instantiate(playerPrefab, new Vector3(transform.position.x, transform.position.y, -2f), Quaternion.identity);
		ai = Instantiate(aiPrefab, new Vector3(mazeInstance.cells[0, 0].transform.position.x,
							  mazeInstance.cells[0, 0].transform.position.y, -1f), Quaternion.identity);

		ai.maze = mazeInstance;
		ai.pathfinder = pathfinder;
		ai.startPos = new IntVector2(0, 0);
		ai.endPos = new IntVector2(mazeInstance.size.x - 1, mazeInstance.size.y - 1);
		ai.NewPath();

		MazeGenFinished();
	}

	private void RestartGame()
	{		
		Destroy(mazeInstance.gameObject);
		Destroy(player.gameObject);
		ai.FlagAndDestroy();

		BeginGame();
	}
}