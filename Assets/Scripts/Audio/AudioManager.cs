using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("BGM")]
    public AudioClip bgmClip;
    [Range(0f, 1f)] public float Bvolume = 1f;
    private AudioSource bgmPlayer;

    [Header("SFX")]
    public AudioClip[] sfxClips;
    [Range(0f, 1f)] public float Svolume = 1f;
    public int Schannels = 8;
    private AudioSource[] sfxPlayers;

    private readonly Dictionary<Sfx, float> lastPlayTime = new Dictionary<Sfx, float>();
    private int channelIdx;
    private bool initialized;

    public enum Sfx
    {
        Jump, Slide, Attack, Parry, ParrySuccess, Hit,  // Player
        SharkAttack, SharkHit, SharkDie,                // Monster
        MimicDie, PigeonDie, OctopusDie, OctopusAttack,
        TileStep, TileEffect,                           // Tile
        CardReroll, CardEffect,                         // Card
        ObstacleBreak,                                  // Obstacle
        ButtonClick, ShopBuy, DiceRoll                  // General
    }

    [Header("SFX Spam Protection")]
    public float defaultCooldown = 0.05f;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            instance.AbsorbSceneAudio(this);
            Destroy(gameObject);
            return;
        }

        instance = this;

        if (transform.parent != null)
        {
            transform.SetParent(null);
        }

        gameObject.name = "AudioManager [Persistent]";
        DontDestroyOnLoad(gameObject);

        Init();
        AbsorbSceneAudio(this);
        SubscribeToSettings();
    }

    private void Start()
    {
        PlayBgm();
    }

    private void OnDestroy()
    {
        if (instance == this && GameSettingsManager.Instance != null)
        {
            GameSettingsManager.Instance.BgmVolumeChanged -= HandleBgmVolumeChanged;
            GameSettingsManager.Instance.SfxVolumeChanged -= HandleSfxVolumeChanged;
        }
    }

    private void Init()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;

        GameObject bgmObject = new GameObject("BgmPlayer");
        bgmObject.transform.SetParent(transform, false);
        bgmPlayer = bgmObject.AddComponent<AudioSource>();
        bgmPlayer.playOnAwake = false;
        bgmPlayer.loop = true;

        int channelCount = Mathf.Max(1, Schannels);
        GameObject sfxObject = new GameObject("SfxPlayer");
        sfxObject.transform.SetParent(transform, false);

        sfxPlayers = new AudioSource[channelCount];
        for (int i = 0; i < channelCount; i++)
        {
            AudioSource source = sfxObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.bypassListenerEffects = true;
            sfxPlayers[i] = source;
        }
    }

    private void SubscribeToSettings()
    {
        GameSettingsManager settingsManager = GameSettingsManager.EnsureInstance();

        settingsManager.BgmVolumeChanged -= HandleBgmVolumeChanged;
        settingsManager.SfxVolumeChanged -= HandleSfxVolumeChanged;
        settingsManager.BgmVolumeChanged += HandleBgmVolumeChanged;
        settingsManager.SfxVolumeChanged += HandleSfxVolumeChanged;

        ApplyVolumes(settingsManager.BgmVolume, settingsManager.SfxVolume);
    }

    private void HandleBgmVolumeChanged(float volume)
    {
        Bvolume = Mathf.Clamp01(volume);
        if (bgmPlayer != null)
        {
            bgmPlayer.volume = Bvolume;
        }
    }

    private void HandleSfxVolumeChanged(float volume)
    {
        Svolume = Mathf.Clamp01(volume);

        if (sfxPlayers == null)
        {
            return;
        }

        for (int i = 0; i < sfxPlayers.Length; i++)
        {
            if (sfxPlayers[i] != null)
            {
                sfxPlayers[i].volume = Svolume;
            }
        }
    }

    private void ApplyVolumes(float bgmVolume, float sfxVolume)
    {
        HandleBgmVolumeChanged(bgmVolume);
        HandleSfxVolumeChanged(sfxVolume);
    }

    private void AbsorbSceneAudio(AudioManager source)
    {
        if (source == null)
        {
            return;
        }

        if (source.sfxClips != null && source.sfxClips.Length > 0)
        {
            sfxClips = source.sfxClips;
        }

        defaultCooldown = source.defaultCooldown;

        if (source.bgmClip == null)
        {
            return;
        }

        bool clipChanged = bgmPlayer == null || bgmPlayer.clip != source.bgmClip;
        bgmClip = source.bgmClip;

        if (bgmPlayer == null)
        {
            return;
        }

        bgmPlayer.clip = bgmClip;

        if (clipChanged)
        {
            bgmPlayer.Stop();
        }

        PlayBgm();
    }

    public void PlayBgm()
    {
        if (bgmPlayer == null || bgmClip == null)
        {
            return;
        }

        bgmPlayer.clip = bgmClip;
        if (!bgmPlayer.isPlaying)
        {
            bgmPlayer.Play();
        }
    }

    public void StopBgm()
    {
        if (bgmPlayer != null && bgmPlayer.isPlaying)
        {
            bgmPlayer.Stop();
        }
    }

    private float GetCooldown(Sfx sfx)
    {
        switch (sfx)
        {
            default:
                return defaultCooldown;
        }
    }

    private bool CanPlayNow(Sfx sfx)
    {
        float now = Time.unscaledTime;
        float cooldown = GetCooldown(sfx);

        if (lastPlayTime.TryGetValue(sfx, out float last) && now - last < cooldown)
        {
            return false;
        }

        lastPlayTime[sfx] = now;
        return true;
    }

    public void PlaySfx(Sfx sfx)
    {
        if (!CanPlayNow(sfx) || sfxPlayers == null || sfxPlayers.Length == 0)
        {
            return;
        }

        int clipIdx = (int)sfx;
        if (sfxClips == null || clipIdx < 0 || clipIdx >= sfxClips.Length || sfxClips[clipIdx] == null)
        {
            return;
        }

        for (int i = 0; i < sfxPlayers.Length; i++)
        {
            int loopIdx = (i + channelIdx) % sfxPlayers.Length;
            if (sfxPlayers[loopIdx].isPlaying)
            {
                continue;
            }

            channelIdx = loopIdx;
            sfxPlayers[loopIdx].clip = sfxClips[clipIdx];
            sfxPlayers[loopIdx].Play();
            return;
        }

        int stealIdx = channelIdx;
        channelIdx = (channelIdx + 1) % sfxPlayers.Length;

        sfxPlayers[stealIdx].Stop();
        sfxPlayers[stealIdx].clip = sfxClips[clipIdx];
        sfxPlayers[stealIdx].Play();
    }
}
