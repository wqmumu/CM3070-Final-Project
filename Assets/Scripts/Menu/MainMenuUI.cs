using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class MainMenuUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject rootMenuPanel;
    [SerializeField] private GameObject volumePanel;       
    [SerializeField] private GameObject controlHintsPanel; 
    [SerializeField] private GameObject loadingPanel;

    [Header("Root Menu Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button volumeButton; 
    [SerializeField] private Button controlsButton;
    [SerializeField] private Button quitButton;

    [Header("Loading UI")]
    [SerializeField] private Slider progressBar;
    [SerializeField] private Text progressText;

    [Header("Scene Names")]
    [SerializeField] private string gameSceneName = "Game";

    [Header("Selection (optional)")]
    [SerializeField] private Selectable defaultRootSelection;   
    [SerializeField] private Selectable defaultVolumeSelection; 
    [SerializeField] private Selectable defaultControlsSelection; 

    private void Awake()
    {
        // Menus should not be paused
        if (Time.timeScale != 1f) Time.timeScale = 1f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        ShowRootMenu();

        // Wire buttons
        if (playButton) playButton.onClick.AddListener(OnPlayClicked);
        if (volumeButton) volumeButton.onClick.AddListener(OnOpenVolume);
        if (controlsButton) controlsButton.onClick.AddListener(OnOpenControls);
        if (quitButton) quitButton.onClick.AddListener(OnQuitClicked);
    }

    private void OnDestroy()
    {
        if (playButton) playButton.onClick.RemoveListener(OnPlayClicked);
        if (volumeButton) volumeButton.onClick.RemoveAllListeners();
        if (controlsButton) controlsButton.onClick.RemoveAllListeners();
        if (quitButton) quitButton.onClick.RemoveAllListeners();
    }

    // ---------- Panel helpers ----------
    private void SetActiveSafe(GameObject go, bool on)
    {
        if (go && go.activeSelf != on) go.SetActive(on);
    }

    private void SetFirstSelected(Selectable s)
    {
        if (!EventSystem.current) return;
        EventSystem.current.SetSelectedGameObject(null);
        if (s) EventSystem.current.SetSelectedGameObject(s.gameObject);
    }

    private void ShowRootMenu()
    {
        SetActiveSafe(rootMenuPanel, true);
        SetActiveSafe(volumePanel, false);
        SetActiveSafe(controlHintsPanel, false);
        SetActiveSafe(loadingPanel, false);
        SetFirstSelected(defaultRootSelection ? defaultRootSelection : (Selectable)playButton);
    }

    // ---------- Button handlers ----------
    public void OnOpenVolume()
    {
        SetActiveSafe(rootMenuPanel, false);
        SetActiveSafe(controlHintsPanel, false);
        SetActiveSafe(loadingPanel, false);
        SetActiveSafe(volumePanel, true);
        SetFirstSelected(defaultVolumeSelection);
    }

    public void OnOpenControls()
    {
        SetActiveSafe(rootMenuPanel, false);
        SetActiveSafe(volumePanel, false);
        SetActiveSafe(loadingPanel, false);
        SetActiveSafe(controlHintsPanel, true);
        SetFirstSelected(defaultControlsSelection);
    }

    public void OnBackToRoot() => ShowRootMenu();

    private void OnPlayClicked() => StartCoroutine(LoadGameAsync());

    private IEnumerator LoadGameAsync()
    {
        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogError("[MainMenuUI] Game scene name is not set.");
            yield break;
        }

        // Show loading UI
        SetActiveSafe(loadingPanel, true);
        SetActiveSafe(rootMenuPanel, false);
        SetActiveSafe(volumePanel, false);
        SetActiveSafe(controlHintsPanel, false);

        if (Time.timeScale != 1f) Time.timeScale = 1f;

        // fade out menu music if present
        var menuMusic = FindFirstObjectByType<MenuMusic>(FindObjectsInactive.Include);
        if (menuMusic) menuMusic.FadeOutAndStop(0.5f);

        AsyncOperation op = UnityEngine.SceneManagement.SceneManager
            .LoadSceneAsync(gameSceneName, LoadSceneMode.Single);
        op.allowSceneActivation = false;

        while (!op.isDone)
        {
            float progress = Mathf.Clamp01(op.progress / 0.9f);
            if (progressBar) progressBar.value = progress;
            if (progressText) progressText.text = $"Loading¡­ {(int)(progress * 100f)}%";

            if (op.progress >= 0.9f)
            {
                yield return new WaitForSecondsRealtime(0.15f);
                op.allowSceneActivation = true;
            }
            yield return null;
        }
    }


    private void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
