using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
	public Pathfinder pathfinder;
	public Maze mazePrefab;
	private Maze mazeInstance;

	private void Start()
	{
		BeginGame();
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Space))
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