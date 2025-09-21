using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class SettingsUIController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject volumePanel;
    [SerializeField] private GameObject controlHintsOverlay;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject levelCompletePanel;

    [Header("Input/Cursor")]
    [SerializeField] private bool openSettingsWithEscape = true;
    [SerializeField] private bool lockCursorDuringPlay = false;

    [Header("Navigation")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("State")]
    [SerializeField] private bool controlHintsOpenedFromSettings = false;

    void Awake()
    {
        controlHintsOpenedFromSettings = false;
        ShowOnly(controlHintsOverlay);
        ForcePausedState();

        SafeSetActive(gameOverPanel, false);
        SafeSetActive(levelCompletePanel, false);
    }

    void OnEnable()
    {
        var tm = FindFirstObjectByType<TroopManager>();
        if (tm != null) tm.onDefeat.AddListener(ShowGameOver);

        // Subscribe to ALL finish gates in scene
        var gates = FindObjectsByType<FinishGate>(FindObjectsSortMode.None);
        foreach (var g in gates) if (g != null) g.onVictory.AddListener(ShowLevelComplete);
    }

    void OnDisable()
    {
        var tm = FindFirstObjectByType<TroopManager>();
        if (tm != null) tm.onDefeat.RemoveListener(ShowGameOver);

        var gates = FindObjectsByType<FinishGate>(FindObjectsSortMode.None);
        foreach (var g in gates) if (g != null) g.onVictory.RemoveListener(ShowLevelComplete);
    }

    void Update()
    {
        if (!openSettingsWithEscape) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (AnyPanelOpen()) CloseAllAndResume();
            else OpenSettings();
        }
    }

    // ---------- Public UI Hooks ----------
    public void OpenSettings()
    {
        ShowOnly(settingsPanel);
        ForcePausedState();
    }

    public void OnClickVolume()
    {
        ShowOnly(volumePanel);
        ForcePausedState();
    }

    public void OnClickControlHints()
    {
        controlHintsOpenedFromSettings = true;
        ShowOnly(controlHintsOverlay);
        ForcePausedState();
    }

    public void OnClickSettingsBack()
    {
        CloseAllAndResume();
    }

    public void OnClickVolumeBack()
    {
        ShowOnly(settingsPanel);
        ForcePausedState();
    }

    // Called by ControlHints "Got it"
    public void OnControlHintsGotIt()
    {
        if (controlHintsOpenedFromSettings)
        {
            controlHintsOpenedFromSettings = false;
            OpenSettings();
        }
        else
        {
            CloseAllAndResume();
        }
    }

    public void CloseAllAndResume()
    {
        controlHintsOpenedFromSettings = false;

        SafeSetActive(settingsPanel, false);
        SafeSetActive(volumePanel, false);
        SafeSetActive(controlHintsOverlay, false);
        SafeSetActive(gameOverPanel, false);
        SafeSetActive(levelCompletePanel, false);

        ResumeGameplayState();
    }

    // Menu button ¡ú go back to main menu
    public void OnClickMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        controlHintsOpenedFromSettings = false;

        if (!string.IsNullOrEmpty(mainMenuSceneName))
            SceneManager.LoadScene(mainMenuSceneName);
        else
            Debug.LogWarning("Main Menu scene name is empty. Set 'mainMenuSceneName' in the Inspector.");
    }

    // ---------- Restart / Next Level ----------
    public void OnClickRestart()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        var current = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(current);
    }

    public void OnClickNextLevel(string nextSceneName)
    {
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning("Next level scene name not set.");
            return;
        }

        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        SceneManager.LoadScene(nextSceneName);
    }

    public void OnClickNextLevelAuto()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        int i = SceneManager.GetActiveScene().buildIndex;
        int count = SceneManager.sceneCountInBuildSettings;
        int next = Mathf.Clamp(i + 1, 0, count - 1);
        SceneManager.LoadScene(next);
    }

    // ---------- Panel Controls (PUBLIC for UnityEvent) ----------
    public void ShowGameOver()
    {
        SafeSetActive(settingsPanel, false);
        SafeSetActive(volumePanel, false);
        SafeSetActive(controlHintsOverlay, false);
        SafeSetActive(levelCompletePanel, false);

        if (gameOverPanel) gameOverPanel.SetActive(true);

        Time.timeScale = 0f;
        AudioListener.pause = true;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ShowLevelComplete()
    {
        SafeSetActive(settingsPanel, false);
        SafeSetActive(volumePanel, false);
        SafeSetActive(controlHintsOverlay, false);
        SafeSetActive(gameOverPanel, false);

        if (levelCompletePanel) levelCompletePanel.SetActive(true);

        Time.timeScale = 0f;
        AudioListener.pause = true;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    // ---------- Internal helpers ----------
    private void ShowOnly(GameObject panelToShow)
    {
        SafeSetActive(settingsPanel, false);
        SafeSetActive(volumePanel, false);
        SafeSetActive(controlHintsOverlay, false);
        SafeSetActive(gameOverPanel, false);
        SafeSetActive(levelCompletePanel, false);
        SafeSetActive(panelToShow, true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private bool AnyPanelOpen()
    {
        return (settingsPanel && settingsPanel.activeSelf)
            || (volumePanel && volumePanel.activeSelf)
            || (controlHintsOverlay && controlHintsOverlay.activeSelf)
            || (gameOverPanel && gameOverPanel.activeSelf)
            || (levelCompletePanel && levelCompletePanel.activeSelf);
    }

    private void ForcePausedState()
    {
        Time.timeScale = 0f;
        AudioListener.pause = true;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void ResumeGameplayState()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (lockCursorDuringPlay)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private static void SafeSetActive(GameObject go, bool v)
    {
        if (go && go.activeSelf != v) go.SetActive(v);
    }
}
