using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("BGM")]
    public AudioClip bgmClip;
    public float Bvolume;
    AudioSource bgmPlayer;

    [Header("SFX")]
    public AudioClip[] sfxClips;
    public float Svolume = 1f;
    public int Schannels = 8;
    AudioSource[] sfxPlayers;

    int channelIdx;

    public enum Sfx
    {
        OnBtn, ClickBtn, Mini0, Mini1, Mini2, MiniClear, Mini0Btn, Mini1Btn,
        Warning, Planet, Cloud, Blackhole, Over, Clear
    }

    [Header("SFX Spam Protection")]
    public float defaultCooldown = 0.05f;
    public float cloudCooldown = 0.25f;
    public float blackholeCooldown = 0.25f;

    // Time.timeScale 무관하게 동작하도록 unscaledTime 기준
    Dictionary<Sfx, float> lastPlayTime = new Dictionary<Sfx, float>();

    void Awake()
    {
        instance = this;
        Init();
    }

    void Init()
    {
        // init bgm player
        GameObject bgmObject = new GameObject("BgmPlayer");
        bgmObject.transform.parent = transform;
        bgmPlayer = bgmObject.AddComponent<AudioSource>();
        bgmPlayer.playOnAwake = false;
        bgmPlayer.loop = true;
        bgmPlayer.volume = Bvolume;
        bgmPlayer.clip = bgmClip;

        // init sfx player
        GameObject sfxObject = new GameObject("SfxPlayer");
        sfxObject.transform.parent = transform;

        sfxPlayers = new AudioSource[Schannels];
        for (int i = 0; i < Schannels; i++)
        {
            sfxPlayers[i] = sfxObject.AddComponent<AudioSource>();
            sfxPlayers[i].playOnAwake = false;
            sfxPlayers[i].bypassListenerEffects = true;
            sfxPlayers[i].volume = Svolume;
        }
    }

    public void PlayBgm()
    {
        if (!bgmPlayer.isPlaying) bgmPlayer.Play();
    }

    float GetCooldown(Sfx sfx)
    {
        switch (sfx)
        {
            case Sfx.Cloud: return cloudCooldown;
            case Sfx.Blackhole: return blackholeCooldown;
            default: return defaultCooldown;
        }
    }

    bool CanPlayNow(Sfx sfx)
    {
        float now = Time.unscaledTime;
        float cd = GetCooldown(sfx);

        if (lastPlayTime.TryGetValue(sfx, out float last))
        {
            if (now - last < cd) return false;
        }

        lastPlayTime[sfx] = now;
        return true;
    }

    public void PlaySfx(Sfx sfx)
    {
        if (!CanPlayNow(sfx)) return;

        // 빈 채널 찾기
        for (int i = 0; i < Schannels; i++)
        {
            int loopIdx = (i + channelIdx) % Schannels;
            if (sfxPlayers[loopIdx].isPlaying) continue;

            channelIdx = loopIdx;

            int clipIdx = (int)sfx;
            if (clipIdx < 0 || clipIdx >= sfxClips.Length) return;

            sfxPlayers[loopIdx].clip = sfxClips[clipIdx];
            sfxPlayers[loopIdx].Play();
            return;
        }

        // 전부 재생 중이면: 중요한 소리는 하나 가져오기
        if (sfx == Sfx.Cloud || sfx == Sfx.Blackhole) return;

        int stealIdx = channelIdx;
        channelIdx = (channelIdx + 1) % Schannels;

        int stealClipIdx = (int)sfx;
        if (stealClipIdx < 0 || stealClipIdx >= sfxClips.Length) return;

        sfxPlayers[stealIdx].Stop();
        sfxPlayers[stealIdx].clip = sfxClips[stealClipIdx];
        sfxPlayers[stealIdx].Play();
    }
}