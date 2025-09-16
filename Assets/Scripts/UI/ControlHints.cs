using UnityEngine;

public class ControlHints : MonoBehaviour
{
    [SerializeField] private GameObject hintsPanel;   // assign your ControlHints root
    [SerializeField] private SettingsUIController ui; // drag SettingsUIController here

    public void OnGotIt()
    {
        if (ui)
        {
            // Let SettingsUIController decide whether to resume gameplay
            // or return to Settings based on how Control Hints was opened.
            ui.OnControlHintsGotIt();
            return;
        }

        // Fallback if ui isn¡¯t wired: hide hints and resume gameplay.
        if (hintsPanel) hintsPanel.SetActive(false);
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

}
