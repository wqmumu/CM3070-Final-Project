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
    [SerializeField] private MonoBehaviour[] scriptsToDisable; // e.g., TroopMovement

    // Animator names
    private const string DeathTrigger = "Dead";   // Trigger in Animator
    private const string DeathState = "Death";  // State name of the death clip

    public bool IsDying { get; private set; }

    private Action _onDeathComplete; // set by TroopManager when removing a unit

    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
    }

    void OnEnable()  // reset for pooled reuse
    {
        IsDying = false;

        if (collidersToDisable != null)
            foreach (var c in collidersToDisable) if (c) c.enabled = true;

        if (scriptsToDisable != null)
            foreach (var s in scriptsToDisable) if (s) s.enabled = true;

        if (animator)
        {
            animator.Rebind();
            animator.Update(0f);
        }
    }

    /// <summary>Starts the death sequence and calls onComplete when the animation finishes.</summary>
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

        // Stop movement systems while dying
        var move = GetComponent<MonoBehaviour>(); // if you have a TroopMovement script, list it in scriptsToDisable instead
        var agent = GetComponent<NavMeshAgent>(); if (agent) agent.enabled = false;

        if (collidersToDisable != null)
            foreach (var c in collidersToDisable) if (c) c.enabled = false;

        if (scriptsToDisable != null)
            foreach (var s in scriptsToDisable) if (s) s.enabled = false;

        if (animator) animator.SetTrigger(DeathTrigger);

        // Wait until we are in Death state, then until it finishes
        if (animator)
        {
            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).IsName(DeathState));
            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.99f);
        }

        if (animator) { animator.Rebind(); animator.Update(0f); }

        _onDeathComplete?.Invoke();
        _onDeathComplete = null;
    }
}
