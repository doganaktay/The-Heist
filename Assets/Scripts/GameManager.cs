using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
	public Pathfinder pathfinder;
	public PatrolManager patrolManager;
	public Player playerPrefab;
	public PathFollow aiPrefab;
	public Maze mazePrefab;
	private Maze mazeInstance;

	private void Start()
	{
		BeginGame();
		Instantiate(playerPrefab, new Vector3(transform.position.x, transform.position.y, -2f), Quaternion.identity);
		Instantiate(aiPrefab, new Vector3(mazeInstance.cells[0, 0].transform.position.x,
							  mazeInstance.cells[0, 0].transform.position.y, -1f), Quaternion.identity);
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
		mazeInstance = Instantiate(mazePrefab);
		mazeInstance.Generate();
		pathfinder.maze = mazeInstance;
	}

	private void RestartGame()
	{		
		Destroy(mazeInstance.gameObject);
		BeginGame();
	}
}