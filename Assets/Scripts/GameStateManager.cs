using UnityEngine;

public class GameStateManager : MonoBehaviour
{
	public static GameStateManager instance;

	public const int MAX_LEVEL = 9;

	public bool isPlaying = false;
	public int currentLevel = 1;
	public int maxUnlockedLevel = 1;

	void Awake()
	{
		if (instance == null)
		{
			instance = this;
			DontDestroyOnLoad(gameObject);
			LoadProgress();
		}
		else
		{
			Destroy(gameObject);
		}
	}

	public void StartPlaying()
	{
		isPlaying = true;

		GameObject[] balls = GameObject.FindGameObjectsWithTag("Ball");
		foreach (GameObject ball in balls)
		{
			SwipeBall swipeBall = ball.GetComponent<SwipeBall>();
			if (swipeBall != null)
			{
				swipeBall.ResetPhysics();
			}
		}
	}

	public void StopPlaying()
	{
		isPlaying = false;
		ReloadCurrentLevel();
	}

	public void LoadLevel(int level)
	{
		currentLevel = level;
		isPlaying = false;

		if (MazeGenerator.instance != null)
		{
			MazeGenerator.instance.ReloadLevel(level);
		}
	}

	public void ReloadCurrentLevel()
	{
		if (MazeGenerator.instance != null)
		{
			MazeGenerator.instance.ReloadLevel(currentLevel);
		}
	}

	public void NextLevel()
	{
		if (currentLevel >= MAX_LEVEL)
		{
			StopPlaying();
			return;
		}

		currentLevel++;
		UnlockLevel(currentLevel);
		LoadLevel(currentLevel);
	}

	public void UnlockLevel(int level)
	{
		if (level > maxUnlockedLevel && level <= MAX_LEVEL)
		{
			maxUnlockedLevel = level;
			SaveProgress();
		}
	}

	void SaveProgress()
	{
		PlayerPrefs.SetInt("MaxUnlockedLevel", maxUnlockedLevel);
		PlayerPrefs.Save();
	}

	void LoadProgress()
	{
		maxUnlockedLevel = PlayerPrefs.GetInt("MaxUnlockedLevel", 1);
		maxUnlockedLevel = Mathf.Min(maxUnlockedLevel, MAX_LEVEL);
		currentLevel = maxUnlockedLevel;
	}
}
