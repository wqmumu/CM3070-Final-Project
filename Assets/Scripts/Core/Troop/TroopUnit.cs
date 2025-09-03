using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class TroopUnit : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Disable while dying (optional)")]
    [SerializeField] private Collider[] collidersToDisable;
    [SerializeField] private MonoBehaviour[] scriptsToDisable; // e.g., forward mover, shake, etc.

    // Animator parameters / state names
    private const string DeathTrigger = "Dead";   // Animator Trigger
    private const string DeathState = "Death";  // State name of death clip

    public bool IsDying { get; private set; }

    private Action _onDeathComplete;

    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
    }

    void OnEnable() // reset for pooled reuse
    {
        IsDying = false;

        if (collidersToDisable != null)
            foreach (var c in collidersToDisable) if (c) c.enabled = true;

        if (scriptsToDisable != null)
            foreach (var s in scriptsToDisable) if (s) s.enabled = true;

        var rb = GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true; // back to normal while alive

        if (animator)
        {
            animator.Rebind();
            animator.Update(0f);
        }
    }

    /// <summary>
    /// Starts the death sequence and calls onComplete when the animation finishes.
    /// The corpse stays at the death spot (no more formation/follow updates).
    /// </summary>
    public void PlayDeath(Action onComplete)
    {
        if (!gameObject.activeInHierarchy)
        {
            onComplete?.Invoke();
            return;
        }

        _onDeathComplete = onComplete;
        StartCoroutine(Co_PlayDeath());
    }

    private IEnumerator Co_PlayDeath()
    {
        IsDying = true;

        // Hard stop: detach from any parent so formation roots can't drag us around
        transform.SetParent(null, true);

        // Kill all motion systems tied to this unit
        var agent = GetComponent<NavMeshAgent>();
        if (agent)
        {
            agent.ResetPath();
            agent.enabled = false;
        }

        if (scriptsToDisable != null)
            foreach (var s in scriptsToDisable) if (s) s.enabled = false;

        // Let physics keep the body grounded exactly where it died (optional)
        var rb = GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Optional: stop interacting while dying
        if (collidersToDisable != null)
            foreach (var c in collidersToDisable) if (c) c.enabled = false;

        // Play the death animation
        if (animator) animator.SetTrigger(DeathTrigger);

        // Wait until we're in the death state, then until it��s nearly finished
        if (animator)
        {
            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).IsName(DeathState));
            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.99f);
        }

        // Clean animator so pooled reuse starts clean
        if (animator) { animator.Rebind(); animator.Update(0f); }

        _onDeathComplete?.Invoke();
        _onDeathComplete = null;
    }
}
