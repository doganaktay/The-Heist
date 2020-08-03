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
			Restart();
		}
	}

	private void BeginGame()
	{
		mazeInstance = Instantiate(mazePrefab);
		mazeInstance.Generate();
		pathfinder.maze = mazeInstance;
        areafinder.maze = mazeInstance;
		aiPrefab.maze = mazeInstance;

		player = Instantiate(playerPrefab, new Vector3(transform.position.x, transform.position.y, -2f), Quaternion.identity);
		ai = Instantiate(aiPrefab, new Vector3(mazeInstance.cells[0, 0].transform.position.x,
							  mazeInstance.cells[0, 0].transform.position.y, -1f), Quaternion.identity);
		MazeGenFinished();
	}

	private void RestartGame()
	{		
		Destroy(mazeInstance.gameObject);
		Destroy(player.gameObject);
		Destroy(ai.gameObject);

		BeginGame();
	}
}