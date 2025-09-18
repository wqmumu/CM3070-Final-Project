using UnityEngine;
using UnityEngine.Events;

[AddComponentMenu("Game/Finish Gate")]
public class FinishGate : MonoBehaviour
{
    [Tooltip("Called once when the player reaches the finish.")]
    public UnityEvent onVictory = new UnityEvent();   // ensure it's never null

    [Tooltip("If true, this finish trigger will only fire once.")]
    public bool oneShot = true;

    private bool triggered;

    // Auto-bind to the UI controller in the scene (works for prefab instances)
    private void OnEnable()
    {
        var ui = FindFirstObjectByType<SettingsUIController>();
        if (ui != null)
        {
            // public method on your controller
            onVictory.AddListener(ui.ShowLevelComplete);
        }
    }

    private void OnDisable()
    {
        var ui = FindFirstObjectByType<SettingsUIController>();
        if (ui != null)
        {
            onVictory.RemoveListener(ui.ShowLevelComplete);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (oneShot && triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;

        if (AudioManager.I != null)
            AudioManager.I.Play2D(SfxId.Victory);

        // freeze everything
        Time.timeScale = 0f;

        // also stop troops
        var tm = FindFirstObjectByType<TroopManager>();
        if (tm != null) tm.StopAllMovementAndAudio();

        // disable all enemies
        var enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        foreach (var e in enemies) if (e) e.enabled = false;

        // fire any listeners (now includes the SettingsUIController)
        onVictory?.Invoke();

        if (oneShot)
        {
            var col = GetComponent<Collider>();
            if (col) col.enabled = false;
        }
    }
}
