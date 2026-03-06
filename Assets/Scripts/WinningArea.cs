using UnityEngine;
using System.Collections;

public class WinningArea : MonoBehaviour
{
	public Color requiredColor;
	private static int totalBalls = -1;
	private static int ballsRemaining = 0;
	private static bool levelCompleted = false;

	void Start()
	{
		if (totalBalls == -1)
		{
			GameObject[] balls = GameObject.FindGameObjectsWithTag("Ball");
			totalBalls = balls.Length;
			ballsRemaining = totalBalls;
			levelCompleted = false;
		}
	}

	void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Ball"))
		{
			SwipeBall swipeBall = other.GetComponent<SwipeBall>();
			if (swipeBall != null && ColorsMatch(swipeBall.ballColor, requiredColor))
			{
				swipeBall.StartEnteringHole(transform.position);

				ballsRemaining--;

				if (ballsRemaining <= 0 && !levelCompleted)
				{
					levelCompleted = true;
					StartCoroutine(OnLevelComplete());
				}
			}
		}
	}

	bool ColorsMatch(Color a, Color b)
	{
		float tolerance = 0.01f;
		return Mathf.Abs(a.r - b.r) < tolerance &&
		       Mathf.Abs(a.g - b.g) < tolerance &&
		       Mathf.Abs(a.b - b.b) < tolerance;
	}

	IEnumerator OnLevelComplete()
	{
		yield return new WaitForSeconds(1.5f);

		if (GameStateManager.instance != null)
		{
			if (GameStateManager.instance.currentLevel >= GameStateManager.MAX_LEVEL)
			{
				GameStateManager.instance.StopPlaying();
			}
			else
			{
				int nextLevel = GameStateManager.instance.currentLevel + 1;
				GameStateManager.instance.UnlockLevel(nextLevel);
				GameStateManager.instance.LoadLevel(nextLevel);
			}
		}
	}

	public static void ResetBallCount()
	{
		totalBalls = -1;
		ballsRemaining = 0;
		levelCompleted = false;
	}
}
