using System;
using UnityEngine;

[DefaultExecutionOrder(-1100)]
public sealed class GameSettingsManager : MonoBehaviour
{
    private const string BgmVolumeKey = "settings_bgm_volume";
    private const string SfxVolumeKey = "settings_sfx_volume";
    private const string BrightnessKey = "settings_brightness";

    public static GameSettingsManager Instance { get; private set; }

    public event Action<float> BgmVolumeChanged;
    public event Action<float> SfxVolumeChanged;
    public event Action<float> BrightnessChanged;

    [SerializeField, Range(0f, 1f)] private float bgmVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float brightness = 1f;

    public float BgmVolume => bgmVolume;
    public float SfxVolume => sfxVolume;
    public float Brightness => brightness;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    public static GameSettingsManager EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameObject settingsObject = new GameObject("GameSettingsManager");
        return settingsObject.AddComponent<GameSettingsManager>();
    }

    public void SetBgmVolume(float value)
    {
        float clamped = Mathf.Clamp01(value);
        if (Mathf.Approximately(bgmVolume, clamped))
        {
            return;
        }

        bgmVolume = clamped;
        PlayerPrefs.SetFloat(BgmVolumeKey, bgmVolume);
        PlayerPrefs.Save();
        BgmVolumeChanged?.Invoke(bgmVolume);
    }

    public void SetSfxVolume(float value)
    {
        float clamped = Mathf.Clamp01(value);
        if (Mathf.Approximately(sfxVolume, clamped))
        {
            return;
        }

        sfxVolume = clamped;
        PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
        PlayerPrefs.Save();
        SfxVolumeChanged?.Invoke(sfxVolume);
    }

    public void SetBrightness(float value)
    {
        float clamped = Mathf.Clamp01(value);
        if (Mathf.Approximately(brightness, clamped))
        {
            return;
        }

        brightness = clamped;
        PlayerPrefs.SetFloat(BrightnessKey, brightness);
        PlayerPrefs.Save();
        BrightnessChanged?.Invoke(brightness);
    }

    public void NotifyCurrentValues()
    {
        BgmVolumeChanged?.Invoke(bgmVolume);
        SfxVolumeChanged?.Invoke(sfxVolume);
        BrightnessChanged?.Invoke(brightness);
    }

    private void Load()
    {
        bgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(BgmVolumeKey, bgmVolume));
        sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, sfxVolume));
        brightness = Mathf.Clamp01(PlayerPrefs.GetFloat(BrightnessKey, brightness));
    }
}
