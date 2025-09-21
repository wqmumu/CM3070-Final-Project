using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using System.Collections;

public class MenuMusic : MonoBehaviour
{
    [Header("Clip & Output")]
    public AudioClip menuClip;
    public bool loop = true;
    [Range(0f, 1f)] public float volume = 0.5f;
    public AudioMixerGroup musicGroup; // route to Mixer ¡ú Music

    [Header("Lifecycle")]
    [Tooltip("If true, this object won¡¯t be destroyed on scene load.")]
    public bool persistAcrossScenes = false;

    [Tooltip("Name of your menu scene. Used to auto-stop when leaving it.")]
    public string menuSceneName = "MainMenu";

    [Tooltip("If true, fades out and stops automatically when scene != menuSceneName.")]
    public bool autoStopOnSceneChange = true;

    [Tooltip("Default fade-out time (seconds).")]
    public float defaultFadeOut = 0.5f;

    [Tooltip("Destroy the GameObject after stopping.")]
    public bool destroyWhenStopped = true;

    private AudioSource src;
    private Coroutine fadeCo;

    private void Awake()
    {
        src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = loop;
        src.volume = volume;
        if (musicGroup) src.outputAudioMixerGroup = musicGroup;

        if (persistAcrossScenes)
            DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        if (menuClip)
        {
            src.clip = menuClip;
            src.Play();
        }
        else
        {
            Debug.LogWarning("[MenuMusic] No menuClip assigned.");
        }
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        if (!autoStopOnSceneChange) return;
        if (!string.IsNullOrEmpty(menuSceneName) && s.name != menuSceneName)
        {
            // Leaving the menu ¡ú fade out and stop
            FadeOutAndStop(defaultFadeOut);
        }
    }

    public void FadeOutAndStop(float seconds)
    {
        if (fadeCo != null) StopCoroutine(fadeCo);
        fadeCo = StartCoroutine(FadeOutCo(Mathf.Max(0f, seconds)));
    }

    private IEnumerator FadeOutCo(float seconds)
    {
        float startVol = src.volume;
        float t = 0f;

        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float k = seconds > 0f ? 1f - (t / seconds) : 0f;
            src.volume = startVol * Mathf.Clamp01(k);
            yield return null;
        }

        src.Stop();
        src.volume = startVol;

        if (destroyWhenStopped)
            Destroy(gameObject);
    }
}
