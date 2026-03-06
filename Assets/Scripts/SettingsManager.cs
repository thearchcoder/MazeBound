using UnityEngine;

public class SettingsManager : MonoBehaviour
{
	public static SettingsManager instance;

	public bool audioEnabled = true;

	[Range(0f, 1f)] public float audioVolume = 1f;

	void Awake()
	{
		if (instance == null)
		{
			instance = this;
			DontDestroyOnLoad(gameObject);
			LoadSettings();
		}
		else
		{
			Destroy(gameObject);
		}
	}

	void Start()
	{
		ApplySettings();
	}

	public void SetAudioVolume(float volume)
	{
		audioVolume = volume;
		ApplySettings();
		SaveSettings();
	}

	void ApplySettings()
	{
		AudioListener.volume = audioVolume;
	}

	void SaveSettings()
	{
		PlayerPrefs.SetFloat("AudioVolume", audioVolume);
		PlayerPrefs.Save();
	}

	void LoadSettings()
	{
		audioVolume = PlayerPrefs.GetFloat("AudioVolume", 1f);
	}
}
