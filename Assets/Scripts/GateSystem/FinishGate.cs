using UnityEngine;
using UnityEngine.Events;

[AddComponentMenu("Game/Finish Gate")]
public class FinishGate : MonoBehaviour
{
    [Tooltip("Called once when the player reaches the finish.")]
    public UnityEvent onVictory;

    [Tooltip("If true, this finish trigger will only fire once.")]
    public bool oneShot = true;

    private bool triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (oneShot && triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;

        if (AudioManager.I != null)
            AudioManager.I.Play2D(SfxId.Victory);

        // freeze everything
        Time.timeScale = 0f;   // <-- stops Update(), physics, animations based on deltaTime

        // also stop troops
        var tm = FindFirstObjectByType<TroopManager>();
        if (tm != null) tm.StopAllMovementAndAudio();

        // disable all enemies
        var enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        foreach (var e in enemies) if (e) e.enabled = false;

        // trigger any custom victory events
        onVictory?.Invoke();

        if (oneShot)
        {
            var col = GetComponent<Collider>();
            if (col) col.enabled = false;
        }
    }
}
