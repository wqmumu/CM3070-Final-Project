using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public enum SfxId
{
    TroopShoot, TroopDie, TroopMove,
    BulletHitZombie, BulletHitBoss, BulletHitGate,
    ZombieAttack, ZombieDie, BossAttack, BossDie,
    GateTrigger, Victory, Defeat
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

    [Header("Background Music (GAME scene)")]
    public AudioClip backgroundMusic;
    [Range(0f, 1f)]
    [Tooltip("Default volume for GAME background music (multiplies with Mixer → Music).")]
    public float backgroundMusicVolume = 1f;
    [SerializeField] private string menuSceneName = "MainMenu";  // don’t auto-play BGM in menu

    [Header("Music")]
    public AudioSource musicSource;

    private AudioSource _oneShot2D;
    Dictionary<SfxId, SfxBank> dict;
    readonly Dictionary<SfxId, float> lastPlayTime = new();

    void Awake()
    {
        // Singleton
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        // Build dictionary
        dict = new Dictionary<SfxId, SfxBank>();
        if (sfxBanks != null)
            foreach (var b in sfxBanks)
                if (b != null) dict[b.id] = b;

        // Prewarm pool
        for (int i = 0; i < poolSize; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            if (sfxGroup) src.outputAudioMixerGroup = sfxGroup;
            src.playOnAwake = false;
            pool.Enqueue(src);
        }

        // Music source
        if (!musicSource)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            if (musicGroup) musicSource.outputAudioMixerGroup = musicGroup;
        }
        musicSource.ignoreListenerPause = true;

        // 2D oneshot
        if (_oneShot2D == null)
        {
            _oneShot2D = gameObject.AddComponent<AudioSource>();
            _oneShot2D.playOnAwake = false;
            _oneShot2D.spatialBlend = 0f;
            if (sfxGroup) _oneShot2D.outputAudioMixerGroup = sfxGroup;
            _oneShot2D.ignoreListenerPause = true;
        }

        // Apply saved mixer values (Master/Music/SFX)
        ApplyPrefsToMixer();

        // Make sure slider value applies
        musicSource.volume = Mathf.Clamp01(backgroundMusicVolume);

        // Handle scene changes for auto-music
        SceneManager.sceneLoaded += OnSceneLoaded_AutoMusic;
    }

    void OnDestroy()
    {
        if (I == this) SceneManager.sceneLoaded -= OnSceneLoaded_AutoMusic;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (musicSource != null)
            musicSource.volume = Mathf.Clamp01(backgroundMusicVolume);
    }
#endif

    void Start()
    {
        // If we are already in a GAME scene (not the menu), start BGM once.
        if (!IsMenuScene(SceneManager.GetActiveScene().name) && backgroundMusic)
            PlayMusic(backgroundMusic, 0.5f);
    }

    // -------- Scene-based music control --------
    private void OnSceneLoaded_AutoMusic(Scene s, LoadSceneMode m)
    {
        if (IsMenuScene(s.name))
        {
            // Don’t play game BGM in menu (MenuMusic handles that)
            if (musicSource.isPlaying) musicSource.Stop();
        }
        else
        {
            // Entered gameplay scene – start BGM if assigned
            if (backgroundMusic && (!musicSource.isPlaying || musicSource.clip != backgroundMusic))
                PlayMusic(backgroundMusic, 0.5f);
        }
    }
    private bool IsMenuScene(string sceneName) =>
        !string.IsNullOrEmpty(menuSceneName) && string.Equals(sceneName, menuSceneName, StringComparison.Ordinal);

    // -------- Mixer prefs --------
    private void ApplyPrefsToMixer()
    {
        float master = PlayerPrefs.GetFloat("pp_master", 1f);
        float music = PlayerPrefs.GetFloat("pp_music", 1f);
        float sfx = PlayerPrefs.GetFloat("pp_sfx", 1f);

        SetMixerLinear("MasterVol", master);
        SetMixerLinear("MusicVol", music);
        SetMixerLinear("SFXVol", sfx);
    }

    private void SetMixerLinear(string exposedParam, float linear01)
    {
        if (!mixer || string.IsNullOrEmpty(exposedParam)) return;
        if (Mathf.Approximately(linear01, 0f)) { mixer.SetFloat(exposedParam, -80f); return; }
        float l = Mathf.Clamp(linear01, 0.0001f, 1f);
        mixer.SetFloat(exposedParam, Mathf.Log10(l) * 20f);
    }

    // -------- SFX --------
    AudioSource GrabSource(bool is3D, Vector3? pos, SfxBank bank)
    {
        if (pool.Count == 0 && currentActiveVoices >= maxSimultaneousSfx) return null;

        var src = (pool.Count > 0) ? pool.Dequeue() : gameObject.AddComponent<AudioSource>();
        if (sfxGroup) src.outputAudioMixerGroup = sfxGroup;
        src.playOnAwake = false;
        src.spatialBlend = is3D ? 1f : 0f;
        if (is3D && pos.HasValue) src.transform.position = pos.Value;

        src.minDistance = 2f;
        src.maxDistance = Mathf.Max(5f, bank != null && bank.maxDistance > 0 ? bank.maxDistance : 30f);
        src.dopplerLevel = 0f;
        src.priority = bank != null ? bank.priority : 128;

        return src;
    }

    void Recycle(AudioSource src, float time) { StartCoroutine(RecycleAfter(src, time)); }
    IEnumerator RecycleAfter(AudioSource src, float t)
    {
        float end = Time.realtimeSinceStartup + t;
        while (Time.realtimeSinceStartup < end) yield return null;
        if (src)
        {
            src.Stop();
            pool.Enqueue(src);
            currentActiveVoices = Mathf.Max(0, currentActiveVoices - 1);
        }
    }

    public void Play2D(SfxId id)
    {
        if (Time.timeScale == 0f && id == SfxId.TroopMove) return;
        if (!dict.TryGetValue(id, out var bank) || bank.clips == null || bank.clips.Length == 0) return;

        float now = Time.realtimeSinceStartup;
        if (bank.minInterval > 0f && lastPlayTime.TryGetValue(id, out float last) && (now - last) < bank.minInterval) return;
        lastPlayTime[id] = now;

        var clip = bank.clips[UnityEngine.Random.Range(0, bank.clips.Length)];
        if (!clip) return;

        _oneShot2D.pitch = 1f + UnityEngine.Random.Range(-bank.randomPitch, bank.randomPitch);
        _oneShot2D.volume = bank.volume;
        _oneShot2D.PlayOneShot(clip);
    }

    public void PlayAt(SfxId id, Vector3 pos)
    {
        if (Time.timeScale == 0f && id == SfxId.TroopMove) return;
        if (!dict.TryGetValue(id, out var bank) || bank.clips == null || bank.clips.Length == 0) return;

        float now = Time.realtimeSinceStartup;
        if (bank.minInterval > 0f && lastPlayTime.TryGetValue(id, out float last) && (now - last) < bank.minInterval) return;
        lastPlayTime[id] = now;

        if (bank.maxDistance > 0f)
        {
            var cam = Camera.main;
            if (cam && Vector3.Distance(cam.transform.position, pos) > bank.maxDistance) return;
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

    // -------- Music --------
    public void PlayMusic(AudioClip clip, float fade = 0.5f)
    {
        if (!clip || musicSource == null) return;
        StartCoroutine(FadeMusicIn(clip, Mathf.Max(0f, fade)));
    }

    IEnumerator FadeMusicIn(AudioClip clip, float t)
    {
        // fade down if something already playing
        if (musicSource.isPlaying)
        {
            float start = musicSource.volume;
            for (float s = t; s > 0f; s -= Time.unscaledDeltaTime)
            {
                musicSource.volume = Mathf.Lerp(0f, start, s / t);
                yield return null;
            }
        }

        musicSource.clip = clip;
        musicSource.volume = 0f;
        musicSource.Play();

        float target = Mathf.Clamp01(backgroundMusicVolume);
        for (float s = 0; s < t; s += Time.unscaledDeltaTime)
        {
            musicSource.volume = Mathf.Lerp(0f, target, s / t);
            yield return null;
        }
        musicSource.volume = target;
    }

    public void SetBackgroundMusicVolume(float v01)
    {
        backgroundMusicVolume = Mathf.Clamp01(v01);
        if (musicSource) musicSource.volume = backgroundMusicVolume;
    }

    public SfxBank GetBank(SfxId id) => dict.TryGetValue(id, out var bank) ? bank : null;

    public void PlayAtImportant(SfxId id, Vector3 pos, bool reinforce2D = true)
    {
        if (Time.timeScale == 0f && id == SfxId.TroopMove) return;
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
            _oneShot2D.volume = Mathf.Clamp01(bank.volume * 0.8f);
            _oneShot2D.PlayOneShot(clip);
        }
    }
}
