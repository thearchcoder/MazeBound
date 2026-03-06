using UnityEngine;
using System.Collections;

public class WinningArea : MonoBehaviour
{
	public Color requiredColor;
	private static int totalBalls = -1;
	private static int ballsRemaining = 0;
	private static bool levelCompleted = false;

	private AudioSource audioSource;
	private AudioClip winSound;

	void Start()
	{
		audioSource = gameObject.AddComponent<AudioSource>();
		audioSource.playOnAwake = false;

		winSound = Resources.Load<AudioClip>("Sound/Win");
		if (winSound == null)
			Debug.LogWarning("Could not load Win sound from Resources/Sound/Win");

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
			BallColor ballColor = other.GetComponent<BallColor>();
			if (ballColor != null && ColorsMatch(ballColor.color, requiredColor))
			{
				if (audioSource != null && winSound != null && SettingsManager.instance != null)
				{
					audioSource.PlayOneShot(winSound, SettingsManager.instance.audioVolume);
				}

				SwipeBall swipeBall = other.GetComponent<SwipeBall>();
				if (swipeBall != null)
				{
					swipeBall.StartEnteringHole(transform.position);
				}

				ballsRemaining--;

				if (ballsRemaining <= 0 && !levelCompleted)
				{
					levelCompleted = true;
					StartCoroutine(OnLevelComplete());
				}
			}
		}
	}

	void DespawnAllBalls()
	{
		GameObject[] balls = GameObject.FindGameObjectsWithTag("Ball");
		foreach (GameObject ball in balls)
		{
			Destroy(ball);
		}
	}

	bool ColorsMatch(Color a, Color b)
	{
		// Check if colors are approximately equal (with small tolerance for floating point comparison)
		float tolerance = 0.01f;
		return Mathf.Abs(a.r - b.r) < tolerance &&
		       Mathf.Abs(a.g - b.g) < tolerance &&
		       Mathf.Abs(a.b - b.b) < tolerance;
	}

	IEnumerator OnLevelComplete()
	{
		Debug.Log("Level completed!");

		yield return new WaitForSeconds(1.5f);

		if (GameStateManager.instance != null)
		{
			GameStateManager.instance.NextLevel();
		}
	}

	public static void ResetBallCount()
	{
		totalBalls = -1;
		ballsRemaining = 0;
		levelCompleted = false;
	}
}
