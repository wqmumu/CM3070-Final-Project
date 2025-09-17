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
    public AudioMixerGroup sfxGroup;

    [Header("Auto-scan")]
    [Tooltip("Leave null to scan the whole scene. Set to a Canvas/parent to limit the scan.")]
    public Transform scanRoot = null;

    [Tooltip("Keep this object alive across scenes. If you enable this, only place ONE in your project.")]
    public bool persistAcrossScenes = false;

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
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.ignoreListenerPause = true; // play in pause menus
        if (sfxGroup) src.outputAudioMixerGroup = sfxGroup;
    }

    private void OnEnable()
    {
        if (persistAcrossScenes && rescanOnSceneLoad)
            SceneManager.sceneLoaded += OnSceneLoaded;

        // hook buttons in the current scene too
        StartCoroutine(RescanSequence(0f));
    }

    private void OnDisable()
    {
        if (persistAcrossScenes && rescanOnSceneLoad)
            SceneManager.sceneLoaded -= OnSceneLoaded;

        hooked.Clear();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        hooked.Clear();
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
        Button[] candidates = scanRoot
            ? scanRoot.GetComponentsInChildren<Button>(true)
            : FindObjectsOfType<Button>(true);

        foreach (var b in candidates)
        {
            if (b == null || hooked.Contains(b)) continue;
            b.onClick.AddListener(PlayClick);
            hooked.Add(b);
        }
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
        if (clickSound)
            src.PlayOneShot(clickSound, volume);
    }
}
