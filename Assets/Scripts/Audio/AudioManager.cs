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
    public AudioClip[] clips;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(-0.2f, 0.2f)] public float randomPitch = 0.05f;
    public bool is3D = false;

    [Header("Perf Hints")]
    public float minInterval = 0.03f;
    public float maxDistance = 35f;
    [Range(0, 256)] public int priority = 128;
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
    [SerializeField] int poolSize = 24;
    [SerializeField] int maxSimultaneousSfx = 32;

    readonly Queue<AudioSource> pool = new();
    int currentActiveVoices = 0;

    [Header("Background Music")]
    public AudioClip backgroundMusic;

    [Header("Music")]
    public AudioSource musicSource;

    private AudioSource _oneShot2D;
    Dictionary<SfxId, SfxBank> dict;
    readonly Dictionary<SfxId, float> lastPlayTime = new();

    void Start()
    {
        if (backgroundMusic != null)
        {
            PlayMusic(backgroundMusic, 1f);
        }
    }

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;

        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        dict = new Dictionary<SfxId, SfxBank>();
        foreach (var b in sfxBanks) dict[b.id] = b;

        for (int i = 0; i < poolSize; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.outputAudioMixerGroup = sfxGroup;
            src.playOnAwake = false;
            pool.Enqueue(src);
        }

        if (!musicSource)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.outputAudioMixerGroup = musicGroup;
            musicSource.playOnAwake = false;
        }

        if (_oneShot2D == null)
        {
            _oneShot2D = gameObject.AddComponent<AudioSource>();
            _oneShot2D.outputAudioMixerGroup = sfxGroup;
            _oneShot2D.playOnAwake = false;
            _oneShot2D.spatialBlend = 0f;
        }
    }

    AudioSource GrabSource(bool is3D, Vector3? pos, SfxBank bank)
    {
        if (pool.Count == 0 && currentActiveVoices >= maxSimultaneousSfx)
            return null;

        var src = (pool.Count > 0) ? pool.Dequeue() : gameObject.AddComponent<AudioSource>();
        src.outputAudioMixerGroup = sfxGroup;
        src.playOnAwake = false;
        src.spatialBlend = is3D ? 1f : 0f;
        if (is3D && pos.HasValue) src.transform.position = pos.Value;

        src.minDistance = 2f;
        src.maxDistance = Mathf.Max(5f, bank.maxDistance > 0 ? bank.maxDistance : 30f);
        src.dopplerLevel = 0f;
        src.priority = bank.priority;

        return src;
    }

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

    public void Play2D(SfxId id)
    {
        if (!dict.TryGetValue(id, out var bank) || bank.clips == null || bank.clips.Length == 0) return;

        float now = Time.realtimeSinceStartup;
        if (bank.minInterval > 0f && lastPlayTime.TryGetValue(id, out float last) && (now - last) < bank.minInterval)
            return;
        lastPlayTime[id] = now;

        var clip = bank.clips[UnityEngine.Random.Range(0, bank.clips.Length)];
        if (!clip) return;

        _oneShot2D.pitch = 1f + UnityEngine.Random.Range(-bank.randomPitch, bank.randomPitch);
        _oneShot2D.volume = bank.volume;
        _oneShot2D.PlayOneShot(clip);
    }

    public void PlayAt(SfxId id, Vector3 pos)
    {
        if (!dict.TryGetValue(id, out var bank) || bank.clips == null || bank.clips.Length == 0) return;

        float now = Time.realtimeSinceStartup;
        if (bank.minInterval > 0f && lastPlayTime.TryGetValue(id, out float last) && (now - last) < bank.minInterval)
            return;
        lastPlayTime[id] = now;

        if (bank.maxDistance > 0f)
        {
            var cam = Camera.main;
            if (cam && Vector3.Distance(cam.transform.position, pos) > bank.maxDistance)
                return;
        }

        var clip = bank.clips[UnityEngine.Random.Range(0, bank.clips.Length)];
        if (!clip) return;

        var src = GrabSource(true, pos, bank);
        if (src == null) return;

        src.clip = clip;
        src.volume = bank.volume;
        src.pitch = 1f + UnityEngine.Random.Range(-bank.randomPitch, bank.randomPitch);
        src.Play();

        currentActiveVoices++;
        Recycle(src, clip.length / Mathf.Abs(src.pitch));
    }

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

    public SfxBank GetBank(SfxId id)
    {
        if (dict.TryGetValue(id, out var bank)) return bank;
        return null;
    }

    public void PlayAtImportant(SfxId id, Vector3 pos, bool reinforce2D = true)
    {
        if (!dict.TryGetValue(id, out var bank) || bank.clips == null || bank.clips.Length == 0) return;

        var clip = bank.clips[UnityEngine.Random.Range(0, bank.clips.Length)];
        if (!clip) return;

        var src = GrabSource(true, pos, bank);
        if (src != null)
        {
            src.clip = clip;
            src.volume = Mathf.Min(1f, bank.volume * 1.2f);
            src.pitch = 1f + UnityEngine.Random.Range(-bank.randomPitch, bank.randomPitch);
            src.priority = Mathf.Min(bank.priority, 64);
            src.Play();

            currentActiveVoices++;
            Recycle(src, clip.length / Mathf.Abs(src.pitch));
        }

        if (reinforce2D && _oneShot2D != null)
        {
            _oneShot2D.pitch = 1f;
            _oneShot2D.volume = Mathf.Clamp01((dict[id].volume) * 0.8f);
            _oneShot2D.PlayOneShot(clip);
        }
    }
}
