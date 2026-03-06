using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
	[Header("Restart Button")]
	[SerializeField] private Button restartButton;

	[Header("Game Buttons")]
	[SerializeField] private Button playButton;
	[SerializeField] private Button homeButton;
	[SerializeField] private Button levelLeftButton;
	[SerializeField] private Button levelRightButton;

	private CanvasGroup restartButtonGroup;
	private CanvasGroup playButtonGroup;
	private CanvasGroup homeButtonGroup;
	private CanvasGroup levelLeftButtonGroup;
	private CanvasGroup levelRightButtonGroup;

	private float fadeSpeed = 15f;
	private bool firstUpdate = true;

	void Start()
	{
		SetupCanvasGroups();

		if (playButton != null)
			playButton.onClick.AddListener(OnPlayClicked);

		if (homeButton != null)
			homeButton.onClick.AddListener(OnHomeClicked);

		if (levelLeftButton != null)
			levelLeftButton.onClick.AddListener(OnLevelLeftClicked);

		if (levelRightButton != null)
			levelRightButton.onClick.AddListener(OnLevelRightClicked);

		if (restartButton != null)
			restartButton.onClick.AddListener(OnRestartClicked);

		UpdateButtonVisibility();
	}

	void SetupCanvasGroups()
	{
		if (restartButton != null)
		{
			restartButtonGroup = restartButton.gameObject.GetComponent<CanvasGroup>();
			if (restartButtonGroup == null)
				restartButtonGroup = restartButton.gameObject.AddComponent<CanvasGroup>();
		}

		if (playButton != null)
		{
			playButtonGroup = playButton.gameObject.GetComponent<CanvasGroup>();
			if (playButtonGroup == null)
				playButtonGroup = playButton.gameObject.AddComponent<CanvasGroup>();
		}

		if (homeButton != null)
		{
			homeButtonGroup = homeButton.gameObject.GetComponent<CanvasGroup>();
			if (homeButtonGroup == null)
				homeButtonGroup = homeButton.gameObject.AddComponent<CanvasGroup>();
		}

		if (levelLeftButton != null)
		{
			levelLeftButtonGroup = levelLeftButton.gameObject.GetComponent<CanvasGroup>();
			if (levelLeftButtonGroup == null)
				levelLeftButtonGroup = levelLeftButton.gameObject.AddComponent<CanvasGroup>();
		}

		if (levelRightButton != null)
		{
			levelRightButtonGroup = levelRightButton.gameObject.GetComponent<CanvasGroup>();
			if (levelRightButtonGroup == null)
				levelRightButtonGroup = levelRightButton.gameObject.AddComponent<CanvasGroup>();
		}
	}

	void Update()
	{
		UpdateButtonVisibility(firstUpdate);
		if (firstUpdate) firstUpdate = false;
	}

	void OnPlayClicked()
	{
		if (GameStateManager.instance != null)
		{
			GameStateManager.instance.StartPlaying();
		}
	}

	void OnHomeClicked()
	{
		if (GameStateManager.instance != null)
		{
			GameStateManager.instance.StopPlaying();
		}
	}

	void OnLevelLeftClicked()
	{
		if (GameStateManager.instance != null && GameStateManager.instance.currentLevel > 1)
		{
			GameStateManager.instance.LoadLevel(GameStateManager.instance.currentLevel - 1);
		}
	}

	void OnLevelRightClicked()
	{
		if (GameStateManager.instance != null)
		{
			int nextLevel = GameStateManager.instance.currentLevel + 1;
			if (nextLevel <= GameStateManager.instance.maxUnlockedLevel)
			{
				GameStateManager.instance.LoadLevel(nextLevel);
			}
		}
	}

	void OnRestartClicked()
	{
		if (GameStateManager.instance != null)
		{
			GameStateManager.instance.ReloadCurrentLevel();
		}
	}

	void UpdateButtonVisibility(bool instant = false)
	{
		if (GameStateManager.instance == null) return;

		bool isPlaying = GameStateManager.instance.isPlaying;
		int currentLevel = GameStateManager.instance.currentLevel;
		int maxUnlocked = GameStateManager.instance.maxUnlockedLevel;

		FadeButton(restartButtonGroup, isPlaying, instant);
		FadeButton(homeButtonGroup, isPlaying, instant);
		FadeButton(playButtonGroup, !isPlaying, instant);
		FadeButton(levelLeftButtonGroup, !isPlaying && currentLevel > 1, instant);
		FadeButton(levelRightButtonGroup, !isPlaying && currentLevel < maxUnlocked, instant);
	}

	void FadeButton(CanvasGroup group, bool shouldShow, bool instant = false)
	{
		if (group == null) return;

		float targetAlpha = shouldShow ? 1f : 0f;

		if (instant)
		{
			group.alpha = targetAlpha;
		}
		else
		{
			group.alpha = Mathf.Lerp(group.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
		}

		group.interactable = shouldShow;
		group.blocksRaycasts = shouldShow;
	}
}
