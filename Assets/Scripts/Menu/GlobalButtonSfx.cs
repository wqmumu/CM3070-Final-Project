using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

public class GlobalButtonSfx : MonoBehaviour
{
    [Header("SFX")]
    public AudioClip clickSound;
    [Range(0f, 1f)] public float volume = 1f;
    [Tooltip("Optional mixer group for UI clicks.")]
    public AudioMixerGroup sfxGroup;
    [Tooltip("If true, don't route to a mixer even if one is assigned.")]
    public bool bypassMixer = true;

    [Header("Auto-scan")]
    [Tooltip("Leave null to scan the whole scene. Set to a Canvas/parent to limit the scan.")]
    public Transform scanRoot = null;

    [Tooltip("Keep this object alive across scenes. If you enable this, only place ONE in your project.")]
    public bool persistAcrossScenes = true;

    [Tooltip("If persistent, rescan after each scene load.")]
    public bool rescanOnSceneLoad = true;

    [Tooltip("Delay before rescanning, to catch UI spawned a moment after load.")]
    public float delayedRescanSeconds = 0.15f;

    [Tooltip("Run an extra pass shortly after the first rescan.")]
    public bool extraPass = true;
    public float extraPassDelay = 0.35f;

    private static GlobalButtonSfx _instance; // used only when persistAcrossScenes = true

    private AudioSource src;
    private readonly HashSet<Button> hooked = new HashSet<Button>();


    private void Awake()
    {
        if (persistAcrossScenes)
        {
            if (_instance != null && _instance != this)
            {
                // another instance already exists; this one self-destructs
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        EnsureAudioSourceAlive();
    }

    private void OnEnable()
    {
        if (persistAcrossScenes && rescanOnSceneLoad)
            SceneManager.sceneLoaded += OnSceneLoaded;

        // hook buttons in the current scene
        StartCoroutine(RescanSequence(0f));
    }

    private void OnDisable()
    {
        if (persistAcrossScenes && rescanOnSceneLoad)
            SceneManager.sceneLoaded -= OnSceneLoaded;

        UnhookAllButtons();
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
        if (persistAcrossScenes && rescanOnSceneLoad)
            SceneManager.sceneLoaded -= OnSceneLoaded;

        UnhookAllButtons();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UnhookAllButtons();
        StartCoroutine(RescanSequence(delayedRescanSeconds));
    }

    private IEnumerator RescanSequence(float initialDelay)
    {
        if (initialDelay > 0f) yield return new WaitForSeconds(initialDelay);
        HookAllButtons();

        if (extraPass)
        {
            yield return new WaitForSeconds(extraPassDelay);
            HookAllButtons();
        }
    }

    private void HookAllButtons()
    {
        Button[] candidates;

        if (scanRoot)
        {
            candidates = scanRoot.GetComponentsInChildren<Button>(true);
        }
        else
        {
#if UNITY_2023_1_OR_NEWER
            candidates = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            candidates = FindObjectsOfType<Button>(true);
#endif
        }

        for (int i = 0; i < candidates.Length; i++)
        {
            var b = candidates[i];
            if (b == null || hooked.Contains(b)) continue;
            b.onClick.AddListener(PlayClick);
            hooked.Add(b);
        }
    }

    private void UnhookAllButtons()
    {
        if (hooked.Count == 0) return;

        foreach (var b in hooked)
        {
            if (b != null) b.onClick.RemoveListener(PlayClick);
        }
        hooked.Clear();
    }

    // Allow runtime panels to register their buttons explicitly if needed.
    public void Register(Button b)
    {
        if (b == null || hooked.Contains(b)) return;
        b.onClick.AddListener(PlayClick);
        hooked.Add(b);
    }

    private void PlayClick()
    {
        if (!clickSound) return;

        // If another script killed/disabled our AudioSource between scenes, restore it.
        EnsureAudioSourceAlive();

        // If the GameObject itself is disabled, can't play audio.
        if (!isActiveAndEnabled || src == null) return;

        src.PlayOneShot(clickSound, Mathf.Clamp01(volume));
    }

    private void EnsureAudioSourceAlive()
    {
        // Recreate or re-enable the AudioSource if needed
        if (src == null)
        {
            src = GetComponent<AudioSource>();
            if (src == null) src = gameObject.AddComponent<AudioSource>();
        }

        if (!src.enabled) src.enabled = true;

        // (Re)configure every time in case something changed
        src.playOnAwake = false;
        src.ignoreListenerPause = true; // still plays in pause menus
        src.loop = false;
        src.clip = null;

        if (!bypassMixer && sfxGroup != null)
            src.outputAudioMixerGroup = sfxGroup;
        else
            src.outputAudioMixerGroup = null;
    }
}
