using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
	[Header("Restart Button")]
	[SerializeField] private Button restartButton;

	[Header("Game Buttons")]
	[SerializeField] private Button playButton;
	[SerializeField] private Button homeButton;
	[SerializeField] private Button levelLeftButton;
	[SerializeField] private Button levelRightButton;

	[Header("Sliders")]
	[SerializeField] private Slider sfxSlider;

	[Header("Settings UI")]
	[SerializeField] private GameObject panel;
	[SerializeField] private Button settingsButton;
	[SerializeField] private Button closeButton;

	[Header("Audio")]
	private AudioSource audioSource;
	private AudioClip menuSelectSound;

	private CanvasGroup restartButtonGroup;
	private CanvasGroup playButtonGroup;
	private CanvasGroup homeButtonGroup;
	private CanvasGroup levelLeftButtonGroup;
	private CanvasGroup levelRightButtonGroup;

	private float fadeSpeed = 15f;
	private bool firstUpdate = true;

	void Start()
	{
		SetupAudio();
		SetupCanvasGroups();

		if (panel != null) {
			panel.SetActive(false);

			RectTransform rect = panel.GetComponent<RectTransform>();
			if (rect != null) {
				rect.anchoredPosition = new Vector2(0, rect.anchoredPosition.y);
			}
		}

		if (settingsButton != null)
			settingsButton.onClick.AddListener(OpenSettings);

		if (closeButton != null)
			closeButton.onClick.AddListener(CloseSettings);

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

		if (SettingsManager.instance != null)
		{
			if (sfxSlider != null)
			{
				sfxSlider.value = SettingsManager.instance.audioVolume;
				sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
			}
		}

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

	void SetupAudio()
	{
		audioSource = gameObject.AddComponent<AudioSource>();
		audioSource.playOnAwake = false;

		menuSelectSound = Resources.Load<AudioClip>("Sound/Menu Select");
		if (menuSelectSound == null)
			Debug.LogWarning("Could not load Menu Select sound from Resources/Sound/Menu Select");
		else
			Debug.Log("Menu Select sound loaded successfully");
	}

	void PlayButtonSound()
	{
		if (audioSource != null && menuSelectSound != null && SettingsManager.instance != null)
		{
			audioSource.PlayOneShot(menuSelectSound, SettingsManager.instance.audioVolume);
		}
	}

	void OpenSettings()
	{
		PlayButtonSound();
		if (panel != null)
		{
			panel.SetActive(true);
		}
	}

	void CloseSettings()
	{
		PlayButtonSound();
		if (panel != null)
			panel.SetActive(false);
	}

	void OnSFXVolumeChanged(float value)
	{
		if (SettingsManager.instance != null)
			SettingsManager.instance.SetAudioVolume(value);
	}

	void OnPlayClicked()
	{
		PlayButtonSound();
		if (GameStateManager.instance != null)
		{
			GameStateManager.instance.StartPlaying();
		}
	}

	void OnHomeClicked()
	{
		PlayButtonSound();
		if (GameStateManager.instance != null)
		{
			GameStateManager.instance.StopPlaying();
		}
	}

	void OnLevelLeftClicked()
	{
		PlayButtonSound();
		if (GameStateManager.instance != null && GameStateManager.instance.currentLevel > 1)
		{
			GameStateManager.instance.LoadLevel(GameStateManager.instance.currentLevel - 1);
		}
	}

	void OnLevelRightClicked()
	{
		PlayButtonSound();
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
		PlayButtonSound();
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
