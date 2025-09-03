using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class TroopUnit : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    private const string DeathTrigger = "Dead";
    private const string DeathState = "Death";

    public bool IsDying { get; private set; }

    private Action _onDeathComplete;

    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
    }

    void OnEnable()
    {
        IsDying = false;

        // re-enable shooter when reused
        var shooter = GetComponent<TroopShooter>();
        if (shooter) shooter.enabled = true;

        var rb = GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        if (animator)
        {
            animator.Rebind();
            animator.Update(0f);
        }
    }

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

        transform.SetParent(null, true);

        var agent = GetComponent<NavMeshAgent>();
        if (agent)
        {
            agent.ResetPath();
            agent.enabled = false;
        }

        // 🔒 stop shooter
        var shooter = GetComponent<TroopShooter>();
        if (shooter)
        {
            shooter.OnOwnerDied();
            shooter.enabled = false;
        }

        var rb = GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (animator) animator.SetTrigger(DeathTrigger);

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
