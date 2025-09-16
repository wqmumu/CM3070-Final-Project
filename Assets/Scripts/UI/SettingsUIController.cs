using UnityEngine;
using UnityEngine.Audio;

public class SettingsUIController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject settingsPanel;        // main settings menu
    [SerializeField] private GameObject volumePanel;          // sliders panel
    [SerializeField] private GameObject controlHintsOverlay;  // control hints overlay

    [Header("Input/Cursor")]
    [SerializeField] private bool openSettingsWithEscape = true;
    [SerializeField] private bool lockCursorDuringPlay = false;

    [Header("State")]
    // Tracks whether Control Hints was opened from Settings (true) or auto/onboarding (false).
    [SerializeField] private bool controlHintsOpenedFromSettings = false;

    void Start()
    {
    }

    void Awake()
    {
        // On first load: show Control Hints as onboarding and PAUSE the game.
        controlHintsOpenedFromSettings = false;    // onboarding
        ShowOnly(controlHintsOverlay);
        ForcePausedState();
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
        // Control Hints opened from Settings; when user taps "Got it", go back to Settings.
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

    // Called by ControlHints "Got it" via ControlHints.OnGotIt()
    public void OnControlHintsGotIt()
    {
        if (controlHintsOpenedFromSettings)
        {
            // Return to Settings (stay paused)
            controlHintsOpenedFromSettings = false;
            OpenSettings();
        }
        else
        {
            // Onboarding flow (startup): close overlays and RESUME gameplay
            CloseAllAndResume();
        }
    }

    public void CloseAllAndResume()
    {
        // Clear the flag whenever we¡¯re exiting UI back to gameplay
        controlHintsOpenedFromSettings = false;

        SafeSetActive(settingsPanel, false);
        SafeSetActive(volumePanel, false);
        SafeSetActive(controlHintsOverlay, false);
        ResumeGameplayState();
    }

    // ---------- Internal helpers ----------
    private void ShowOnly(GameObject panelToShow)
    {
        SafeSetActive(settingsPanel, false);
        SafeSetActive(volumePanel, false);
        SafeSetActive(controlHintsOverlay, false);
        SafeSetActive(panelToShow, true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private bool AnyPanelOpen()
    {
        return (settingsPanel && settingsPanel.activeSelf)
            || (volumePanel && volumePanel.activeSelf)
            || (controlHintsOverlay && controlHintsOverlay.activeSelf);
    }

    private void ForcePausedState()
    {
        Time.timeScale = 0f;
        AudioListener.pause = true; // mutes everything except sources with ignoreListenerPause=true
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
