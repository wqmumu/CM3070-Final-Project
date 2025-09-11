using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public enum SfxId
{
    TroopShoot,
    TroopDie,
    TroopMove,
    BulletHitZombie,
    BulletHitBoss,
    BulletHitGate,
    ZombieAttack,
    ZombieDie,
    BossAttack,
    BossDie,
    GateTrigger,
    Victory,
    Defeat
}

[Serializable]
public class SfxBank
{
    public SfxId id;
    public AudioClip[] clips;                  // allow variations
    [Range(0f, 1f)] public float volume = 1f;
    [Range(-0.2f, 0.2f)] public float randomPitch = 0.05f;
    public bool is3D = false;                  // hint only; Play2D ignores this

    // ---- Performance hints (tunable in Inspector) ----
    [Header("Perf Hints")]
    [Tooltip("Minimum time between plays of THIS SFX (seconds). 0 = no limit.")]
    public float minInterval = 0.03f;          // prevents dozens of identical plays per frame

    [Tooltip("Skip 3D playback if farther than this (meters). 0 = unlimited.")]
    public float maxDistance = 35f;            // avoids playing off-screen audio

    [Tooltip("Lower = more important (Unity priority 0..256).")]
    [Range(0, 256)] public int priority = 128; // voice arbitration when many sounds overlap
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager I { get; private set; }

    [Header("Mixer")]
    public AudioMixer mixer;
    public AudioMixerGroup sfxGroup;
    public AudioMixerGroup musicGroup;

    [Header("Banks")]
    public SfxBank[] sfxBanks;

    [Header("Pool")]
    [SerializeField] int poolSize = 24;        // initial prewarmed sources
    [SerializeField, Tooltip("Hard cap on concurrent SFX voices. New plays beyond this are dropped.")]
    int maxSimultaneousSfx = 32;

    readonly Queue<AudioSource> pool = new();
    int currentActiveVoices = 0;               // voices currently playing

    [Header("Background Music")]
    public AudioClip backgroundMusic;

    [Header("Music")]
    public AudioSource musicSource;            // dedicated looping music source

    // Dedicated one-shot 2D source for very frequent UI/pew sounds
    private AudioSource _oneShot2D;

    // Fast lookup for banks + simple per-ID cooldowns
    Dictionary<SfxId, SfxBank> dict;
    readonly Dictionary<SfxId, float> lastPlayTime = new();

    void Start()
    {
        if (backgroundMusic != null)
        {
            PlayMusic(backgroundMusic, 1f); // fade-in over 1 sec
        }
    }

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;

        // Make sure we're a root object so DDOL works
        if (transform.parent != null) transform.SetParent(null);

        DontDestroyOnLoad(gameObject);

        // Build fast lookup
        dict = new Dictionary<SfxId, SfxBank>();
        foreach (var b in sfxBanks) dict[b.id] = b;

        // Prewarm SFX pool
        for (int i = 0; i < poolSize; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.outputAudioMixerGroup = sfxGroup;
            src.playOnAwake = false;
            pool.Enqueue(src);
        }

        // Dedicated looping music source
        if (!musicSource)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.outputAudioMixerGroup = musicGroup;
            musicSource.playOnAwake = false;
        }

        // One-shot 2D source for spammy SFX (e.g., TroopShoot)
        if (_oneShot2D == null)
        {
            _oneShot2D = gameObject.AddComponent<AudioSource>();
            _oneShot2D.outputAudioMixerGroup = sfxGroup;
            _oneShot2D.playOnAwake = false;
            _oneShot2D.spatialBlend = 0f; // 2D
        }
    }

    // --- Source management ----------------------------------------------------

    AudioSource GrabSource(bool is3D, Vector3? pos, SfxBank bank)
    {
        // Respect hard cap: if we already have max voices, drop this play politely
        if (pool.Count == 0 && currentActiveVoices >= maxSimultaneousSfx)
            return null;

        var src = (pool.Count > 0) ? pool.Dequeue() : gameObject.AddComponent<AudioSource>();
        src.outputAudioMixerGroup = sfxGroup;
        src.playOnAwake = false;
        src.spatialBlend = is3D ? 1f : 0f;
        if (is3D && pos.HasValue) src.transform.position = pos.Value;

        // Sensible 3D defaults
        src.minDistance = 2f;
        src.maxDistance = Mathf.Max(5f, bank.maxDistance > 0 ? bank.maxDistance : 30f);
        src.dopplerLevel = 0f;
        src.priority = bank.priority;

        return src;
    }

    // Recycle using realtime so timeScale changes don’t stall the pool
    void Recycle(AudioSource src, float time)
    {
        StartCoroutine(RecycleAfter(src, time));
    }
    IEnumerator RecycleAfter(AudioSource src, float t)
    {
        float end = Time.realtimeSinceStartup + t;
        while (Time.realtimeSinceStartup < end) yield return null;
        src.Stop();
        pool.Enqueue(src);
        currentActiveVoices = Mathf.Max(0, currentActiveVoices - 1);
    }

    // --- Public API -----------------------------------------------------------

    // Allocation-free, pool-free 2D playback (ideal for spammy SFX)
    public void Play2D(SfxId id)
    {
        if (!dict.TryGetValue(id, out var bank) || bank.clips == null || bank.clips.Length == 0) return;

        // Per-ID cooldown
        float now = Time.realtimeSinceStartup;
        if (bank.minInterval > 0f && lastPlayTime.TryGetValue(id, out float last) && (now - last) < bank.minInterval)
            return;
        lastPlayTime[id] = now;

        var clip = bank.clips[UnityEngine.Random.Range(0, bank.clips.Length)];
        if (!clip) return;

        _oneShot2D.pitch = 1f + UnityEngine.Random.Range(-bank.randomPitch, bank.randomPitch);
        _oneShot2D.volume = bank.volume;
        _oneShot2D.PlayOneShot(clip);
        // one-shot 2D does not consume a pooled voice
    }

    // Positional (3D) playback via pool (bullet hits, enemies, gate trigger, etc.)
    public void PlayAt(SfxId id, Vector3 pos)
    {
        if (!dict.TryGetValue(id, out var bank) || bank.clips == null || bank.clips.Length == 0) return;

        // Per-ID cooldown
        float now = Time.realtimeSinceStartup;
        if (bank.minInterval > 0f && lastPlayTime.TryGetValue(id, out float last) && (now - last) < bank.minInterval)
            return;
        lastPlayTime[id] = now;

        // Distance cull against camera (skip inaudible far sounds)
        if (bank.maxDistance > 0f)
        {
            var cam = Camera.main;
            if (cam && Vector3.Distance(cam.transform.position, pos) > bank.maxDistance)
                return;
        }

        var clip = bank.clips[UnityEngine.Random.Range(0, bank.clips.Length)];
        if (!clip) return;

        var src = GrabSource(true, pos, bank);
        if (src == null) return; // hard-cap reached, politely skip

        src.clip = clip;
        src.volume = bank.volume;
        src.pitch = 1f + UnityEngine.Random.Range(-bank.randomPitch, bank.randomPitch);
        src.Play();

        currentActiveVoices++;
        Recycle(src, clip.length / Mathf.Abs(src.pitch));
    }

    // Music control (simple fade-in)
    public void PlayMusic(AudioClip clip, float fade = 0.5f)
    {
        if (clip == null) return;
        StartCoroutine(FadeMusicIn(clip, fade));
    }

    IEnumerator FadeMusicIn(AudioClip clip, float t)
    {
        float start = musicSource.volume;
        if (musicSource.isPlaying)
        {
            for (float s = t; s > 0f; s -= Time.unscaledDeltaTime)
            {
                musicSource.volume = Mathf.Lerp(0f, start, s / t);
                yield return null;
            }
        }
        musicSource.clip = clip; musicSource.volume = 0f; musicSource.Play();
        for (float s = 0; s < t; s += Time.unscaledDeltaTime)
        {
            musicSource.volume = Mathf.Lerp(0f, 1f, s / t);
            yield return null;
        }
        musicSource.volume = 1f;
    }

    // Helper for systems that want to fetch a clip directly (e.g., TroopMove loop)
    public SfxBank GetBank(SfxId id)
    {
        if (dict.TryGetValue(id, out var bank)) return bank;
        return null;
    }
}
